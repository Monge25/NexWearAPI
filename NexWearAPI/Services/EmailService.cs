using MailKit.Security;
using MimeKit;
using MailKit.Net.Smtp;
using NexWearAPI.Models;

namespace NexWearAPI.Services
{
    public interface IEmailService
    {
        Task SendPasswordResetCodeAsync(string toEmail, string firstName, string code);
        Task SendOrderStatusEmailAsync(Order order, string firstName, string toEmail);
    }

    public class EmailService : IEmailService
    {
        private readonly IConfiguration _config;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IConfiguration config, ILogger<EmailService> logger)
        {
            _config = config;
            _logger = logger;
        }

        // ── Recuperación de contraseña ──────────────────────────────────────────

        public async Task SendPasswordResetCodeAsync(string toEmail, string firstName, string code)
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(_config["Email:FromName"]!, _config["Email:Username"]!));
            message.To.Add(new MailboxAddress(firstName, toEmail));
            message.Subject = "Código de acceso — NexWear";
            message.Body = new TextPart("html") { Text = BuildResetEmail(firstName, code) };
            await SendAsync(message, toEmail);
        }

        // ── Email por estado de orden ───────────────────────────────────────────

        public async Task SendOrderStatusEmailAsync(Order order, string firstName, string toEmail)
        {
            var (subject, body) = order.Status switch
            {
                OrderStatus.Paid      => BuildPaidEmail(order, firstName),
                OrderStatus.Shipped   => BuildShippedEmail(order, firstName),
                OrderStatus.Delivered => BuildDeliveredEmail(order, firstName),
                OrderStatus.Cancelled => BuildCancelledEmail(order, firstName),
                _                     => ((string?)null, (string?)null)
            };

            if (subject is null) return;

            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(_config["Email:FromName"]!, _config["Email:Username"]!));
            message.To.Add(new MailboxAddress(firstName, toEmail));
            message.Subject = subject;
            message.Body = new TextPart("html") { Text = body! };
            await SendAsync(message, toEmail);
        }

        // ── Builders ────────────────────────────────────────────────────────────

        private static string BuildResetEmail(string firstName, string code) =>
            Wrap($@"
  <p style='margin:0 0 32px;font-size:13px;letter-spacing:3px;text-transform:uppercase;color:#a89d8f;'>Seguridad de cuenta</p>
  <h1 style='margin:0 0 16px;font-size:26px;font-weight:300;color:#1a1714;letter-spacing:-0.5px;'>Código de acceso</h1>
  <p style='margin:0 0 40px;font-size:15px;color:#6b6057;line-height:1.7;'>
    Hola, {firstName}. Recibimos una solicitud para restablecer la contraseña de tu cuenta.
    Utiliza el siguiente código para continuar.
  </p>

  <div style='background:#f5f0ea;border:1px solid #e8e0d5;padding:36px;text-align:center;margin:0 0 40px;'>
    <p style='margin:0 0 8px;font-size:11px;letter-spacing:3px;text-transform:uppercase;color:#a89d8f;'>Tu código</p>
    <p style='margin:0;font-size:40px;font-weight:300;letter-spacing:14px;color:#1a1714;'>{code}</p>
  </div>

  <p style='margin:0;font-size:13px;color:#a89d8f;line-height:1.6;'>
    Este código expira en <strong style='color:#6b6057;font-weight:400;'>15 minutos</strong>.
    Si no solicitaste este cambio, puedes ignorar este mensaje con seguridad.
  </p>");

        private static (string, string) BuildPaidEmail(Order order, string firstName)
        {
            var num = OrderNumber(order);
            return (
                $"Pedido confirmado {num} — NexWear",
                Wrap($@"
  <p style='margin:0 0 32px;font-size:13px;letter-spacing:3px;text-transform:uppercase;color:#a89d8f;'>Confirmación de pedido</p>
  <h1 style='margin:0 0 16px;font-size:26px;font-weight:300;color:#1a1714;letter-spacing:-0.5px;'>Gracias por tu compra, {firstName}.</h1>
  <p style='margin:0 0 40px;font-size:15px;color:#6b6057;line-height:1.7;'>
    Tu pago fue procesado correctamente. Hemos recibido tu pedido y ya estamos preparándolo con cuidado.
  </p>

  {SummaryBox(num, order.Total, order.PaidAt ?? order.CreatedAt)}
  {ItemsTable(order)}
  {AddressBox(order)}

  <p style='margin:32px 0 0;font-size:14px;color:#a89d8f;line-height:1.6;'>
    Te notificaremos en cuanto tu pedido sea enviado.
  </p>")
            );
        }

        private static (string, string) BuildShippedEmail(Order order, string firstName)
        {
            var num = OrderNumber(order);
            return (
                $"Tu pedido {num} está en camino — NexWear",
                Wrap($@"
  <p style='margin:0 0 32px;font-size:13px;letter-spacing:3px;text-transform:uppercase;color:#a89d8f;'>Actualización de envío</p>
  <h1 style='margin:0 0 16px;font-size:26px;font-weight:300;color:#1a1714;letter-spacing:-0.5px;'>Tu pedido está en camino.</h1>
  <p style='margin:0 0 40px;font-size:15px;color:#6b6057;line-height:1.7;'>
    Hola, {firstName}. Tu paquete ha sido enviado y se encuentra en ruta hacia tu domicilio.
  </p>

  {SummaryBox(num, order.Total, order.CreatedAt)}
  {ItemsTable(order)}
  {AddressBox(order)}

  <div style='border-top:1px solid #e8e0d5;padding-top:24px;margin-top:32px;'>
    <p style='margin:0;font-size:13px;color:#a89d8f;line-height:1.6;'>
      Tiempo estimado de entrega: <strong style='color:#6b6057;font-weight:400;'>3 a 7 días hábiles</strong>.
    </p>
  </div>")
            );
        }

        private static (string, string) BuildDeliveredEmail(Order order, string firstName)
        {
            var num = OrderNumber(order);
            return (
                $"Tu pedido {num} ha sido entregado — NexWear",
                Wrap($@"
  <p style='margin:0 0 32px;font-size:13px;letter-spacing:3px;text-transform:uppercase;color:#a89d8f;'>Pedido entregado</p>
  <h1 style='margin:0 0 16px;font-size:26px;font-weight:300;color:#1a1714;letter-spacing:-0.5px;'>Tu pedido ha llegado, {firstName}.</h1>
  <p style='margin:0 0 40px;font-size:15px;color:#6b6057;line-height:1.7;'>
    Esperamos que estés completamente satisfecho con tu compra. Ha sido un placer atenderte.
  </p>

  {SummaryBox(num, order.Total, order.CreatedAt)}
  {ItemsTable(order)}

  <div style='border:1px solid #e8e0d5;padding:24px;margin:32px 0 0;'>
    <p style='margin:0 0 6px;font-size:11px;letter-spacing:3px;text-transform:uppercase;color:#a89d8f;'>Una ultima cosa</p>
    <p style='margin:0;font-size:14px;color:#6b6057;line-height:1.6;'>
      Tu opinión nos ayuda a mejorar. Si deseas compartir tu experiencia, puedes dejar una reseña en tu cuenta.
    </p>
  </div>")
            );
        }

        private static (string, string) BuildCancelledEmail(Order order, string firstName)
        {
            var num = OrderNumber(order);
            return (
                $"Pedido {num} cancelado — NexWear",
                Wrap($@"
  <p style='margin:0 0 32px;font-size:13px;letter-spacing:3px;text-transform:uppercase;color:#a89d8f;'>Cancelación de pedido</p>
  <h1 style='margin:0 0 16px;font-size:26px;font-weight:300;color:#1a1714;letter-spacing:-0.5px;'>Tu pedido ha sido cancelado.</h1>
  <p style='margin:0 0 40px;font-size:15px;color:#6b6057;line-height:1.7;'>
    Hola, {firstName}. Lamentamos informarte que tu pedido ha sido cancelado.
    Si tienes alguna duda, nuestro equipo está disponible para ayudarte.
  </p>

  {SummaryBox(num, order.Total, order.CreatedAt)}

  <div style='border-top:1px solid #e8e0d5;padding-top:24px;margin-top:32px;'>
    <p style='margin:0;font-size:13px;color:#a89d8f;line-height:1.6;'>
      Si realizaste un pago, el reembolso se procesará en un plazo de
      <strong style='color:#6b6057;font-weight:400;'>5 a 10 días hábiles</strong>
      según tu banco o institución financiera.
    </p>
  </div>")
            );
        }

        // ── Componentes HTML ─────────────────────────────────────────────────────

        private static string Wrap(string content) =>
            $@"<!DOCTYPE html>
<html lang='es'>
<head><meta charset='UTF-8'/><meta name='viewport' content='width=device-width,initial-scale=1.0'/></head>
<body style='margin:0;padding:0;background:#faf7f4;font-family:Georgia,""Times New Roman"",serif;'>
  <table width='100%' cellpadding='0' cellspacing='0' style='background:#faf7f4;'>
    <tr><td align='center' style='padding:48px 24px;'>
      <table width='100%' cellpadding='0' cellspacing='0' style='max-width:560px;'>

        <!-- Cabecera -->
        <tr><td style='padding:0 0 48px;text-align:center;border-bottom:1px solid #d4c9bc;'>
          <p style='margin:0;font-size:11px;letter-spacing:6px;text-transform:uppercase;color:#1a1714;font-family:Arial,sans-serif;font-weight:400;'>N E X W E A R</p>
        </td></tr>

        <!-- Contenido -->
        <tr><td style='padding:48px 0;'>
          {content}
        </td></tr>

        <!-- Pie -->
        <tr><td style='border-top:1px solid #d4c9bc;padding:32px 0 0;text-align:center;'>
          <p style='margin:0 0 8px;font-size:11px;letter-spacing:6px;text-transform:uppercase;color:#1a1714;font-family:Arial,sans-serif;font-weight:400;'>N E X W E A R</p>
          <p style='margin:0;font-size:12px;color:#a89d8f;font-family:Arial,sans-serif;'>Tu tienda de moda — nexwear.com</p>
        </td></tr>

      </table>
    </td></tr>
  </table>
</body>
</html>";

        private static string OrderNumber(Order order) =>
            $"ORD-{order.Id.ToString()[..8].ToUpper()}";

        private static string SummaryBox(string num, decimal total, DateTime date) =>
            $@"<table width='100%' cellpadding='0' cellspacing='0' style='background:#f5f0ea;border:1px solid #e8e0d5;margin:0 0 32px;'>
  <tr>
    <td style='padding:20px 24px;border-bottom:1px solid #e8e0d5;'>
      <p style='margin:0;font-size:11px;letter-spacing:3px;text-transform:uppercase;color:#a89d8f;font-family:Arial,sans-serif;'>Pedido</p>
      <p style='margin:4px 0 0;font-size:15px;color:#1a1714;'>{num}</p>
    </td>
    <td style='padding:20px 24px;border-bottom:1px solid #e8e0d5;text-align:right;'>
      <p style='margin:0;font-size:11px;letter-spacing:3px;text-transform:uppercase;color:#a89d8f;font-family:Arial,sans-serif;'>Fecha</p>
      <p style='margin:4px 0 0;font-size:15px;color:#1a1714;'>{date:dd/MM/yyyy}</p>
    </td>
  </tr>
  <tr>
    <td colspan='2' style='padding:20px 24px;text-align:right;'>
      <p style='margin:0;font-size:11px;letter-spacing:3px;text-transform:uppercase;color:#a89d8f;font-family:Arial,sans-serif;'>Total</p>
      <p style='margin:4px 0 0;font-size:22px;font-weight:300;color:#1a1714;'>${total:N2} <span style='font-size:14px;color:#a89d8f;'>MXN</span></p>
    </td>
  </tr>
</table>";

        private static string ItemsTable(Order order)
        {
            if (!order.OrderItems.Any()) return string.Empty;

            var rows = string.Join("", order.OrderItems.Select(i =>
                $@"<tr>
  <td style='padding:16px 0;border-bottom:1px solid #e8e0d5;vertical-align:top;'>
    <p style='margin:0;font-size:14px;color:#1a1714;'>{i.ProductName}</p>
    <p style='margin:4px 0 0;font-size:12px;letter-spacing:1px;color:#a89d8f;font-family:Arial,sans-serif;text-transform:uppercase;'>{i.VariantColor} &middot; Talla {i.VariantSize}</p>
  </td>
  <td style='padding:16px 8px;border-bottom:1px solid #e8e0d5;text-align:center;vertical-align:top;'>
    <p style='margin:0;font-size:13px;color:#6b6057;font-family:Arial,sans-serif;'>x{i.Quantity}</p>
  </td>
  <td style='padding:16px 0;border-bottom:1px solid #e8e0d5;text-align:right;vertical-align:top;'>
    <p style='margin:0;font-size:14px;color:#1a1714;'>${i.UnitPrice * i.Quantity:N2}</p>
  </td>
</tr>"));

            return $@"<table width='100%' cellpadding='0' cellspacing='0' style='margin:0 0 32px;'>
  <thead>
    <tr>
      <th style='text-align:left;padding-bottom:12px;border-bottom:1px solid #1a1714;font-size:11px;letter-spacing:3px;text-transform:uppercase;color:#1a1714;font-weight:400;font-family:Arial,sans-serif;'>Producto</th>
      <th style='text-align:center;padding-bottom:12px;border-bottom:1px solid #1a1714;font-size:11px;letter-spacing:3px;text-transform:uppercase;color:#1a1714;font-weight:400;font-family:Arial,sans-serif;'>Cant.</th>
      <th style='text-align:right;padding-bottom:12px;border-bottom:1px solid #1a1714;font-size:11px;letter-spacing:3px;text-transform:uppercase;color:#1a1714;font-weight:400;font-family:Arial,sans-serif;'>Subtotal</th>
    </tr>
  </thead>
  <tbody>{rows}</tbody>
</table>";
        }

        private static string AddressBox(Order order)
        {
            var interior = order.Interior is not null ? $", {order.Interior}" : "";
            var phone = order.Phone is not null ? $"<br/>{order.Phone}" : "";
            return $@"<table width='100%' cellpadding='0' cellspacing='0' style='background:#f5f0ea;border:1px solid #e8e0d5;margin:0 0 32px;'>
  <tr>
    <td style='padding:20px 24px;'>
      <p style='margin:0 0 8px;font-size:11px;letter-spacing:3px;text-transform:uppercase;color:#a89d8f;font-family:Arial,sans-serif;'>Direccion de entrega</p>
      <p style='margin:0;font-size:14px;color:#6b6057;line-height:1.7;'>
        {order.Street}{interior}<br/>
        {order.City}, {order.State} {order.ZipCode}<br/>
        {order.Country}{phone}
      </p>
    </td>
  </tr>
</table>";
        }

        // ── SMTP ─────────────────────────────────────────────────────────────────

        private async Task SendAsync(MimeMessage message, string toEmail)
        {
            try
            {
                using var client = new SmtpClient();
                await client.ConnectAsync(_config["Email:Host"]!, int.Parse(_config["Email:Port"]!), SecureSocketOptions.StartTls);
                await client.AuthenticateAsync(_config["Email:Username"]!, _config["Email:Password"]!);
                await client.SendAsync(message);
                await client.DisconnectAsync(true);
                _logger.LogInformation("Email enviado a {Email}", toEmail);
            }
            catch (Exception ex)
            {
                _logger.LogError("Error al enviar email a {Email}: {Error} | Inner: {Inner}", toEmail, ex.Message, ex.InnerException?.Message);
                throw;
            }
        }
    }
}
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
            message.Subject = "Código de recuperación — NexWear";
            message.Body = new TextPart("html")
            {
                Text = BuildResetEmail(firstName, code)
            };
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
            $@"<!DOCTYPE html>
<html><body style='font-family:Arial,sans-serif;max-width:480px;margin:0 auto;padding:24px;'>
  {Header()}
  <h2 style='color:#111;'>Hola, {firstName} 👋</h2>
  <p style='color:#555;'>Recibimos una solicitud para restablecer la contraseña de tu cuenta en NexWear.</p>
  <p style='color:#555;'>Tu código de recuperación es:</p>
  <div style='background:#f5f5f5;border-radius:8px;padding:24px;text-align:center;margin:24px 0;'>
    <span style='font-size:36px;font-weight:bold;letter-spacing:8px;color:#111;'>{code}</span>
  </div>
  <p style='color:#888;font-size:13px;'>Este código expira en <strong>15 minutos</strong>. Si no solicitaste este cambio, ignora este mensaje.</p>
  {Footer()}
</body></html>";

        private static (string, string) BuildPaidEmail(Order order, string firstName)
        {
            var num = OrderNumber(order);
            return (
                $"✅ Pedido confirmado {num} — NexWear",
                $@"<!DOCTYPE html>
<html><body style='font-family:Arial,sans-serif;max-width:600px;margin:0 auto;padding:24px;color:#333;'>
  {Header()}
  <h2 style='color:#111;'>¡Gracias por tu compra, {firstName}! 🎉</h2>
  <p>Tu pago fue procesado exitosamente. Hemos recibido tu pedido y ya estamos preparándolo.</p>
  {SummaryBox(num, order.Total, order.PaidAt ?? order.CreatedAt)}
  {ItemsTable(order)}
  {AddressBox(order)}
  <p style='color:#555;'>Te notificaremos cuando tu pedido sea enviado.</p>
  {Footer()}
</body></html>"
            );
        }

        private static (string, string) BuildShippedEmail(Order order, string firstName)
        {
            var num = OrderNumber(order);
            return (
                $"🚚 Tu pedido {num} está en camino — NexWear",
                $@"<!DOCTYPE html>
<html><body style='font-family:Arial,sans-serif;max-width:600px;margin:0 auto;padding:24px;color:#333;'>
  {Header()}
  <h2 style='color:#111;'>¡Tu pedido está en camino, {firstName}! 🚚</h2>
  <p>Tu paquete ha sido enviado y está en ruta hacia tu domicilio.</p>
  {SummaryBox(num, order.Total, order.CreatedAt)}
  {ItemsTable(order)}
  {AddressBox(order)}
  <div style='background:#fff8e1;border-left:4px solid #f59e0b;padding:16px;border-radius:4px;margin:24px 0;'>
    <p style='margin:0;color:#92400e;'>📦 El tiempo estimado de entrega es de <strong>3 a 7 días hábiles</strong>.</p>
  </div>
  {Footer()}
</body></html>"
            );
        }

        private static (string, string) BuildDeliveredEmail(Order order, string firstName)
        {
            var num = OrderNumber(order);
            return (
                $"📦 Tu pedido {num} fue entregado — NexWear",
                $@"<!DOCTYPE html>
<html><body style='font-family:Arial,sans-serif;max-width:600px;margin:0 auto;padding:24px;color:#333;'>
  {Header()}
  <h2 style='color:#111;'>¡Tu pedido fue entregado, {firstName}! 🎊</h2>
  <p>Esperamos que estés disfrutando tu compra. Si tienes algún problema no dudes en contactarnos.</p>
  {SummaryBox(num, order.Total, order.CreatedAt)}
  {ItemsTable(order)}
  <div style='background:#f0fdf4;border-left:4px solid #22c55e;padding:16px;border-radius:4px;margin:24px 0;'>
    <p style='margin:0;color:#166534;'>⭐ ¿Te gustó tu compra? ¡Deja una reseña y ayuda a otros clientes!</p>
  </div>
  {Footer()}
</body></html>"
            );
        }

        private static (string, string) BuildCancelledEmail(Order order, string firstName)
        {
            var num = OrderNumber(order);
            return (
                $"❌ Pedido {num} cancelado — NexWear",
                $@"<!DOCTYPE html>
<html><body style='font-family:Arial,sans-serif;max-width:600px;margin:0 auto;padding:24px;color:#333;'>
  {Header()}
  <h2 style='color:#111;'>Tu pedido fue cancelado, {firstName}</h2>
  <p>Lamentamos informarte que tu pedido ha sido cancelado.</p>
  {SummaryBox(num, order.Total, order.CreatedAt)}
  <div style='background:#fef2f2;border-left:4px solid #ef4444;padding:16px;border-radius:4px;margin:24px 0;'>
    <p style='margin:0;color:#991b1b;'>Si realizaste un pago, el reembolso se procesará en un plazo de <strong>5 a 10 días hábiles</strong>.</p>
  </div>
  <p style='color:#555;'>Si tienes dudas, contáctanos y con gusto te atendemos.</p>
  {Footer()}
</body></html>"
            );
        }

        // ── Componentes HTML ─────────────────────────────────────────────────────

        private static string Header() =>
            "<div style='text-align:center;padding:16px 0;border-bottom:2px solid #111;margin-bottom:24px;'>" +
            "<h1 style='margin:0;font-size:28px;letter-spacing:4px;color:#111;'>NEXWEAR</h1></div>";

        private static string Footer() =>
            "<hr style='border:none;border-top:1px solid #e8e8e8;margin:32px 0;'/>" +
            "<p style='color:#bbb;font-size:12px;text-align:center;margin:0;'>NexWear · Tu tienda de moda</p>";

        private static string OrderNumber(Order order) =>
            $"ORD-{order.Id.ToString()[..8].ToUpper()}";

        private static string SummaryBox(string num, decimal total, DateTime date) =>
            $@"<div style='background:#f9f9f9;border-radius:8px;padding:16px;margin:24px 0;'>
  <table style='width:100%;font-size:14px;'>
    <tr><td style='color:#888;padding:4px 0;'>Número de pedido</td><td style='text-align:right;font-weight:bold;color:#111;'>{num}</td></tr>
    <tr><td style='color:#888;padding:4px 0;'>Fecha</td><td style='text-align:right;color:#111;'>{date:dd/MM/yyyy}</td></tr>
    <tr><td style='color:#888;padding:4px 0;'>Total</td><td style='text-align:right;font-weight:bold;color:#111;font-size:16px;'>${total:N2} MXN</td></tr>
  </table>
</div>";

        private static string ItemsTable(Order order)
        {
            if (!order.OrderItems.Any()) return string.Empty;
            var rows = string.Join("", order.OrderItems.Select(i =>
                $"<tr>" +
                $"<td style='padding:10px 0;border-bottom:1px solid #f0f0f0;'><strong>{i.ProductName}</strong><br/>" +
                $"<span style='color:#888;font-size:13px;'>{i.VariantColor} · Talla {i.VariantSize}</span></td>" +
                $"<td style='text-align:center;padding:10px 8px;border-bottom:1px solid #f0f0f0;color:#555;'>x{i.Quantity}</td>" +
                $"<td style='text-align:right;padding:10px 0;border-bottom:1px solid #f0f0f0;font-weight:bold;'>${i.UnitPrice * i.Quantity:N2}</td>" +
                "</tr>"));

            return $@"<table style='width:100%;border-collapse:collapse;font-size:14px;margin:16px 0;'>
  <thead><tr>
    <th style='text-align:left;padding-bottom:8px;border-bottom:2px solid #111;'>Producto</th>
    <th style='text-align:center;padding-bottom:8px;border-bottom:2px solid #111;'>Cant.</th>
    <th style='text-align:right;padding-bottom:8px;border-bottom:2px solid #111;'>Subtotal</th>
  </tr></thead>
  <tbody>{rows}</tbody>
</table>";
        }

        private static string AddressBox(Order order)
        {
            var interior = order.Interior is not null ? $", {order.Interior}" : "";
            var phone = order.Phone is not null ? $"<br/>Tel: {order.Phone}" : "";
            return $@"<div style='background:#f9f9f9;border-radius:8px;padding:16px;margin:16px 0;'>
  <p style='margin:0 0 4px 0;font-weight:bold;color:#111;'>📍 Dirección de entrega</p>
  <p style='margin:0;color:#555;font-size:14px;'>
    {order.Street}{interior}<br/>
    {order.City}, {order.State} {order.ZipCode}<br/>
    {order.Country}{phone}
  </p>
</div>";
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
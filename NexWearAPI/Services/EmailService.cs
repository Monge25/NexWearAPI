using MailKit.Security;
using MimeKit;
using MailKit.Net.Smtp;

namespace NexWearAPI.Services
{
    public interface IEmailService
    {
        Task SendPasswordResetCodeAsync(string toEmail, string firstName, string code);
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

        public async Task SendPasswordResetCodeAsync(string toEmail, string firstName, string code)
        {
            var message = new MimeMessage();

            message.From.Add(new MailboxAddress(
                _config["Email:FromName"]!,
                _config["Email:FromEmail"]!
            ));

            message.To.Add(new MailboxAddress(firstName, toEmail));
            message.Subject = "Código de recuperación — NexWear";

            message.Body = new TextPart("html")
            {
                Text = $"""
                <!DOCTYPE html>
                <html>
                <body style="font-family: Arial, sans-serif; max-width: 480px;
                             margin: 0 auto; padding: 24px;">
                    <h2 style="color: #111;">Hola, {firstName} 👋</h2>
                    <p style="color: #555;">
                        Recibimos una solicitud para restablecer la contraseña
                        de tu cuenta en NexWear.
                    </p>
                    <p style="color: #555;">Tu código de recuperación es:</p>
                    <div style="
                        background: #f5f5f5;
                        border-radius: 8px;
                        padding: 24px;
                        text-align: center;
                        margin: 24px 0;
                    ">
                        <span style="
                            font-size: 36px;
                            font-weight: bold;
                            letter-spacing: 8px;
                            color: #111;
                        ">{code}</span>
                    </div>
                    <p style="color: #888; font-size: 13px;">
                        Este código expira en <strong>15 minutos</strong>.
                        Si no solicitaste este cambio, ignora este mensaje.
                    </p>
                    <hr style="border: none; border-top: 1px solid #e8e8e8;
                               margin: 24px 0;" />
                    <p style="color: #bbb; font-size: 12px; text-align: center;">
                        NexWear · Tu tienda de moda
                    </p>
                </body>
                </html>
            """
            };

            try
            {
                using var client = new SmtpClient();

                await client.ConnectAsync(
                    _config["Email:Host"]!,
                    int.Parse(_config["Email:Port"]!),
                    SecureSocketOptions.StartTls
                );

                await client.AuthenticateAsync(
                    _config["Email:Username"]!,
                    _config["Email:Password"]!
                );

                await client.SendAsync(message);
                await client.DisconnectAsync(true);

                _logger.LogInformation("Email enviado a {Email}", toEmail);
            }
            catch (Exception ex)
            {
                _logger.LogError("Error completo: {Error} | Inner: {Inner}",
                    ex.Message, ex.InnerException?.Message);
                throw;
            }
            //catch (Exception ex)
            //{
            //    _logger.LogError("Error al enviar email a {Email}: {Error}", toEmail, ex.Message);
            //    throw;
            //}
        }
    }
}

using Resend;

namespace NexWearAPI.Services
{
    public interface IEmailService
    {
        Task SendPasswordResetCodeAsync(string toEmail, string firstName, string code);
    }

    public class EmailService : IEmailService
    {
        private readonly IResend _resend;
        private readonly IConfiguration _config;

        public EmailService(IResend resend, IConfiguration config)
        {
            _resend = resend;
            _config = config;
        }

        public async Task SendPasswordResetCodeAsync(string toEmail, string firstName, string code)
        {
            var fromEmail = _config["Resend:FromEmail"]!;
            var fromName = _config["Resend:FromName"]!;

            var message = new EmailMessage
            {
                From = $"{fromName} <{fromEmail}>",
                To = { toEmail },
                Subject = "Código de recuperación — NexWear",
                HtmlBody = $"""
                <!DOCTYPE html>
                <html>
                <body style="font-family: Arial, sans-serif; max-width: 480px; margin: 0 auto; padding: 24px;">
                    <h2 style="color: #111;">Hola, {firstName} 👋</h2>
                    <p style="color: #555;">
                        Recibimos una solicitud para restablecer la contraseña de tu cuenta en NexWear.
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
                    <hr style="border: none; border-top: 1px solid #e8e8e8; margin: 24px 0;" />
                    <p style="color: #bbb; font-size: 12px; text-align: center;">
                        NexWear · Tu tienda de moda
                    </p>
                </body>
                </html>
            """
            };

            await _resend.EmailSendAsync(message);
        }
    }
}

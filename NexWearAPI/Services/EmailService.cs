using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace NexWearAPI.Services;

public interface IEmailService
{
    Task SendPasswordResetCodeAsync(string toEmail, string firstName, string code);
}

public class EmailService : IEmailService
{
    private readonly IConfiguration _config;
    private readonly ILogger<EmailService> _logger;
    private readonly HttpClient _httpClient;

    public EmailService(IConfiguration config, ILogger<EmailService> logger, IHttpClientFactory httpClientFactory)
    {
        _config = config;
        _logger = logger;
        _httpClient = httpClientFactory.CreateClient();
    }

    public async Task SendPasswordResetCodeAsync(string toEmail, string firstName, string code)
    {
        var apiKey = _config["Brevo:ApiKey"]!;
        var fromEmail = _config["Brevo:FromEmail"]!;
        var fromName = _config["Brevo:FromName"]!;

        var body = new
        {
            sender = new { name = fromName, email = fromEmail },
            to = new[] { new { email = toEmail, name = firstName } },
            subject = "Código de recuperación — NexWear",
            htmlContent = $"""
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

        var json = JsonSerializer.Serialize(body);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        _httpClient.DefaultRequestHeaders.Clear();
        _httpClient.DefaultRequestHeaders.Add("api-key", apiKey);
        _httpClient.DefaultRequestHeaders.Add("Accept", "application/json");

        var response = await _httpClient.PostAsync(
            "https://api.brevo.com/v3/smtp/email", content);

        var responseBody = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("Error Brevo API: {Status} - {Body}",
                response.StatusCode, responseBody);
            throw new InvalidOperationException($"Error al enviar email: {responseBody}");
        }

        _logger.LogInformation("✅ Email enviado a {Email} via Brevo API", toEmail);
    }
}
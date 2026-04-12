using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace NexWearAPI.Services
{
    public interface IPayPalService
    {
        Task<string> CreateOrderAsync(decimal amount, string currency = "MXN");
        Task<PayPalCaptureResult> CaptureOrderAsync(string paypalOrderId);
    }

    public record PayPalCaptureResult(bool Success, string CaptureId, string Status);

    public class PayPalService : IPayPalService
    {
        private readonly IConfiguration _config;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<PayPalService> _logger;

        public PayPalService(
            IConfiguration config,
            IHttpClientFactory httpClientFactory,
            ILogger<PayPalService> logger)
        {
            _config = config;
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        private string BaseUrl => _config["PayPal:IsSandbox"] == "true"
            ? "https://api-m.sandbox.paypal.com"
            : "https://api-m.paypal.com";

        // ── Access Token ──────────────────────────────────────────────────────────

        private async Task<string> GetAccessTokenAsync()
        {
            var client = _httpClientFactory.CreateClient();

            var clientId = _config["PayPal:ClientId"]
                ?? throw new InvalidOperationException("PayPal:ClientId no configurado.");
            var secret = _config["PayPal:Secret"]
                ?? throw new InvalidOperationException("PayPal:Secret no configurado.");

            var credentials = Convert.ToBase64String(
                Encoding.UTF8.GetBytes($"{clientId}:{secret}"));

            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Basic", credentials);

            var content = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("grant_type", "client_credentials")
            });

            var response = await client.PostAsync($"{BaseUrl}/v1/oauth2/token", content);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            return doc.RootElement.GetProperty("access_token").GetString()!;
        }

        private async Task<HttpClient> GetAuthorizedClientAsync()
        {
            var token = await GetAccessTokenAsync();
            var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);
            client.DefaultRequestHeaders.Accept.Add(
                new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
            return client;
        }

        // ── Crear orden en PayPal ─────────────────────────────────────────────────

        public async Task<string> CreateOrderAsync(decimal amount, string currency = "MXN")
        {
            var client = await GetAuthorizedClientAsync();

            var body = new
            {
                intent = "CAPTURE",
                purchase_units = new[]
                {
                    new
                    {
                        amount = new
                        {
                            currency_code = currency,
                            value = amount.ToString("F2")
                        }
                    }
                }
            };

            var json = JsonSerializer.Serialize(body);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await client.PostAsync($"{BaseUrl}/v2/checkout/orders", content);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                _logger.LogError("Error al crear orden PayPal: {Error}", error);
                throw new InvalidOperationException("No se pudo crear la orden en PayPal.");
            }

            var responseJson = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(responseJson);
            return doc.RootElement.GetProperty("id").GetString()!;
        }

        // ── Capturar orden (cobrar dinero) ────────────────────────────────────────

        public async Task<PayPalCaptureResult> CaptureOrderAsync(string paypalOrderId)
        {
            var client = await GetAuthorizedClientAsync();

            var response = await client.PostAsync(
                $"{BaseUrl}/v2/checkout/orders/{paypalOrderId}/capture",
                new StringContent("{}", Encoding.UTF8, "application/json"));

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                _logger.LogError("Error al capturar orden PayPal {OrderId}: {Error}",
                    paypalOrderId, error);
                return new PayPalCaptureResult(false, string.Empty, "ERROR");
            }

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);

            var status = doc.RootElement.GetProperty("status").GetString() ?? "";

            // Obtener capture ID desde purchase_units[0].payments.captures[0].id
            var captureId = doc.RootElement
                .GetProperty("purchase_units")[0]
                .GetProperty("payments")
                .GetProperty("captures")[0]
                .GetProperty("id")
                .GetString() ?? "";

            return new PayPalCaptureResult(status == "COMPLETED", captureId, status);
        }
    }
}
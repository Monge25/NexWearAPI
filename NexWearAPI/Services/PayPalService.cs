using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace NexWearAPI.Services
{
    public interface IPayPalService
    {
        Task<bool> VerifyOrderAsync(string paypalOrderId, decimal expectedTotal);
    }

    public class PayPalService : IPayPalService
    {
        private readonly IConfiguration _config;
        private readonly IHttpClientFactory _httpClientFactory;

        public PayPalService(IConfiguration config, IHttpClientFactory httpClientFactory)
        {
            _config = config;
            _httpClientFactory = httpClientFactory;
        }

        private string BaseUrl => _config["PayPal:Mode"] == "live"
            ? "https://api-m.paypal.com"
            : "https://api-m.sandbox.paypal.com";

        // ── Obtener access token de PayPal ───────────────────────
        private async Task<string> GetAccessTokenAsync()
        {
            var client = _httpClientFactory.CreateClient();
            var clientId = _config["PayPal:ClientId"]!;
            var secret = _config["PayPal:Secret"]!;

            var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{clientId}:{secret}"));
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Basic", credentials);

            var content = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("grant_type", "client_credentials")
            });

            var response = await client.PostAsync($"{BaseUrl}/v1/oauth2/token", content);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            var doc = JsonDocument.Parse(json);
            return doc.RootElement.GetProperty("access_token").GetString()!;
        }

        // ── Verificar la orden con PayPal ────────────────────────
        public async Task<bool> VerifyOrderAsync(string paypalOrderId, decimal expectedTotal)
        {
            try
            {
                var token = await GetAccessTokenAsync();
                var client = _httpClientFactory.CreateClient();
                client.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", token);

                var response = await client.GetAsync(
                    $"{BaseUrl}/v2/checkout/orders/{paypalOrderId}");

                if (!response.IsSuccessStatusCode) return false;

                var json = await response.Content.ReadAsStringAsync();
                var doc = JsonDocument.Parse(json);

                // Verificar que el status sea COMPLETED (capturado)
                var status = doc.RootElement.GetProperty("status").GetString();
                if (status != "COMPLETED") return false;

                // Verificar que el monto sea el correcto (evitar manipulación)
                var amount = doc.RootElement
                    .GetProperty("purchase_units")[0]
                    .GetProperty("payments")
                    .GetProperty("captures")[0]
                    .GetProperty("amount")
                    .GetProperty("value")
                    .GetString();

                if (!decimal.TryParse(amount,
                    System.Globalization.NumberStyles.Any,
                    System.Globalization.CultureInfo.InvariantCulture,
                    out var paidAmount))
                    return false;

                // Tolerancia de 1 centavo por redondeo
                return Math.Abs(paidAmount - expectedTotal) < 0.02m;
            }
            catch
            {
                return false;
            }
        }
    }
}
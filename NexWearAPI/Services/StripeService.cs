using Stripe;

namespace NexWearAPI.Services
{
    public interface IStripeService
    {
        Task<string> CreatePaymentAsync(decimal amount, string description, string paymentMethodId, string payerEmail);
        Task<StripePaymentResult> GetPaymentAsync(string paymentIntentId);
    }

    public record StripePaymentResult(bool Success, string PaymentId, string Status);

    public class StripeService : IStripeService
    {
        private readonly ILogger<StripeService> _logger;

        public StripeService(IConfiguration config, ILogger<StripeService> logger)
        {
            _logger = logger;
            StripeConfiguration.ApiKey = config["Stripe:SecretKey"]
                ?? throw new InvalidOperationException("Stripe:SecretKey no configurado.");
        }

        public async Task<string> CreatePaymentAsync(decimal amount, string description, string paymentMethodId, string payerEmail)
        {
            var options = new PaymentIntentCreateOptions
            {
                Amount = (long)(amount * 100), // Stripe usa centavos
                Currency = "mxn",
                Description = description,
                PaymentMethod = paymentMethodId,
                ReceiptEmail = payerEmail,
                Confirm = true,                // Confirmar de inmediato
                AutomaticPaymentMethods = new PaymentIntentAutomaticPaymentMethodsOptions
                {
                    Enabled = true,
                    AllowRedirects = "never"   // Para pagos de tarjeta sin redireccionamiento
                }
            };

            var service = new PaymentIntentService();
            PaymentIntent intent = await service.CreateAsync(options);

            _logger.LogInformation("Stripe PaymentIntent: {Id} - Status: {Status}", intent.Id, intent.Status);

            if (intent.Status != "succeeded")
                throw new InvalidOperationException($"Pago no completado. Estado: {intent.Status}");

            return intent.Id;
        }

        public async Task<StripePaymentResult> GetPaymentAsync(string paymentIntentId)
        {
            var service = new PaymentIntentService();
            PaymentIntent intent = await service.GetAsync(paymentIntentId);

            bool success = intent.Status == "succeeded";
            return new StripePaymentResult(success, intent.Id, intent.Status);
        }
    }
}
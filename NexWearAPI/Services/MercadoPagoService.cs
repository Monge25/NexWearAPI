using MercadoPago.Client.Payment;
using MercadoPago.Config;
using MercadoPago.Resource.Payment;

namespace NexWearAPI.Services
{
    public interface IMercadoPagoService
    {
        Task<string> CreatePaymentAsync(decimal amount, string description, string token, string payerEmail, string paymentMethodId = "visa");
        Task<MpPaymentResult> GetPaymentAsync(long paymentId);
    }

    public record MpPaymentResult(bool Success, string PaymentId, string Status);

    public class MercadoPagoService : IMercadoPagoService
    {
        private readonly ILogger<MercadoPagoService> _logger;

        public MercadoPagoService(IConfiguration config, ILogger<MercadoPagoService> logger)
        {
            _logger = logger;
            MercadoPagoConfig.AccessToken = config["MercadoPago:AccessToken"]
                ?? throw new InvalidOperationException("MercadoPago:AccessToken no configurado.");
        }

        public async Task<string> CreatePaymentAsync(decimal amount, string description, string token, string payerEmail, string paymentMethodId = "visa")
        {
            var client = new PaymentClient();
            var request = new PaymentCreateRequest
            {
                TransactionAmount = amount,
                Description = description,
                Token = token,
                Installments = 1,
                PaymentMethodId = "visa",   // ← agregar esto
                Payer = new PaymentPayerRequest
                {
                    Email = payerEmail
                }
            };

            Payment payment = await client.CreateAsync(request);

            _logger.LogInformation("Pago MP: {Id} - Status: {Status} - Detail: {Detail}",
                payment.Id, payment.Status, payment.StatusDetail);

            // Si fue rechazado lanzar excepción con detalle
            if (payment.Status != "approved" && payment.Status != "in_process")
                throw new InvalidOperationException(
                    $"Pago rechazado: {payment.StatusDetail ?? payment.Status}");

            return payment.Id.ToString()!;
        }

        public async Task<MpPaymentResult> GetPaymentAsync(long paymentId)
        {
            var client = new PaymentClient();
            Payment payment = await client.GetAsync(paymentId);

            bool success = payment.Status == "approved";
            return new MpPaymentResult(success, payment.Id.ToString()!, payment.Status ?? "");
        }
    }
}
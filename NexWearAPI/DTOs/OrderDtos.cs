using System.ComponentModel.DataAnnotations;

namespace NexWearAPI.DTOs
{
    // ── Request: datos del pago desde el frontend ─────────────────────────────────
    public class MpCheckoutDto
    {
        [Required]
        public string Token { get; set; } = string.Empty; // Token de tarjeta generado por MP.js

        [Required]
        public string ShippingAddress { get; set; } = string.Empty;
    }

    // ── Response: detalle de un item dentro de la orden ───────────────────────────
    public class OrderItemResponseDto
    {
        public Guid Id { get; set; }
        public Guid ProductId { get; set; }
        public Guid VariantId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string? VariantColor { get; set; }
        public string? VariantSize { get; set; }
        public string? ImageUrl { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal Total { get; set; }
    }

    // ── Response: orden completa ──────────────────────────────────────────────────
    public class OrderResponseDto
    {
        public Guid Id { get; set; }
        public string OrderNumber { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public decimal Total { get; set; }
        public string ShippingAddress { get; set; } = string.Empty;
        public string PaymentMethod { get; set; } = string.Empty;
        public string? PaypalOrderId { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? PaidAt { get; set; }
        public IEnumerable<OrderItemResponseDto> Items { get; set; } = new List<OrderItemResponseDto>();
    }
}
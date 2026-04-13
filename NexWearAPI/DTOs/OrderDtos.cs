using System.ComponentModel.DataAnnotations;

namespace NexWearAPI.DTOs
{
    // ── Request: datos del pago desde el frontend ─────────────────────────────────
    public class MpCheckoutDto
    {
        [Required]
        public string Token { get; set; } = string.Empty;
        public string? PaymentMethodId { get; set; }

        // ── Dirección ─────────────────────────────────────────
        // Opción A: dirección guardada
        public Guid? AddressId { get; set; }

        // Opción B: dirección nueva
        public string? Street { get; set; }
        public string? Interior { get; set; }
        public string? City { get; set; }
        public string? State { get; set; }
        public string? ZipCode { get; set; }
        public string? Country { get; set; }
        public string? Phone { get; set; }

        // Guardar dirección nueva para después
        public bool SaveAddress { get; set; } = false;
        public string? AddressAlias { get; set; }
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
        public DateTime CreatedAt { get; set; }
        public DateTime? PaidAt { get; set; }

        // Dirección
        public string Street { get; set; } = string.Empty;
        public string? Interior { get; set; }
        public string City { get; set; } = string.Empty;
        public string State { get; set; } = string.Empty;
        public string ZipCode { get; set; } = string.Empty;
        public string Country { get; set; } = string.Empty;
        public string? Phone { get; set; }

        // Dirección formateada para mostrar en el front
        public string FullAddress =>
            $"{Street}{(Interior != null ? $", {Interior}" : "")}, {City}, {State} {ZipCode}, {Country}";

        public IEnumerable<OrderItemResponseDto> Items { get; set; } = new List<OrderItemResponseDto>();
    }
}
using System.ComponentModel.DataAnnotations;

namespace NexWearAPI.DTOs
{
    public class CheckoutDto
    {
        // Opción A: usar dirección guardada
        public Guid? AddressId { get; set; }

        // Opción B: escribir dirección nueva al momento
        public string? Street { get; set; }
        public string? Interior { get; set; }
        public string? City { get; set; }
        public string? State { get; set; }
        public string? ZipCode { get; set; }
        public string? Country { get; set; }
        public string? Phone { get; set; }

        // Guardar como nueva dirección si es nueva
        public bool SaveAddress { get; set; } = false;
        public string? AddressAlias { get; set; }
    }

    // ── Response ─────────────────────────────────────────────────
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

    public class OrderResponseDto
    {
        public Guid Id { get; set; }
        public string OrderNumber { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public decimal Total { get; set; }
        // Dirección como campos separados
        public string Street { get; set; } = string.Empty;
        public string? Interior { get; set; }
        public string City { get; set; } = string.Empty;
        public string State { get; set; } = string.Empty;
        public string ZipCode { get; set; } = string.Empty;
        public string Country { get; set; } = string.Empty;
        public string? Phone { get; set; }

        // Dirección formateada para mostrar fácil en el front
        public string FullAddress =>
            $"{Street}{(Interior != null ? $", {Interior}" : "")}, {City}, {State} {ZipCode}, {Country}";
        public DateTime CreatedAt { get; set; }
        public IEnumerable<OrderItemResponseDto> Items { get; set; } = new List<OrderItemResponseDto>();
    }
}

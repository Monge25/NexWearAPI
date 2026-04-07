using System.ComponentModel.DataAnnotations;

namespace NexWearAPI.DTOs
{
    public class AddToCartDto
    {
        [Required]
        public Guid ProductId { get; set; }

        [Required]
        public Guid VariantId { get; set; }

        [Range(1, 99)]
        public int Quantity { get; set; } = 1;
    }

    public class UpdateCartItemDto
    {
        [Range(1, 99)]
        public int Quantity { get; set; }
    }

    // ── Response ─────────────────────────────────────────────────
    public class CartItemResponseDto
    {
        public Guid Id { get; set; }
        public Guid ProductId { get; set; }
        public Guid VariantId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string? ImageUrl { get; set; }
        public string? Color { get; set; }
        public string? ColorHex { get; set; }
        public string? Size { get; set; }
        public decimal UnitPrice { get; set; }
        public int Quantity { get; set; }
        public decimal Subtotal { get; set; }
        public int Stock { get; set; }
    }

    public class CartResponseDto
    {
        public IEnumerable<CartItemResponseDto> Items { get; set; } = new List<CartItemResponseDto>();
        public int TotalItems { get; set; }
        public decimal Total { get; set; }
    }
}

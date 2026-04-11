using System.ComponentModel.DataAnnotations;

namespace NexWearAPI.DTOs
{
    // ── Request: Crear variante ──────────────────────────────────
    public class CreateVariantDto
    {
        [MaxLength(50)]
        public string? Color { get; set; }

        [MaxLength(7)]
        public string? ColorHex { get; set; }

        [MaxLength(20)]
        public string? Size { get; set; }

        public decimal PriceModifier { get; set; } = 0;

        [Required]
        [Range(0, int.MaxValue, ErrorMessage = "El stock no puede ser negativo")]
        public int Stock { get; set; }
        public bool IsOnSale { get; set; } = false;
        public decimal? SalePrice { get; set; }
    }

    // ── Request: Actualizar variante ─────────────────────────────
    public class UpdateVariantDto
    {
        [MaxLength(50)]
        public string? Color { get; set; }

        [MaxLength(7)]
        public string? ColorHex { get; set; }

        [MaxLength(20)]
        public string? Size { get; set; }

        public decimal? PriceModifier { get; set; }

        [Range(0, int.MaxValue)]
        public int? Stock { get; set; }

        public bool? IsActive { get; set; }
        public bool? IsOnSale { get; set; } = false;
        public decimal? SalePrice { get; set; }
    }

    // ── Response: Variante ───────────────────────────────────────
    public class VariantResponseDto
    {
        public Guid Id { get; set; }
        public string? Color { get; set; }
        public string? ColorHex { get; set; }
        public string? Size { get; set; }
        public decimal PriceModifier { get; set; }
        public decimal FinalPrice { get; set; }  // base_price + price_modifier
        public int Stock { get; set; }
        public string? ImageUrl { get; set; }
        public bool IsActive { get; set; }
        public bool IsOnSale { get; set; }
        public decimal? SalePrice { get; set; }
        public decimal DisplayPrice { get; set; }
    }
}

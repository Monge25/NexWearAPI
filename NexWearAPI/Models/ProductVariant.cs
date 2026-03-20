using System.ComponentModel.DataAnnotations;

namespace NexWearAPI.Models
{
    public class ProductVariant
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public Guid ProductId { get; set; }

        [MaxLength(50)]
        public string? Color { get; set; }        // "Rojo", "Negro", "Azul"

        [MaxLength(7)]
        public string? ColorHex { get; set; }     // "#FF0000", "#0A0A0A"

        [MaxLength(20)]
        public string? Size { get; set; }         // "XS", "S", "M", "L", "XL" / "38", "40"

        public decimal PriceModifier { get; set; } = 0;  // + o - sobre el precio base

        public int Stock { get; set; } = 0;

        [MaxLength(500)]
        public string? ImageUrl { get; set; }     // Imagen específica de este color

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navegación
        public Product Product { get; set; } = null!;
    }
}

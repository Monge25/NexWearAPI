using System.ComponentModel.DataAnnotations;

namespace NexWearAPI.DTOs
{
    // ── Request: Crear producto ──────────────────────────────────
    public class CreateProductDto
    {
        [Required(ErrorMessage = "El nombre del producto es obligatorio")]
        [MaxLength(255)]
        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }

        [Required(ErrorMessage = "La cantidad del precio es obligatorio")]
        [Range(0.01, 999999.99, ErrorMessage = "El precio debe ser mayor a 0")]
        public decimal Price { get; set; }

        [Required(ErrorMessage = "El stock es obligatorio")]
        [Range(0, int.MaxValue, ErrorMessage = "El stock no puede ser negativo")]
        public int Stock { get; set; }

        [MaxLength(20)]
        public string? Size { get; set; }

        [MaxLength(50)]
        public string? Color { get; set; }

        [MaxLength(500)]
        public string? ImageUrl { get; set; }

        [Required(ErrorMessage = "La categoría es obligatoria")]
        [MaxLength(100)]
        public string Category { get; set; } = string.Empty;
    }

    // ── Request: Actualizar producto ─────────────────────────────
    public class UpdateProductDto
    {
        [MaxLength(255)]
        public string? Name { get; set; }

        public string? Description { get; set; }

        [Range(0.01, 999999.99, ErrorMessage = "El precio debe ser mayor a 0")]
        public decimal? Price { get; set; }

        [Range(0, int.MaxValue, ErrorMessage = "El stock no puede ser negativo")]
        public int? Stock { get; set; }

        [MaxLength(20)]
        public string? Size { get; set; }

        [MaxLength(50)]
        public string? Color { get; set; }

        [MaxLength(500)]
        public string? ImageUrl { get; set; }

        [MaxLength(100)]
        public string? Category { get; set; }

        public bool? IsActive { get; set; }
    }

    // ── Response: Producto ───────────────────────────────────────
    // A04 - Nunca exponemos la entidad directamente, usamos un DTO
    public class ProductResponseDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public decimal Price { get; set; }
        public int Stock { get; set; }
        public string? Size { get; set; }
        public string? Color { get; set; }
        public string? ImageUrl { get; set; }
        public string Category { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}

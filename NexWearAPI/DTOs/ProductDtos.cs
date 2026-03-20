using System.ComponentModel.DataAnnotations;

namespace NexWearAPI.DTOs
{
    // ── Request: Crear producto ──────────────────────────────────
    // ── Request: Crear producto ──────────────────────────────────
    public class CreateProductDto
    {
        [Required(ErrorMessage = "El nombre del producto es obligatorio")]
        [MaxLength(255)]
        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }

        [Required(ErrorMessage = "El precio es obligatorio")]
        [Range(0.01, 999999.99, ErrorMessage = "El precio debe ser mayor a 0")]
        public decimal Price { get; set; }          // Precio base, las variantes lo ajustan

        [MaxLength(500)]
        public string? ImageUrl { get; set; }       // Imagen principal del producto

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

        [MaxLength(500)]
        public string? ImageUrl { get; set; }

        [MaxLength(100)]
        public string? Category { get; set; }

        public bool? IsActive { get; set; }
    }

    // ── Response: Producto simple (sin variantes) ─────────────────
    // A04 - Nunca exponemos la entidad directamente
    public class ProductResponseDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public decimal Price { get; set; }
        public string? ImageUrl { get; set; }
        public string Category { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    // ── Response: Producto con variantes incluidas ────────────────
    // Se usa cuando el frontend necesita mostrar colores/tallas disponibles
    public class ProductWithVariantsDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public decimal BasePrice { get; set; }
        public string? ImageUrl { get; set; }
        public string Category { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }

        // Variantes agrupadas para el frontend
        public IEnumerable<VariantResponseDto> Variants { get; set; } = new List<VariantResponseDto>();
    }
}

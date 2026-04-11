using System.ComponentModel.DataAnnotations;

namespace NexWearAPI.DTOs
{
    // ── Request: Crear reseña ────────────────────────────────────
    public class CreateReviewDto
    {
        [Required]
        public Guid OrderId { get; set; }       // para verificar compra

        [Required]
        public Guid ProductId { get; set; }

        [Required]
        [Range(1, 5, ErrorMessage = "El rating debe ser entre 1 y 5")]
        public int Rating { get; set; }

        [MaxLength(1000)]
        public string? Comment { get; set; }
    }

    // ── Request: Editar reseña ────────────────────────────────────
    public class UpdateReviewDto
    {
        [Required]
        [Range(1, 5, ErrorMessage = "El rating debe ser entre 1 y 5")]
        public int? Rating { get; set; }

        [MaxLength(1000)]
        public string? Comment { get; set; }
    }

    // ── Request: Moderación (solo Admin) ─────────────────────────
    public class ModerateReviewDto
    {
        [Required]
        public bool Approved { get; set; }     // true = aprobar, false = rechazar
    }

    // ── Response ─────────────────────────────────────────────────
    public class ReviewResponseDto
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public Guid ProductId { get; set; }
        public int Rating { get; set; }
        public string? Comment { get; set; }
        public List<string> PhotoUrls { get; set; } = new();
        public bool IsApproved { get; set; }
        public bool IsVerifiedPurchase { get; set; } = true;
        public DateTime CreatedAt { get; set; }
    }

    // ── Response: Resumen de ratings de un producto ──────────────
    public class ProductRatingSummaryDto
    {
        public double Average { get; set; }
        public int Total { get; set; }
        public Dictionary<int, int> Distribution { get; set; } = new();
        // Distribution: { 5: 10, 4: 5, 3: 2, 2: 1, 1: 0 }
    }
}

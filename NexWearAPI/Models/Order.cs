using System.ComponentModel.DataAnnotations;

namespace NexWearAPI.Models
{
    public enum OrderStatus
    {
        Pending,
        Paid,
        Shipped,
        Delivered,
        Cancelled
    }

    public class Order
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public Guid UserId { get; set; }

        public OrderStatus Status { get; set; } = OrderStatus.Pending;

        [Required]
        public decimal Total { get; set; }

        // ── Snapshot de dirección ─────────────────────────────
        [Required]
        [MaxLength(255)]
        public string Street { get; set; } = string.Empty;

        [MaxLength(100)]
        public string? Interior { get; set; }

        [Required]
        [MaxLength(100)]
        public string City { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string State { get; set; } = string.Empty;

        [Required]
        [MaxLength(10)]
        public string ZipCode { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string Country { get; set; } = "México";

        [MaxLength(20)]
        public string? Phone { get; set; }

        // ── Pago ──────────────────────────────────────────────
        [MaxLength(20)]
        public string PaymentMethod { get; set; } = "mercadopago";

        [MaxLength(100)]
        public string? MPOrderId { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? PaidAt { get; set; }

        // Navegación
        public User User { get; set; } = null!;
        public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
    }
}
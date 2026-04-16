using System.ComponentModel.DataAnnotations;

namespace NexWearAPI.Models
{
    public class AuditLog
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        // Quién
        public Guid? UserId { get; set; }
        public string? UserEmail { get; set; }

        // Qué
        [Required]
        [MaxLength(50)]
        public string Action { get; set; } = string.Empty;  // LOGIN, ORDER_CREATED, etc.

        [MaxLength(50)]
        public string Category { get; set; } = string.Empty; // Auth, Order, Admin, Product...

        // Resultado
        [MaxLength(10)]
        public string Result { get; set; } = "SUCCESS";     // SUCCESS | ERROR

        public string? Details { get; set; }                // JSON o texto libre

        // Contexto
        [MaxLength(45)]
        public string? IpAddress { get; set; }

        [MaxLength(255)]
        public string? UserAgent { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navegación opcional
        public User? User { get; set; }
    }
}

using System.ComponentModel.DataAnnotations;

namespace NexWearAPI.Models
{
    public class PasswordResetCode
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public Guid UserId { get; set; }

        [Required]
        [MaxLength(6)]
        public string Code { get; set; } = string.Empty;

        public bool IsUsed { get; set; } = false;

        public DateTime ExpiresAt { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navegación
        public User User { get; set; } = null!;
    }
}

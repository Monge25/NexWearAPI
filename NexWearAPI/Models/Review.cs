using System.ComponentModel.DataAnnotations;

namespace NexWearAPI.Models
{
    public class Review
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public Guid UserId { get; set; }

        [Required]
        public Guid ProductId { get; set; }

        [Required]
        public Guid OrderId { get; set; } 

        [Required]
        [Range(1, 5)]
        public int Rating { get; set; }

        public string? Comment { get; set; }

        public List<string> PhotoUrls { get; set; } = new();

        public bool IsApproved { get; set; } = false;  // ← moderación
        public bool IsRejected { get; set; } = false;  // ← rechazada por admin


        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navegación
        public User User { get; set; } = null!;
        public Product Product { get; set; } = null!;
        public Order Order { get; set; } = null!;
    }
}

using System.ComponentModel.DataAnnotations;

namespace NexWearAPI.Models
{
    public class CartItem
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public Guid UserId { get; set; }

        [Required]
        public Guid ProductId { get; set; }

        public int Quantity { get; set; } = 1;

        public DateTime AddedAt { get; set; } = DateTime.UtcNow;

        // Navegación
        public User User { get; set; } = null!;
        public Product Product { get; set; } = null!;
    }
}

using Microsoft.AspNetCore.Mvc.ViewEngines;
using System.ComponentModel.DataAnnotations;

namespace NexWearAPI.Models
{
    public class Product
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        [MaxLength(255)]
        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }

        [Required]
        public decimal Price { get; set; }

        public int Stock { get; set; } = 0;

        [MaxLength(20)]
        public string? Size { get; set; }       // XS, S, M, L, XL / 38, 40, 42...

        [MaxLength(50)]
        public string? Color { get; set; }

        [MaxLength(500)]
        public string? ImageUrl { get; set; }

        [Required]
        [MaxLength(100)]
        public string Category { get; set; } = string.Empty;  // Ropa, Calzado, Accesorios

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navegación
        public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
        public ICollection<CartItem> CartItems { get; set; } = new List<CartItem>();
        public ICollection<Review> Reviews { get; set; } = new List<Review>();
    }
}

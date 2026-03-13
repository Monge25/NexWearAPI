using System.ComponentModel.DataAnnotations;

namespace NexWearAPI.Models
{
    public class OrderItem
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public Guid OrderId { get; set; }

        [Required]
        public Guid ProductId { get; set; }

        [Required]
        public int Quantity { get; set; }

        [Required]
        public decimal UnitPrice { get; set; }  // Precio al momento de comprar

        // Navegación
        public Order Order { get; set; } = null!;
        public Product Product { get; set; } = null!;
    }
}

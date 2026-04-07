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
        public Guid VariantId { get; set; }

        [Required]
        public int Quantity { get; set; }

        [Required]
        public decimal UnitPrice { get; set; }  // Precio al momento de comprar

        // Snapshot — guardar nombre, color y talla al momento de comprar
        // Si el producto cambia después, la orden queda intacta
        [MaxLength(255)]
        public string ProductName { get; set; } = string.Empty; 

        [MaxLength(50)]
        public string? VariantColor { get; set; }               

        [MaxLength(20)]
        public string? VariantSize { get; set; }                

        // Navegación
        public Order Order { get; set; } = null!;
        public Product Product { get; set; } = null!;
        public ProductVariant Variant { get; set; } = null!;
    }
}

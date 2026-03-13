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

        [Required]
        public string ShippingAddress { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navegación
        public User User { get; set; } = null!;
        public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
    }
}

using System.ComponentModel.DataAnnotations;

namespace NexWearAPI.Models
{
    public class Address
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public Guid UserId { get; set; }

        [Required]
        [MaxLength(50)]
        public string Alias { get; set; } = string.Empty;  // "Casa", "Trabajo", etc.

        [Required]
        [MaxLength(255)]
        public string Street { get; set; } = string.Empty; // Calle y número

        [MaxLength(100)]
        public string? Interior { get; set; }              // Depto, piso, etc.

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
        public string? Phone { get; set; }                 // Teléfono de contacto

        public bool IsDefault { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navegación
        public User User { get; set; } = null!;
    }
}

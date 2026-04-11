using System.ComponentModel.DataAnnotations;

namespace NexWearAPI.DTOs
{
    // ── Request: Crear / Editar ───────────────────────────────────
    public class CreateAddressDto
    {
        [Required(ErrorMessage = "El alias es obligatorio")]
        [MaxLength(50)]
        public string Alias { get; set; } = string.Empty;

        [Required(ErrorMessage = "La calle es obligatoria")]
        [MaxLength(255)]
        public string Street { get; set; } = string.Empty;

        [MaxLength(100)]
        public string? Interior { get; set; }

        [Required(ErrorMessage = "La ciudad es obligatoria")]
        [MaxLength(100)]
        public string City { get; set; } = string.Empty;

        [Required(ErrorMessage = "El estado es obligatorio")]
        [MaxLength(100)]
        public string State { get; set; } = string.Empty;

        [Required(ErrorMessage = "El código postal es obligatorio")]
        [MaxLength(10)]
        public string ZipCode { get; set; } = string.Empty;

        [MaxLength(100)]
        public string Country { get; set; } = "México";

        [MaxLength(20)]
        public string? Phone { get; set; }

        public bool IsDefault { get; set; } = false;
    }

    public class UpdateAddressDto
    {
        [MaxLength(50)]
        public string? Alias { get; set; }

        [MaxLength(255)]
        public string? Street { get; set; }

        [MaxLength(100)]
        public string? Interior { get; set; }

        [MaxLength(100)]
        public string? City { get; set; }

        [MaxLength(100)]
        public string? State { get; set; }

        [MaxLength(10)]
        public string? ZipCode { get; set; }

        [MaxLength(100)]
        public string? Country { get; set; }

        [MaxLength(20)]
        public string? Phone { get; set; }

        public bool? IsDefault { get; set; }
    }

    // ── Response ─────────────────────────────────────────────────
    public class AddressResponseDto
    {
        public Guid Id { get; set; }
        public string Alias { get; set; } = string.Empty;
        public string Street { get; set; } = string.Empty;
        public string? Interior { get; set; }
        public string City { get; set; } = string.Empty;
        public string State { get; set; } = string.Empty;
        public string ZipCode { get; set; } = string.Empty;
        public string Country { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public bool IsDefault { get; set; }

        // Dirección formateada para mostrar en UI
        public string FullAddress =>
            $"{Street}{(Interior != null ? $", {Interior}" : "")}, {City}, {State} {ZipCode}, {Country}";
    }
}

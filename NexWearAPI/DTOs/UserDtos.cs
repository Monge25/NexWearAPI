using System.ComponentModel.DataAnnotations;

namespace NexWearAPI.DTOs
{
    // ── Response: Perfil del usuario ─────────────────────────────
    // A04 - Nunca exponemos la entidad User directamente
    public class UserProfileDto
    {
        public Guid Id { get; set; }
        public string Email { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }

    // ── Request: Editar perfil ───────────────────────────────────
    public class UpdateProfileDto
    {
        [MaxLength(100)]
        public string? FirstName { get; set; }

        [MaxLength(100)]
        public string? LastName { get; set; }

        [EmailAddress(ErrorMessage = "Email inválido")]
        [MaxLength(255)]
        public string? Email { get; set; }
    }

    // ── Request: Cambiar contraseña ──────────────────────────────
    public class ChangePasswordDto
    {
        [Required(ErrorMessage = "La contraseña actual es obligatoria")]
        public string CurrentPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "La nueva contraseña es obligatoria")]
        [MinLength(8, ErrorMessage = "La contraseña debe tener mínimo 8 caracteres")]
        public string NewPassword { get; set; } = string.Empty;
    }
}

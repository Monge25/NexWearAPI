using Microsoft.EntityFrameworkCore;
using NexWearAPI.Data;
using NexWearAPI.DTOs;

namespace NexWearAPI.Services
{
    // ── Interface ────────────────────────────────────────────────
    public interface IUserService
    {
        Task<UserProfileDto?> GetProfileAsync(Guid userId);
        Task<UserProfileDto?> UpdateProfileAsync(Guid userId, UpdateProfileDto dto);
        Task<bool> ChangePasswordAsync(Guid userId, ChangePasswordDto dto);
    }

    // ── Implementación ───────────────────────────────────────────
    public class UserService : IUserService
    {
        private readonly AppDbContext _context;

        public UserService(AppDbContext context)
        {
            _context = context;
        }

        // ── Obtener perfil ────────────────────────────────────────
        public async Task<UserProfileDto?> GetProfileAsync(Guid userId)
        {
            var user = await _context.Users.FindAsync(userId);
            return user is null ? null : MapToDto(user);
        }

        // ── Editar perfil ─────────────────────────────────────────
        public async Task<UserProfileDto?> UpdateProfileAsync(Guid userId, UpdateProfileDto dto)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user is null) return null;

            // Verificar si el nuevo email ya está en uso por otro usuario
            if (dto.Email is not null)
            {
                var emailTaken = await _context.Users
                    .AnyAsync(u => u.Email == dto.Email.ToLower().Trim() && u.Id != userId);

                if (emailTaken) return null;

                user.Email = dto.Email.ToLower().Trim();
            }

            if (dto.FirstName is not null) user.FirstName = dto.FirstName.Trim();
            if (dto.LastName is not null) user.LastName = dto.LastName.Trim();

            await _context.SaveChangesAsync();
            return MapToDto(user);
        }

        // ── Cambiar contraseña ────────────────────────────────────
        // A02 - Verificamos la contraseña actual antes de cambiarla
        public async Task<bool> ChangePasswordAsync(Guid userId, ChangePasswordDto dto)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user is null) return false;

            // Verificar que la contraseña actual sea correcta
            var validPassword = BCrypt.Net.BCrypt.Verify(dto.CurrentPassword, user.PasswordHash);
            if (!validPassword) return false;

            // A02 - Hashear la nueva contraseña con bcrypt
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword, workFactor: 12);
            await _context.SaveChangesAsync();

            return true;
        }

        // ── Mapper ────────────────────────────────────────────────
        private static UserProfileDto MapToDto(Models.User u) => new()
        {
            Id = u.Id,
            Email = u.Email,
            FirstName = u.FirstName,
            LastName = u.LastName,
            Role = u.Role.ToString(),
            CreatedAt = u.CreatedAt
        };
    }
}

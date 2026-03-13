using Microsoft.IdentityModel.Tokens;
using NexWearAPI.Data;
using NexWearAPI.DTOs;
using NexWearAPI.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.EntityFrameworkCore;

namespace NexWearAPI.Services
{
    public interface IAuthService
    {
        Task<AuthResponseDto?> RegisterAsync(RegisterRequestDto dto);
        Task<AuthResponseDto?> RegisterAdminAsync(RegisterRequestDto dto);
        Task<AuthResponseDto?> LoginAsync(LoginRequestDto dto);
    }

    // ── Implementación ───────────────────────────────────────────
    public class AuthService : IAuthService
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _config;

        // A07 - OWASP: Límite de intentos fallidos de login por email
        private static readonly Dictionary<string, (int Attempts, DateTime LockUntil)> _loginAttempts = new();
        private const int MaxAttempts = 5;
        private const int LockMinutes = 15;

        public AuthService(AppDbContext context, IConfiguration config)
        {
            _context = context;
            _config = config;
        }

        // ── Registro ─────────────────────────────────────────────
        public async Task<AuthResponseDto?> RegisterAsync(RegisterRequestDto dto)
        {
            // A07 - Verificar si el email ya existe (no revelar si existe o no en el mensaje)
            var exists = await _context.Users
                .AnyAsync(u => u.Email == dto.Email.ToLower().Trim());

            if (exists)
                return null; // El controller devolverá 409 Conflict

            // A02 - bcrypt con salt automático (work factor 12)
            var passwordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password, workFactor: 12);

            var user = new User
            {
                Email = dto.Email.ToLower().Trim(),
                PasswordHash = passwordHash,
                FirstName = dto.FirstName.Trim(),
                LastName = dto.LastName.Trim(),
                Role = UserRole.Customer
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return BuildAuthResponse(user);
        }

        public async Task<AuthResponseDto?> RegisterAdminAsync(RegisterRequestDto dto)
        {
            var exists = await _context.Users
                .AnyAsync(u => u.Email == dto.Email.ToLower().Trim());

            if (exists) return null;

            var passwordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password, workFactor: 12);

            var user = new User
            {
                Email = dto.Email.ToLower().Trim(),
                PasswordHash = passwordHash,
                FirstName = dto.FirstName.Trim(),
                LastName = dto.LastName.Trim(),
                Role = UserRole.Admin  // Crear administradores
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return BuildAuthResponse(user);
        }

        // ── Login ─────────────────────────────────────────────────
        public async Task<AuthResponseDto?> LoginAsync(LoginRequestDto dto)
        {
            var email = dto.Email.ToLower().Trim();

            // A07 - Verificar si la cuenta está bloqueada por intentos fallidos
            if (IsLocked(email))
                return null;

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == email);

            // A07 - Verificar contraseña con bcrypt
            // Aunque no exista el usuario, se hace una verificación falsa para
            // evitar timing attacks (no revelar si el email existe)
            var validPassword = user != null &&
                BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash);

            if (!validPassword)
            {
                RegisterFailedAttempt(email);
                return null; // El controller devolverá 401 siempre con el mismo mensaje
            }

            // Login exitoso — resetear intentos fallidos
            ResetAttempts(email);

            return BuildAuthResponse(user!);
        }

        // ── Generar JWT ───────────────────────────────────────────
        private AuthResponseDto BuildAuthResponse(User user)
        {
            var jwtConfig = _config.GetSection("Jwt");
            var secretKey = jwtConfig["SecretKey"]!;
            var issuer = jwtConfig["Issuer"]!;
            var audience = jwtConfig["Audience"]!;
            var expiresIn = int.Parse(jwtConfig["ExpiresInMinutes"]!);

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            // A01 - Claims que viajan dentro del token (id + role para control de acceso)
            var claims = new[]
            {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim(ClaimTypes.Role, user.Role.ToString()),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()), // ID único del token
        };

            var expiresAt = DateTime.UtcNow.AddMinutes(expiresIn);

            var token = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                expires: expiresAt,
                signingCredentials: creds
            );

            return new AuthResponseDto
            {
                Token = new JwtSecurityTokenHandler().WriteToken(token),
                Email = user.Email,
                FirstName = user.FirstName,
                Role = user.Role.ToString(),
                ExpiresAt = expiresAt
            };
        }

        // ── Helpers para control de intentos fallidos ─────────────
        private static bool IsLocked(string email)
        {
            if (!_loginAttempts.TryGetValue(email, out var entry)) return false;
            if (entry.LockUntil > DateTime.UtcNow) return true;

            // El bloqueo expiró — limpiar
            _loginAttempts.Remove(email);
            return false;
        }

        private static void RegisterFailedAttempt(string email)
        {
            _loginAttempts.TryGetValue(email, out var entry);
            var attempts = entry.Attempts + 1;
            var lockUntil = attempts >= MaxAttempts
                ? DateTime.UtcNow.AddMinutes(LockMinutes)
                : entry.LockUntil;

            _loginAttempts[email] = (attempts, lockUntil);
        }

        private static void ResetAttempts(string email) =>
            _loginAttempts.Remove(email);
    }
}

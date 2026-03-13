using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NexWearAPI.DTOs;
using NexWearAPI.Services;

namespace NexWearAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly ILogger<AuthController> _logger;

        public AuthController(IAuthService authService, ILogger<AuthController> logger)
        {
            _authService = authService;
            _logger = logger;
        }

        /// <summary>Registrar un nuevo usuario</summary>
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequestDto dto)
        {
            // A08 - Las validaciones del DTO se ejecutan automáticamente
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _authService.RegisterAsync(dto);

            if (result is null)
            {
                // A07 - Mensaje genérico, no revelar si el email ya existe
                return Conflict(new { message = "No fue posible completar el registro." });
            }

            // A09 - Log de registro exitoso
            _logger.LogInformation("Nuevo usuario registrado: {Email} a las {Time}",
                dto.Email, DateTime.UtcNow);

            return CreatedAtAction(nameof(Register), result);
        }

        /// <summary>Crear un usuario administrador (solo Admin)</summary>
        [Authorize(Roles = "Admin")]
        [HttpPost("register-admin")]
        public async Task<IActionResult> RegisterAdmin([FromBody] RegisterRequestDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _authService.RegisterAdminAsync(dto);

            if (result is null)
                return Conflict(new { message = "No fue posible completar el registro." });

            _logger.LogInformation("Nuevo admin registrado: {Email} a las {Time}",
                dto.Email, DateTime.UtcNow);

            return CreatedAtAction(nameof(RegisterAdmin), result);
        }

        /// <summary>Iniciar sesión y obtener JWT</summary>
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequestDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _authService.LoginAsync(dto);

            if (result is null)
            {
                // A09 - Log de intento fallido (sin revelar si el email existe)
                _logger.LogWarning("Intento de login fallido para: {Email} a las {Time}",
                    dto.Email, DateTime.UtcNow);

                // A07 - Siempre el mismo mensaje, nunca revelar si el email existe o no
                return Unauthorized(new { message = "Credenciales inválidas." });
            }

            // A09 - Log de login exitoso
            _logger.LogInformation("Login exitoso: {Email} a las {Time}",
                dto.Email, DateTime.UtcNow);

            return Ok(result);
        }
    }
}

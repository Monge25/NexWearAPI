using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using NexWearAPI.Constants;
using NexWearAPI.DTOs;
using NexWearAPI.Extensions;
using NexWearAPI.Services;

namespace NexWearAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [EnableRateLimiting("auth")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly ILogger<AuthController> _logger;
        private readonly IAuditService _auditService;
        private readonly IServiceScopeFactory _scopeFactory;

        public AuthController(IAuthService authService, ILogger<AuthController> logger, IAuditService auditService, IServiceScopeFactory scopeFactory)
        {
            _authService = authService;
            _logger = logger;
            _auditService = auditService;
            _scopeFactory = scopeFactory;
        }

        /// <summary>Registrar un nuevo usuario</summary>
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequestDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            var result = await _authService.RegisterAsync(dto);
            if (result is null)
            {
                await _auditService.LogAsync(AuditActions.REGISTER, AuditCategories.Auth, "ERROR",
                    new { email = dto.Email, razon = "Email ya registrado" },
                    userEmail: dto.Email, ipAddress: HttpContext.GetIpAddress(), userAgent: HttpContext.GetUserAgent());
                return Conflict(new { message = "No fue posible completar el registro." });
            }
            await _auditService.LogAsync(AuditActions.REGISTER, AuditCategories.Auth, "SUCCESS",
                new { email = dto.Email },
                userId: result.UserId, userEmail: dto.Email,
                ipAddress: HttpContext.GetIpAddress(), userAgent: HttpContext.GetUserAgent());
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
            if (!ModelState.IsValid) return BadRequest(ModelState);
            var result = await _authService.LoginAsync(dto);
            if (result is null)
            {
                await _auditService.LogAsync(AuditActions.LOGIN_FAILED, AuditCategories.Auth, "ERROR",
                    new { email = dto.Email },
                    userEmail: dto.Email, ipAddress: HttpContext.GetIpAddress(), userAgent: HttpContext.GetUserAgent());
                return Unauthorized(new { message = "Credenciales inválidas." });
            }
            await _auditService.LogAsync(AuditActions.LOGIN_SUCCESS, AuditCategories.Auth, "SUCCESS",
                new { email = dto.Email },
                userEmail: dto.Email,
                ipAddress: HttpContext.GetIpAddress(), userAgent: HttpContext.GetUserAgent());
            return Ok(result);
        }

        /// <summary>Solicitar código de recuperación</summary>
        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            // Captura ANTES del Task.Run — HttpContext no es accesible desde otro thread
            var ipAddress = HttpContext.GetIpAddress();
            var userAgent = HttpContext.GetUserAgent();

            // ✅ Scope propio = DbContext propio, sin competencia con el request
            _ = Task.Run(async () =>
            {
                using var scope = _scopeFactory.CreateScope();
                var authService = scope.ServiceProvider.GetRequiredService<IAuthService>();
                try { await authService.ForgotPasswordAsync(dto); }
                catch (Exception ex) { _logger.LogError("Error enviando email: {Error}", ex.Message); }
            });

            // ✅ await para que el contexto del request siga vivo
            await _auditService.LogAsync(
                AuditActions.PASSWORD_RESET_REQUEST,
                AuditCategories.Auth,
                "SUCCESS",
                new { email = dto.Email },
                userEmail: dto.Email,
                ipAddress: ipAddress,
                userAgent: userAgent
            );

            return Ok(new { message = "Si el email está registrado, recibirá un código en breve." });
        }

        /// <summary>Verificar si el código es válido</summary>
        [HttpPost("verify-reset-code")]
        public async Task<IActionResult> VerifyResetCode([FromBody] VerifyResetCodeDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var isValid = await _authService.VerifyResetCodeAsync(dto);

            if (!isValid)
                return BadRequest(new { message = "Código inválido o expirado." });

            return Ok(new { message = "Código válido." });
        }

        /// <summary>Resetear contraseña con el código</summary>
        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            var result = await _authService.ResetPasswordAsync(dto);
            if (!result)
            {
                await _auditService.LogAsync(AuditActions.PASSWORD_RESET_SUCCESS, AuditCategories.Auth, "ERROR",
                    new { email = dto.Email, razon = "Código inválido o expirado" },
                    userEmail: dto.Email, ipAddress: HttpContext.GetIpAddress(), userAgent: HttpContext.GetUserAgent());
                return BadRequest(new { message = "Código inválido o expirado." });
            }
            await _auditService.LogAsync(AuditActions.PASSWORD_RESET_SUCCESS, AuditCategories.Auth, "SUCCESS",
                new { email = dto.Email },
                userEmail: dto.Email, ipAddress: HttpContext.GetIpAddress(), userAgent: HttpContext.GetUserAgent());
            return Ok(new { message = "Contraseña restablecida correctamente." });
        }
    }
}

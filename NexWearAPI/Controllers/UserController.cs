using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NexWearAPI.DTOs;
using NexWearAPI.Models;
using NexWearAPI.Services;
using System.Security.Claims;

namespace NexWearAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]  // Todos los endpoints de este controller requieren token
    public class UsersController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly ILogger<UsersController> _logger;

        public UsersController(IUserService userService, ILogger<UsersController> logger)
        {
            _userService = userService;
            _logger = logger;
        }

        // ── Helper: obtener el ID del usuario desde el token ─────
        // A01 - El usuario solo puede ver/editar SU propio perfil
        // El ID viene del JWT, no del request — el usuario no puede falsificarlo
        private Guid GetUserIdFromToken() =>
            Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? User.FindFirstValue("sub")
                ?? throw new UnauthorizedAccessException());

        /// <summary>Obtener perfil del usuario autenticado</summary>
        [HttpGet("profile")]
        public async Task<IActionResult> GetProfile()
        {
            var userId = GetUserIdFromToken();
            var profile = await _userService.GetProfileAsync(userId);

            if (profile is null)
                return NotFound(new { message = "Usuario no encontrado." });

            return Ok(profile);
        }

        /// <summary>Editar perfil del usuario autenticado</summary>
        [HttpPut("profile")]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var userId = GetUserIdFromToken();
            var profile = await _userService.UpdateProfileAsync(userId, dto);

            if (profile is null)
                return Conflict(new { message = "El email ya está en uso." });

            _logger.LogInformation("Perfil actualizado: {UserId} a las {Time}", userId, DateTime.UtcNow);

            return Ok(profile);
        }

        /// <summary>Cambiar contraseña del usuario autenticado</summary>
        [HttpPut("change-password")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var userId = GetUserIdFromToken();
            var result = await _userService.ChangePasswordAsync(userId, dto);

            if (!result)
                // A07 - Mensaje genérico, no revelar si fue contraseña incorrecta o usuario no encontrado
                return BadRequest(new { message = "No fue posible cambiar la contraseña." });

            _logger.LogInformation("Contraseña cambiada: {UserId} a las {Time}", userId, DateTime.UtcNow);

            return Ok(new { message = "Contraseña actualizada correctamente." });
        }
    }
}

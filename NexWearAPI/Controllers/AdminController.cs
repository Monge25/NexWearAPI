using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NexWearAPI.DTOs;
using NexWearAPI.Services;

namespace NexWearAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin")]
    public class AdminController : ControllerBase
    {
        private readonly IAdminService _adminService;
        private readonly ILogger<AdminController> _logger;

        public AdminController(IAdminService adminService, ILogger<AdminController> logger)
        {
            _adminService = adminService;
            _logger = logger;
        }

        // ════════════════════════════════════════════════════════
        //  USUARIOS
        // ════════════════════════════════════════════════════════

        /// <summary>Listar todos los usuarios</summary>
        [HttpGet("users")]
        public async Task<IActionResult> GetUsers(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] string? search = null,
            [FromQuery] string? role = null)
        {
            page = Math.Max(1, page);
            pageSize = Math.Clamp(pageSize, 1, 100);

            var result = await _adminService.GetUsersAsync(page, pageSize, search, role);
            return Ok(result);
        }

        /// <summary>Cambiar rol de un usuario</summary>
        [HttpPatch("users/{userId:guid}/role")]
        public async Task<IActionResult> UpdateUserRole(Guid userId, [FromBody] UpdateUserRoleDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var user = await _adminService.UpdateUserRoleAsync(userId, dto.Role);
            if (user is null)
                return BadRequest(new { message = "Usuario no encontrado o rol inválido." });

            _logger.LogInformation("Rol actualizado: {UserId} → {Role}", userId, dto.Role);
            return Ok(user);
        }

        // ════════════════════════════════════════════════════════
        //  ÓRDENES
        // ════════════════════════════════════════════════════════

        /// <summary>Listar todas las órdenes</summary>
        [HttpGet("orders")]
        public async Task<IActionResult> GetOrders(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] string? status = null)
        {
            page = Math.Max(1, page);
            pageSize = Math.Clamp(pageSize, 1, 100);

            var result = await _adminService.GetOrdersAsync(page, pageSize, status);
            return Ok(result);
        }

        /// <summary>Detalle de una orden específica</summary>
        [HttpGet("orders/{orderId:guid}")]
        public async Task<IActionResult> GetOrderById(Guid orderId)
        {
            var order = await _adminService.GetOrderByIdAsync(orderId);
            if (order is null)
                return NotFound(new { message = "Orden no encontrada." });

            return Ok(order);
        }

        /// <summary>Cambiar estado de una orden</summary>
        [HttpPatch("orders/{orderId:guid}/status")]
        public async Task<IActionResult> UpdateOrderStatus(Guid orderId, [FromBody] UpdateOrderStatusDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var order = await _adminService.UpdateOrderStatusAsync(orderId, dto.Status);
            if (order is null)
                return BadRequest(new { message = "Orden no encontrada o estado inválido." });

            _logger.LogInformation("Estado de orden actualizado: {OrderId} → {Status}",
                orderId, dto.Status);

            return Ok(order);
        }
    }
}

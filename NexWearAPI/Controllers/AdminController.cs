using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NexWearAPI.Constants;
using NexWearAPI.DTOs;
using NexWearAPI.Extensions;
using NexWearAPI.Services;
using System.Security.Claims;

namespace NexWearAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin")]
    public class AdminController : ControllerBase
    {
        private readonly IAdminService _adminService;
        private readonly ILogger<AdminController> _logger;
        private readonly IAuditService _auditService;

        public AdminController(IAdminService adminService, ILogger<AdminController> logger, IAuditService auditService)
        {
            _adminService = adminService;
            _logger = logger;
            _auditService = auditService;
        }

        private Guid GetAdminId() =>
            Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub")!);

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
            if (order is null) return BadRequest(new { message = "Orden no encontrada o estado inválido." });

            await _auditService.LogAsync(AuditActions.ADMIN_ORDER_STATUS_CHANGED, AuditCategories.Admin, "SUCCESS",
                new { orderId, newStatus = dto.Status },
                userId: GetAdminId(), ipAddress: HttpContext.GetIpAddress(), userAgent: HttpContext.GetUserAgent());
            return Ok(order);
        }

        /// <summary>Logs de auditoria del sistema (solo Admin)</summary>
        [HttpGet("audit-logs")]
        public async Task<IActionResult> GetAuditLogs(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 50,
            [FromQuery] string? action = null,
            [FromQuery] string? category = null,
            [FromQuery] string? result = null,
            [FromQuery] string? search = null,
            [FromQuery] DateTime? from = null,
            [FromQuery] DateTime? to = null)
        {
            page = Math.Max(1, page);
            pageSize = Math.Clamp(pageSize, 1, 200);

            var logs = await _adminService.GetAuditLogsAsync(
                page, pageSize, action, category, result, search, from, to);

            return Ok(logs);
        }
    }
}

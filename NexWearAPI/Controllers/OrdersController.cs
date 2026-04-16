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
    [Authorize]
    public class OrdersController : ControllerBase
    {
        private readonly IOrderService _orderService;
        private readonly ILogger<OrdersController> _logger;
        private readonly IAuditService _auditService;

        public OrdersController(IOrderService orderService, ILogger<OrdersController> logger, IAuditService auditService)
        {
            _orderService = orderService;
            _logger = logger;
            _auditService = auditService;
        }

        private Guid GetUserId() =>
            Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? User.FindFirstValue("sub")!);

        // ── Checkout ─────────────────────────────────────────────

        /// <summary>
        /// Procesa el pago y registra la orden.
        /// El frontend manda el token de tarjeta generado y la dirección.
        /// </summary>
        [HttpPost("checkout")]
        public async Task<IActionResult> Checkout([FromBody] StripeCheckoutDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            try
            {
                var order = await _orderService.CheckoutAsync(GetUserId(), dto);
                await _auditService.LogAsync(AuditActions.ORDER_CREATED, AuditCategories.Order, "SUCCESS",
                    new { orderId = order.Id, total = order.Total, items = order.Items.Count() },
                    userId: GetUserId(), ipAddress: HttpContext.GetIpAddress(), userAgent: HttpContext.GetUserAgent());
                return CreatedAtAction(nameof(GetById), new { id = order.Id }, order);
            }
            catch (InvalidOperationException ex)
            {
                await _auditService.LogAsync(AuditActions.ORDER_CREATED, AuditCategories.Order, "ERROR",
                    new { razon = ex.Message },
                    userId: GetUserId(), ipAddress: HttpContext.GetIpAddress(), userAgent: HttpContext.GetUserAgent());
                return BadRequest(new { message = ex.Message });
            }
        }

        // ── Historial y detalle ───────────────────────────────────────────────────

        /// <summary>Historial de órdenes del usuario autenticado.</summary>
        [HttpGet]
        public async Task<IActionResult> GetMyOrders()
        {
            var orders = await _orderService.GetMyOrdersAsync(GetUserId());
            return Ok(orders);
        }

        /// <summary>Detalle de una orden.</summary>
        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var order = await _orderService.GetByIdAsync(GetUserId(), id);
            if (order is null) return NotFound(new { message = "Orden no encontrada." });
            return Ok(order);
        }
    }
}
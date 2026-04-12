using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NexWearAPI.DTOs;
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

        public OrdersController(IOrderService orderService, ILogger<OrdersController> logger)
        {
            _orderService = orderService;
            _logger = logger;
        }

        private Guid GetUserId() =>
            Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? User.FindFirstValue("sub")!);

        // ── Checkout con Mercado Pago ─────────────────────────────────────────────

        /// <summary>
        /// Procesa el pago y registra la orden.
        /// El frontend manda el token de tarjeta generado por MP.js y la dirección.
        /// </summary>
        [HttpPost("checkout")]
        public async Task<IActionResult> Checkout([FromBody] MpCheckoutDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            try
            {
                var order = await _orderService.CheckoutAsync(GetUserId(), dto);
                return CreatedAtAction(nameof(GetById), new { id = order.Id }, order);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en checkout MP");
                return StatusCode(500, new { message = "Error al procesar el pago. Intenta de nuevo." });
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
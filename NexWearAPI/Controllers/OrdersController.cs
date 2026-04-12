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

        // ── PASO 1: Frontend llama esto ANTES de abrir el popup de PayPal ─────────

        /// <summary>
        /// Crea una orden en PayPal y retorna el paypalOrderId.
        /// El frontend usa ese ID para abrir el popup de pago.
        /// </summary>
        [HttpPost("paypal/create")]
        public async Task<IActionResult> CreatePayPalOrder()
        {
            try
            {
                var result = await _orderService.CreatePayPalOrderAsync(GetUserId());
                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creando orden PayPal");
                return StatusCode(500, new { message = "Error al iniciar el pago. Intenta de nuevo." });
            }
        }

        // ── PASO 2: Frontend llama esto DESPUÉS de que el usuario aprobó en PayPal ─

        /// <summary>
        /// Captura el pago (cobra), descuenta stock y registra la orden en BD.
        /// </summary>
        [HttpPost("paypal/capture")]
        public async Task<IActionResult> CapturePayPalOrder([FromBody] CaptureCheckoutDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            try
            {
                var order = await _orderService.CaptureAndCheckoutAsync(GetUserId(), dto);
                return CreatedAtAction(nameof(GetById), new { id = order.Id }, order);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error capturando pago PayPal");
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
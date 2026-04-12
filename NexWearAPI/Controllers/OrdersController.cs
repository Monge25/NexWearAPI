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
        private readonly IPayPalService _payPalService;

        public OrdersController(IOrderService orderService, IPayPalService payPalService)
        {
            _orderService = orderService;
            _payPalService = payPalService;
        }

        private Guid GetUserId() =>
            Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? User.FindFirstValue("sub")!);

        /// <summary>Checkout legacy (sin PayPal)</summary>
        [HttpPost("checkout")]
        public async Task<IActionResult> Checkout([FromBody] CheckoutDto dto)
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
            catch (Exception)
            {
                return StatusCode(500, new { message = "Error interno al procesar el pedido. Intenta de nuevo." });
            }
        }

        /// <summary>Checkout con PayPal — captura server-side y crea la orden.</summary>
        [HttpPost("checkout/paypal")]
        public async Task<IActionResult> CheckoutWithPaypal([FromBody] PaypalCheckoutDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            try
            {
                var order = await _orderService.CheckoutWithPaypalAsync(GetUserId(), dto);
                return CreatedAtAction(nameof(GetById), new { id = order.Id }, order);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception)
            {
                return StatusCode(500, new { message = "Error al procesar el pago. Intenta de nuevo." });
            }
        }

        /// <summary>Historial de órdenes del usuario autenticado</summary>
        [HttpGet]
        public async Task<IActionResult> GetMyOrders()
        {
            var orders = await _orderService.GetMyOrdersAsync(GetUserId());
            return Ok(orders);
        }

        /// <summary>Detalle de una orden</summary>
        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var order = await _orderService.GetByIdAsync(GetUserId(), id);
            if (order is null) return NotFound(new { message = "Orden no encontrada." });
            return Ok(order);
        }
    }
}
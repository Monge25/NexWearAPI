using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NexWearAPI.DTOs;
using NexWearAPI.Models;
using NexWearAPI.Services;
using System.Security.Claims;

namespace NexWearAPI.Controllers
{
    [ApiController]
    [Route("/api/[controller]")]
    [Authorize]
    public class CartController : ControllerBase
    {
        private readonly ICartService _cartService;

        public CartController(ICartService cartService)
        {
            _cartService = cartService;
        }

        private Guid GetUserId() =>
            Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? User.FindFirstValue("sub")!);

        /// <summary>Obtener carrito del usuario autenticado</summary>
        [HttpGet]
        public async Task<IActionResult> GetCart()
        {
            var cart = await _cartService.GetCartAsync(GetUserId());
            return Ok(cart);
        }

        /// <summary>Agregar producto al carrito</summary>
        [HttpPost("items")]
        public async Task<IActionResult> AddItem([FromBody] AddToCartDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var cart = await _cartService.AddItemAsync(GetUserId(), dto);
                return Ok(cart);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>Actualizar cantidad de un item</summary>
        [HttpPut("items/{cartItemId:guid}")]
        public async Task<IActionResult> UpdateItem(Guid cartItemId, [FromBody] UpdateCartItemDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            try
            {
                var cart = await _cartService.UpdateItemAsync(GetUserId(), cartItemId, dto);
                return Ok(cart);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>Eliminar item del carrito</summary>
        [HttpDelete("items/{cartItemId:guid}")]
        public async Task<IActionResult> RemoveItem(Guid cartItemId)
        {
            var cart = await _cartService.RemoveItemAsync(GetUserId(), cartItemId);
            return Ok(cart);
        }

        /// <summary>Vaciar carrito</summary>
        [HttpDelete]
        public async Task<IActionResult> ClearCart() 
        {
            await _cartService.ClearCartAsync(GetUserId());
            return NoContent();
        }
    }
}

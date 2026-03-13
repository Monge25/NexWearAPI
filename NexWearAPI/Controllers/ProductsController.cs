using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NexWearAPI.DTOs;
using NexWearAPI.Services;

namespace NexWearAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProductsController : ControllerBase
    {
        private readonly IProductService _productService;
        private readonly ILogger<ProductsController> _logger;

        public ProductsController(IProductService productService, ILogger<ProductsController> logger)
        {
            _productService = productService;
            _logger = logger;
        }

        /// <summary>Listar todos los productos activos (público)</summary>
        // A01 - Endpoint público, cualquiera puede ver productos
        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> GetAll(
            [FromQuery] string? category,
            [FromQuery] string? search)
        {
            var products = await _productService.GetAllProductsAsync(category, search);
            return Ok(products);
        }

        /// <summary>Obtener un producto por ID (público)</summary>
        [AllowAnonymous]
        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var product = await _productService.GetProductByIdAsync(id);

            if (product is null)
                return NotFound(new { message = "Producto no encontrado." });

            return Ok(product);
        }

        /// <summary>Crear un producto (solo Admin)</summary>
        // A01 - Solo administradores pueden crear productos
        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateProductDto dto)
        {
            // A08 - Validaciones del DTO se ejecutan automáticamente
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var product = await _productService.CreateAsync(dto);

            // A09 - Log de creación de producto
            _logger.LogInformation("Producto creado: {Name} a las {Time}", dto.Name, DateTime.UtcNow);

            return CreatedAtAction(nameof(GetById), new { id = product.Id }, product);
        }

        /// <summary>Actualizar un producto (solo Admin)</summary>
        [Authorize(Roles = "Admin")]
        [HttpPut("{id:guid}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateProductDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var product = await _productService.UpdateAsync(id, dto);

            if (product is null)
                return NotFound(new { message = "Producto no encontrado." });

            _logger.LogInformation("Producto actualizado: {Id} a las {Time}", id, DateTime.UtcNow);

            return Ok(product);
        }

        /// <summary>Eliminar un producto - soft delete (solo Admin)</summary>
        [Authorize(Roles = "Admin")]
        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var result = await _productService.DeleteAsync(id);

            if (!result)
                return NotFound(new { message = "Producto no encontrado." });

            _logger.LogInformation("Producto desactivado: {Id} a las {Time}", id, DateTime.UtcNow);

            return NoContent();
        }
    }
}

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NexWearAPI.DTOs;
using NexWearAPI.Services;

namespace NexWearAPI.Controllers
{
    [ApiController]
    [Route("api/products/{productId:guid}/variants")]
    public class ProductVariantsController : ControllerBase
    {
        private readonly IProductVariantService _variantService;
        private readonly IImageService _imageService;
        private readonly ILogger<ProductVariantsController> _logger;

        public ProductVariantsController(
            IProductVariantService variantService,
            IImageService imageService,
            ILogger<ProductVariantsController> logger)
        {
            _variantService = variantService;
            _imageService = imageService;
            _logger = logger;
        }

        /// <summary>Listar variantes de un producto (público)</summary>
        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> GetAll(Guid productId)
        {
            var variants = await _variantService.GetByProductAsync(productId);
            return Ok(variants);
        }

        /// <summary>Obtener variante por ID (público)</summary>
        [AllowAnonymous]
        [HttpGet("{variantId:guid}")]
        public async Task<IActionResult> GetById(Guid productId, Guid variantId)
        {
            var variant = await _variantService.GetByIdAsync(productId, variantId);
            if (variant is null)
                return NotFound(new { message = "Variante no encontrada." });

            return Ok(variant);
        }

        /// <summary>Crear variante (solo Admin)</summary>
        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<IActionResult> Create(Guid productId, [FromBody] CreateVariantDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var variant = await _variantService.CreateAsync(productId, dto);
            if (variant is null)
                return NotFound(new { message = "Producto no encontrado." });

            _logger.LogInformation("Variante creada para producto {ProductId}", productId);

            return CreatedAtAction(nameof(GetById),
                new { productId, variantId = variant.Id }, variant);
        }

        /// <summary>Actualizar variante (solo Admin)</summary>
        [Authorize(Roles = "Admin")]
        [HttpPut("{variantId:guid}")]
        public async Task<IActionResult> Update(Guid productId, Guid variantId, [FromBody] UpdateVariantDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var variant = await _variantService.UpdateAsync(productId, variantId, dto);
            if (variant is null)
                return NotFound(new { message = "Variante no encontrada." });

            return Ok(variant);
        }

        /// <summary>Subir imagen de variante (solo Admin)</summary>
        [Authorize(Roles = "Admin")]
        [HttpPost("{variantId:guid}/image")]
        public async Task<IActionResult> UploadImage(Guid productId, Guid variantId, IFormFile file)
        {
            var imageUrl = await _imageService.UploadImageAsync(file);
            if (imageUrl is null)
                return BadRequest(new { message = "Archivo inválido. Solo JPG, PNG o WEBP de máximo 5MB." });

            var variant = await _variantService.UploadImageAsync(productId, variantId, imageUrl);
            if (variant is null)
                return NotFound(new { message = "Variante no encontrada." });

            _logger.LogInformation("Imagen subida para variante {VariantId}", variantId);

            return Ok(variant);
        }

        /// <summary>Eliminar variante - soft delete (solo Admin)</summary>
        [Authorize(Roles = "Admin")]
        [HttpDelete("{variantId:guid}")]
        public async Task<IActionResult> Delete(Guid productId, Guid variantId)
        {
            var result = await _variantService.DeleteAsync(productId, variantId);
            if (!result)
                return NotFound(new { message = "Variante no encontrada." });

            return NoContent();
        }
    }
}

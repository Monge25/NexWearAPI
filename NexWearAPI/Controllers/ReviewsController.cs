using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NexWearAPI.DTOs;
using NexWearAPI.Services;
using System.Security.Claims;

namespace NexWearAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ReviewsController : ControllerBase
    {
        private readonly IReviewService _reviewService;
        private readonly IImageService _imageService;
        private readonly ILogger<ReviewsController> _logger;

        public ReviewsController(
            IReviewService reviewService,
            IImageService imageService,
            ILogger<ReviewsController> logger)
        {
            _reviewService = reviewService;
            _imageService = imageService;
            _logger = logger;
        }

        private Guid GetUserId() =>
            Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? User.FindFirstValue("sub")!);

        private bool IsAdmin() =>
            User.IsInRole("Admin");

        /// <summary>Reseñas aprobadas de un producto (público)</summary>
        [AllowAnonymous]
        [HttpGet("product/{productId:guid}")]
        public async Task<IActionResult> GetByProduct(Guid productId)
        {
            var reviews = await _reviewService.GetByProductAsync(productId);
            return Ok(reviews);
        }

        /// <summary>Resumen de ratings de un producto (público)</summary>
        [AllowAnonymous]
        [HttpGet("product/{productId:guid}/summary")]
        public async Task<IActionResult> GetSummary(Guid productId)
        {
            var summary = await _reviewService.GetRatingSummaryAsync(productId);
            return Ok(summary);
        }

        /// <summary>Crear reseña (requiere compra verificada)</summary>
        [Authorize]
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateReviewDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            try
            {
                var review = await _reviewService.CreateAsync(GetUserId(), dto);
                _logger.LogInformation("Reseña creada: {ReviewId} por usuario {UserId}", review.Id, GetUserId());
                return CreatedAtAction(nameof(GetByProduct), new { productId = dto.ProductId }, review);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>Editar reseña propia</summary>
        [Authorize]
        [HttpPut("{reviewId:guid}")]
        public async Task<IActionResult> Update(Guid reviewId, [FromBody] UpdateReviewDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var review = await _reviewService.UpdateAsync(GetUserId(), reviewId, dto);

            if (review is null)
                return NotFound(new { message = "Reseña no encontrada." });

            _logger.LogInformation("Reseña {ReviewId} editada por usuario {UserId}",
                reviewId, GetUserId());

            return Ok(review);
        }

        /// <summary>Subir foto a una reseña</summary>
        [Authorize]
        [HttpPost("{reviewId:guid}/photos")]
        public async Task<IActionResult> UploadPhoto(Guid reviewId, IFormFile file)
        {
            var photoUrl = await _imageService.UploadImageAsync(file);
            if (photoUrl is null)
                return BadRequest(new { message = "Archivo inválido. Solo JPG, PNG o WEBP de máximo 5MB." });

            try
            {
                var review = await _reviewService.UploadPhotoAsync(GetUserId(), reviewId, photoUrl);
                if (review is null) return NotFound(new { message = "Reseña no encontrada." });
                return Ok(review);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>Reseñas pendientes de moderación (solo Admin)</summary>
        [Authorize(Roles = "Admin")]
        [HttpGet("pending")]
        public async Task<IActionResult> GetPending()
        {
            var reviews = await _reviewService.GetPendingAsync();
            return Ok(reviews);
        }

        /// <summary>Aprobar o rechazar reseña (solo Admin)</summary>
        [Authorize(Roles = "Admin")]
        [HttpPut("{reviewId:guid}/moderate")]
        public async Task<IActionResult> Moderate(Guid reviewId, [FromBody] ModerateReviewDto dto)
        {
            var review = await _reviewService.ModerateAsync(reviewId, dto);
            if (review is null) return NotFound(new { message = "Reseña no encontrada." });

            _logger.LogInformation("Reseña {ReviewId} {Action} por admin",
                reviewId, dto.Approved ? "aprobada" : "rechazada");

            return Ok(review);
        }

        /// <summary>Eliminar reseña</summary>
        [Authorize]
        [HttpDelete("{reviewId:guid}")]
        public async Task<IActionResult> Delete(Guid reviewId)
        {
            var result = await _reviewService.DeleteAsync(GetUserId(), reviewId, IsAdmin());
            if (!result) return NotFound(new { message = "Reseña no encontrada." });
            return NoContent();
        }

        /// <summary>Eliminar foto de una reseña</summary>
        [Authorize]
        [HttpDelete("{reviewId:guid}/photos")]
        public async Task<IActionResult> DeletePhoto(Guid reviewId, [FromQuery] string photoUrl)
        {
            if (string.IsNullOrWhiteSpace(photoUrl))
                return BadRequest(new { message = "Se requiere la URL de la foto." });

            var result = await _reviewService.DeletePhotoAsync(GetUserId(), reviewId, photoUrl);

            if (!result)
                return NotFound(new { message = "Foto o reseña no encontrada." });

            return NoContent();
        }
    }
}

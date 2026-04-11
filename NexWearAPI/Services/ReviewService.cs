using Microsoft.EntityFrameworkCore;
using NexWearAPI.Data;
using NexWearAPI.DTOs;
using NexWearAPI.Models;

namespace NexWearAPI.Services
{
    public interface IReviewService
    {
        Task<ReviewResponseDto> CreateAsync(Guid userId, CreateReviewDto dto);
        Task<ReviewResponseDto?> UpdateAsync(Guid userId, Guid reviewId, UpdateReviewDto dto);
        Task<ReviewResponseDto?> UploadPhotoAsync(Guid userId, Guid reviewId, string photoUrl);
        Task<IEnumerable<ReviewResponseDto>> GetByProductAsync(Guid productId);
        Task<IEnumerable<ReviewResponseDto>> GetPendingAsync();       // Admin
        Task<ReviewResponseDto?> ModerateAsync(Guid reviewId, ModerateReviewDto dto); // Admin
        Task<bool> DeleteAsync(Guid userId, Guid reviewId, bool isAdmin);
        Task<ProductRatingSummaryDto> GetRatingSummaryAsync(Guid productId);
        Task<bool> DeletePhotoAsync(Guid userId, Guid reviewId, string photoUrl);
    }

    public class ReviewService : IReviewService
    {
        private readonly AppDbContext _context;

        public ReviewService(AppDbContext context)
        {
            _context = context;
        }

        // ── Crear reseña ──────────────────────────────────────────
        public async Task<ReviewResponseDto> CreateAsync(Guid userId, CreateReviewDto dto)
        {
            // Verificar que la orden existe, pertenece al usuario y contiene el producto
            var orderItem = await _context.OrderItems
                .Include(oi => oi.Order)
                .FirstOrDefaultAsync(oi =>
                    oi.Order.UserId == userId &&
                    oi.OrderId == dto.OrderId &&
                    oi.ProductId == dto.ProductId &&
                    oi.Order.Status == OrderStatus.Paid);           // MODIFICAR PARA QUE SOLO SEA CON ESTATUS DE ENTREGADO

            if (orderItem is null)
                throw new InvalidOperationException("Solo puedes reseñar productos que hayas recibido.");

            // Verificar que no haya reseñado este producto antes
            var exists = await _context.Reviews
                .AnyAsync(r => r.UserId == userId && r.ProductId == dto.ProductId);

            if (exists)
                throw new InvalidOperationException("Ya tienes una reseña para este producto.");

            var review = new Review
            {
                UserId = userId,
                ProductId = dto.ProductId,
                OrderId = dto.OrderId,
                Rating = dto.Rating,
                Comment = dto.Comment?.Trim(),
                IsApproved = false,  // pendiente de moderación
            };

            _context.Reviews.Add(review);
            await _context.SaveChangesAsync();

            return await MapToDtoAsync(review);
        }

        // ── Editar reseña ─────────────────────────────────────────────
        public async Task<ReviewResponseDto?> UpdateAsync(Guid userId, Guid reviewId, UpdateReviewDto dto)
        {
            var review = await _context.Reviews
                .Include(r => r.User)
                .FirstOrDefaultAsync(r => r.Id == reviewId && r.UserId == userId);

            if (review is null) return null;

            // Al editar vuelve a pendiente de moderación
            if (dto.Rating is not null) review.Rating = dto.Rating.Value;
            if (dto.Comment is not null) review.Comment = dto.Comment.Trim();

            review.IsApproved = false;
            review.IsRejected = false;

            await _context.SaveChangesAsync();
            return MapToDto(review);
        }

        // ── Subir foto a la reseña ────────────────────────────────
        public async Task<ReviewResponseDto?> UploadPhotoAsync(Guid userId, Guid reviewId, string photoUrl)
        {
            var review = await _context.Reviews
                .Include(r => r.User)
                .FirstOrDefaultAsync(r => r.Id == reviewId && r.UserId == userId);

            if (review is null) return null;

            // Máximo 3 fotos por reseña
            if (review.PhotoUrls.Count >= 3)
                throw new InvalidOperationException("Máximo 3 fotos por reseña.");

            review.PhotoUrls.Add(photoUrl);
            await _context.SaveChangesAsync();

            return await MapToDtoAsync(review);
        }

        // ── Reseñas aprobadas de un producto ─────────────────────
        public async Task<IEnumerable<ReviewResponseDto>> GetByProductAsync(Guid productId)
        {
            var reviews = await _context.Reviews
                .Include(r => r.User)
                .Where(r => r.ProductId == productId && r.IsApproved)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            return reviews.Select(r => MapToDto(r));
        }

        // ── Reseñas pendientes de moderación (Admin) ──────────────
        public async Task<IEnumerable<ReviewResponseDto>> GetPendingAsync()
        {
            var reviews = await _context.Reviews
                .Include(r => r.User)
                .Include(r => r.Product)
                .Where(r => !r.IsApproved && !r.IsRejected)
                .OrderBy(r => r.CreatedAt)
                .ToListAsync();

            return reviews.Select(r => MapToDto(r));
        }

        // ── Moderar reseña (Admin) ────────────────────────────────
        public async Task<ReviewResponseDto?> ModerateAsync(Guid reviewId, ModerateReviewDto dto)
        {
            var review = await _context.Reviews
                .Include(r => r.User)
                .FirstOrDefaultAsync(r => r.Id == reviewId);

            if (review is null) return null;

            review.IsApproved = dto.Approved;
            review.IsRejected = !dto.Approved;

            await _context.SaveChangesAsync();
            return MapToDto(review);
        }

        // ── Eliminar reseña ───────────────────────────────────────
        public async Task<bool> DeleteAsync(Guid userId, Guid reviewId, bool isAdmin)
        {
            var review = await _context.Reviews
                .FirstOrDefaultAsync(r => r.Id == reviewId &&
                    (isAdmin || r.UserId == userId));  // admin puede borrar cualquiera

            if (review is null) return false;

            _context.Reviews.Remove(review);
            await _context.SaveChangesAsync();
            return true;
        }

        // ── Resumen de ratings del producto ───────────────────────
        public async Task<ProductRatingSummaryDto> GetRatingSummaryAsync(Guid productId)
        {
            var ratings = await _context.Reviews
                .Where(r => r.ProductId == productId && r.IsApproved)
                .Select(r => r.Rating)
                .ToListAsync();

            if (!ratings.Any())
                return new ProductRatingSummaryDto
                {
                    Average = 0,
                    Total = 0,
                    Distribution = new Dictionary<int, int> { { 5, 0 }, { 4, 0 }, { 3, 0 }, { 2, 0 }, { 1, 0 } }
                };

            return new ProductRatingSummaryDto
            {
                Average = Math.Round(ratings.Average(), 1),
                Total = ratings.Count,
                Distribution = Enumerable.Range(1, 5)
                    .ToDictionary(i => i, i => ratings.Count(r => r == i))
            };
        }

        // ── Eliminar foto de la reseña ────────────────────────────────
        public async Task<bool> DeletePhotoAsync(Guid userId, Guid reviewId, string photoUrl)
        {
            var review = await _context.Reviews
                .FirstOrDefaultAsync(r => r.Id == reviewId && r.UserId == userId);

            if (review is null) return false;
            if (!review.PhotoUrls.Contains(photoUrl)) return false;

            review.PhotoUrls.Remove(photoUrl);
            await _context.SaveChangesAsync();
            return true;
        }

        // ── Mappers ───────────────────────────────────────────────
        private static ReviewResponseDto MapToDto(Review r) => new()
        {
            Id = r.Id,
            UserId = r.UserId,
            UserName = $"{r.User?.FirstName} {r.User?.LastName}".Trim(),
            ProductId = r.ProductId,
            Rating = r.Rating,
            Comment = r.Comment,
            PhotoUrls = r.PhotoUrls,
            IsApproved = r.IsApproved,
            IsVerifiedPurchase = true,
            CreatedAt = r.CreatedAt,
        };

        private async Task<ReviewResponseDto> MapToDtoAsync(Review r)
        {
            await _context.Entry(r).Reference(x => x.User).LoadAsync();
            return MapToDto(r);
        }
    }
}

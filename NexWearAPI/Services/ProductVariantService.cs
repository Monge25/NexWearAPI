using Microsoft.EntityFrameworkCore;
using NexWearAPI.Data;
using NexWearAPI.DTOs;
using NexWearAPI.Models;

namespace NexWearAPI.Services
{
    public interface IProductVariantService
    {
        Task<IEnumerable<VariantResponseDto>> GetByProductAsync(Guid productId);
        Task<VariantResponseDto?> GetByIdAsync(Guid productId, Guid variantId);
        Task<VariantResponseDto?> CreateAsync(Guid productId, CreateVariantDto dto);
        Task<VariantResponseDto?> UpdateAsync(Guid productId, Guid variantId, UpdateVariantDto dto);
        Task<bool> DeleteAsync(Guid productId, Guid variantId);
        Task<VariantResponseDto?> UploadImageAsync(Guid productId, Guid variantId, string imageUrl);
    }

    public class ProductVariantService : IProductVariantService
    {
        private readonly AppDbContext _context;

        public ProductVariantService(AppDbContext context)
        {
            _context = context;
        }

        // ── Listar variantes de un producto ───────────────────────
        public async Task<IEnumerable<VariantResponseDto>> GetByProductAsync(Guid productId)
        {
            var product = await _context.Products.FindAsync(productId);
            if (product is null) return Enumerable.Empty<VariantResponseDto>();

            var variants = await _context.ProductVariants
                .Where(v => v.ProductId == productId && v.IsActive)
                .ToListAsync();

            return variants.Select(v => MapToDto(v, product.Price));
        }

        // ── Obtener variante por ID ────────────────────────────────
        public async Task<VariantResponseDto?> GetByIdAsync(Guid productId, Guid variantId)
        {
            var product = await _context.Products.FindAsync(productId);
            if (product is null) return null;

            var variant = await _context.ProductVariants
                .FirstOrDefaultAsync(v => v.Id == variantId && v.ProductId == productId);

            return variant is null ? null : MapToDto(variant, product.Price);
        }

        // ── Crear variante ────────────────────────────────────────
        public async Task<VariantResponseDto?> CreateAsync(Guid productId, CreateVariantDto dto)
        {
            var product = await _context.Products.FindAsync(productId);
            if (product is null) return null;

            var variant = new ProductVariant
            {
                ProductId = productId,
                Color = dto.Color?.Trim(),
                ColorHex = dto.ColorHex?.Trim(),
                Size = dto.Size?.Trim(),
                PriceModifier = dto.PriceModifier,
                Stock = dto.Stock,
                IsActive = true,
                IsOnSale = dto.IsOnSale
            };

            _context.ProductVariants.Add(variant);
            await _context.SaveChangesAsync();

            return MapToDto(variant, product.Price);
        }

        // ── Actualizar variante ───────────────────────────────────
        public async Task<VariantResponseDto?> UpdateAsync(Guid productId, Guid variantId, UpdateVariantDto dto)
        {
            var product = await _context.Products.FindAsync(productId);
            if (product is null) return null;

            var variant = await _context.ProductVariants
                .FirstOrDefaultAsync(v => v.Id == variantId && v.ProductId == productId);
            if (variant is null) return null;

            if (dto.Color is not null) variant.Color = dto.Color.Trim();
            if (dto.ColorHex is not null) variant.ColorHex = dto.ColorHex.Trim();
            if (dto.Size is not null) variant.Size = dto.Size.Trim();
            if (dto.PriceModifier is not null) variant.PriceModifier = dto.PriceModifier.Value;
            if (dto.Stock is not null) variant.Stock = dto.Stock.Value;
            if (dto.IsActive is not null) variant.IsActive = dto.IsActive.Value;
            if (dto.IsOnSale is not null) variant.IsOnSale = dto.IsOnSale.Value;

            await _context.SaveChangesAsync();
            return MapToDto(variant, product.Price);
        }

        // ── Eliminar variante (soft delete) ───────────────────────
        public async Task<bool> DeleteAsync(Guid productId, Guid variantId)
        {
            var variant = await _context.ProductVariants
                .FirstOrDefaultAsync(v => v.Id == variantId && v.ProductId == productId);
            if (variant is null) return false;

            variant.IsActive = false;
            await _context.SaveChangesAsync();
            return true;
        }

        // ── Actualizar imagen de variante ─────────────────────────
        public async Task<VariantResponseDto?> UploadImageAsync(Guid productId, Guid variantId, string imageUrl)
        {
            var product = await _context.Products.FindAsync(productId);
            if (product is null) return null;

            var variant = await _context.ProductVariants
                .FirstOrDefaultAsync(v => v.Id == variantId && v.ProductId == productId);
            if (variant is null) return null;

            variant.ImageUrl = imageUrl;

            // Si el producto base no tiene imagen principal todavía,
            // usar automáticamente la primera imagen que se suba
            if (string.IsNullOrEmpty(product.ImageUrl))
            {
                product.ImageUrl = imageUrl;
            }

            await _context.SaveChangesAsync();

            return MapToDto(variant, product.Price);
        }

        // ── Mapper ────────────────────────────────────────────────
        private static VariantResponseDto MapToDto(ProductVariant v, decimal basePrice) => new()
        {
            Id = v.Id,
            Color = v.Color,
            ColorHex = v.ColorHex,
            Size = v.Size,
            PriceModifier = v.PriceModifier,
            FinalPrice = basePrice + v.PriceModifier,  // precio final calculado
            Stock = v.Stock,
            ImageUrl = v.ImageUrl,
            IsActive = v.IsActive,
            IsOnSale = v.IsOnSale
        };
    }
}

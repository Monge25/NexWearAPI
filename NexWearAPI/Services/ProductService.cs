using CloudinaryDotNet.Core;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NexWearAPI.Data;
using NexWearAPI.DTOs;
using NexWearAPI.Models;

namespace NexWearAPI.Services
{
    // ── Interface ────────────────────────────────────────────────
    public interface IProductService
    {
        Task<PagedResult<ProductResponseDto>> GetAllProductsAsync(
            string? category, 
            string? search, 
            decimal? minPrice, 
            decimal? maxPrice, 
            string? sortBy, 
            bool? isOnSale,
            int page,
            int pageSize);
        Task<ProductResponseDto?> GetProductByIdAsync(Guid id);
        Task<ProductResponseDto> CreateAsync(CreateProductDto dto);
        Task<ProductResponseDto?> UpdateAsync(Guid id, UpdateProductDto dto);
        Task<bool> DeleteAsync(Guid id);
        // Task<IActionResult> UploadImageAsync(Guid id, IFormFile file);
    }

    // ── Implementación ───────────────────────────────────────────
    public class ProductService : IProductService
    {
        private readonly AppDbContext _context;

        public ProductService(AppDbContext context)
        {
            _context = context;
        }

        // ── Listar productos ──────────────────────────────────────
        // A01 - Endpoint público, solo muestra productos activos
        public async Task<PagedResult<ProductResponseDto>> GetAllProductsAsync(

            string? category, 
            string? search, 
            decimal? minPrice, 
            decimal? maxPrice, 
            string? sortBy, 
            bool? isOnSale,
            int page = 1,
            int pageSize = 10)
        {
            page = page < 1 ? 1 : page;
            pageSize = pageSize > 50 ? 50 : pageSize; // límite máximo de 50

            // A03 - EF Core usa queries parametrizados, nunca SQL crudo
            var query = _context.Products
                .Where(p => p.IsActive)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(category))
                query = query.Where(p => p.Category.ToLower() == category.ToLower());

            if (!string.IsNullOrWhiteSpace(search))
                query = query.Where(p =>
                    p.Name.ToLower().Contains(search.ToLower()) ||
                    (p.Description != null && p.Description.ToLower().Contains(search.ToLower())));

            if (minPrice.HasValue)
                query = query.Where(p => p.Price >= minPrice.Value);

            if (maxPrice.HasValue)
                query = query.Where(p => p.Price <= maxPrice.Value);

            if (isOnSale == true)
                query = query.Where(p => p.Variants.Any(v => v.IsOnSale && v.IsActive));

            query = sortBy switch
            {
                "price_asc" => query.OrderBy(p => p.Price),
                "price_desc" => query.OrderByDescending(p => p.Price),
                "createdAt_asc" => query.OrderBy(p => p.CreatedAt),
                "createdAt_desc" => query.OrderByDescending(p => p.CreatedAt),
                _ => query.OrderByDescending(p => p.CreatedAt)
            };

            var totalCount = await query.CountAsync();

            var products = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new PagedResult<ProductResponseDto>
            {
                Items = products.Select(MapToDto),
                Page = page,
                PageSize = pageSize,
                TotalCount = totalCount
            };

            // return products.Select(MapToDto);
        }

        // ── Obtener producto por ID ───────────────────────────────
        public async Task<ProductResponseDto?> GetProductByIdAsync(Guid id)
        {
            var product = await _context.Products
                .FirstOrDefaultAsync(p => p.Id == id && p.IsActive);

            return product is null ? null : MapToDto(product);
        }

        // ── Crear producto (solo Admin) ───────────────────────────
        public async Task<ProductResponseDto> CreateAsync(CreateProductDto dto)
        {
            var product = new Product
            {
                Name = dto.Name.Trim(),
                Description = dto.Description?.Trim(),
                Price = dto.Price,
                ImageUrl = dto.ImageUrl?.Trim(),
                Category = dto.Category.Trim(),
                IsActive = true,
                Care = dto.Care?.Trim(),
                Origin = dto.Origin?.Trim()
            };

            _context.Products.Add(product);
            await _context.SaveChangesAsync();

            return MapToDto(product);
        }

        // ── Actualizar producto (solo Admin) ──────────────────────
        public async Task<ProductResponseDto?> UpdateAsync(Guid id, UpdateProductDto dto)
        {
            var product = await _context.Products.FindAsync(id);

            if (product is null) return null;

            // Solo actualiza los campos que vienen en el request
            if (dto.Name is not null) product.Name = dto.Name.Trim();
            if (dto.Description is not null) product.Description = dto.Description.Trim();
            if (dto.Price is not null) product.Price = dto.Price.Value;
            if (dto.ImageUrl is not null) product.ImageUrl = dto.ImageUrl.Trim();
            if (dto.Category is not null) product.Category = dto.Category.Trim();
            if (dto.IsActive is not null) product.IsActive = dto.IsActive.Value;
            if (dto.Care is not null) product.Care = dto.Care.Trim();
            if (dto.Origin is not null) product.Origin = dto.Origin.Trim();

            await _context.SaveChangesAsync();

            return MapToDto(product);
        }

        // ── Eliminar producto (solo Admin) ────────────────────────
        // Soft delete: no borramos el registro, solo lo desactivamos
        // Esto protege el historial de órdenes que referencian este producto
        public async Task<bool> DeleteAsync(Guid id)
        {
            var product = await _context.Products.FindAsync(id);

            if (product is null) return false;

            product.IsActive = false;
            await _context.SaveChangesAsync();

            return true;
        }

        // ── Mapper: Product → ProductResponseDto ─────────────────
        // A04 - Nunca devolvemos la entidad directamente
        private static ProductResponseDto MapToDto(Product p) => new()
        {
            Id = p.Id,
            Name = p.Name,
            Description = p.Description,
            Price = p.Price,
            ImageUrl = p.ImageUrl,
            Category = p.Category,
            IsActive = p.IsActive,
            CreatedAt = p.CreatedAt,
            Care = p.Care,
            Origin = p.Origin
        };

        private static ProductWithVariantsDto MapToDtoWithVariants(Product p) => new()
        {
            Id = p.Id,
            Name = p.Name,
            Description = p.Description,
            BasePrice = p.Price,
            ImageUrl = p.ImageUrl,
            Category = p.Category,
            IsActive = p.IsActive,
            CreatedAt = p.CreatedAt,
            Variants = p.Variants.Select(v => new VariantResponseDto
            {
                Id = v.Id,
                Color = v.Color,
                ColorHex = v.ColorHex,
                Size = v.Size,
                PriceModifier = v.PriceModifier,
                FinalPrice = p.Price + v.PriceModifier,
                Stock = v.Stock,
                ImageUrl = v.ImageUrl,
                IsActive = v.IsActive
            })
        };
        //public Task<IActionResult> UploadImageAsync(Guid id, IFormFile file)
        //{
        //    throw new NotImplementedException();
        //}
    }
}

using NexWearAPI.Data;
using NexWearAPI.DTOs;
using NexWearAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace NexWearAPI.Services
{
    // ── Interface ────────────────────────────────────────────────
    public interface IProductService
    {
        Task<IEnumerable<ProductResponseDto>> GetAllProductsAsync(string? category, string? search);
        Task<ProductResponseDto?> GetProductByIdAsync(Guid id);
        Task<ProductResponseDto> CreateAsync(CreateProductDto dto);
        Task<ProductResponseDto?> UpdateAsync(Guid id, UpdateProductDto dto);
        Task<bool> DeleteAsync(Guid id);
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
        public async Task<IEnumerable<ProductResponseDto>> GetAllProductsAsync(string? category, string? search)
        {
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

            var products = await query
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();

            return products.Select(MapToDto);
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
                Stock = dto.Stock,
                Size = dto.Size?.Trim(),
                Color = dto.Color?.Trim(),
                ImageUrl = dto.ImageUrl?.Trim(),
                Category = dto.Category.Trim(),
                IsActive = true
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
            if (dto.Stock is not null) product.Stock = dto.Stock.Value;
            if (dto.Size is not null) product.Size = dto.Size.Trim();
            if (dto.Color is not null) product.Color = dto.Color.Trim();
            if (dto.ImageUrl is not null) product.ImageUrl = dto.ImageUrl.Trim();
            if (dto.Category is not null) product.Category = dto.Category.Trim();
            if (dto.IsActive is not null) product.IsActive = dto.IsActive.Value;

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
            Stock = p.Stock,
            Size = p.Size,
            Color = p.Color,
            ImageUrl = p.ImageUrl,
            Category = p.Category,
            IsActive = p.IsActive,
            CreatedAt = p.CreatedAt
        };
    }
}

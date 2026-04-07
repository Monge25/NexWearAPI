using Microsoft.EntityFrameworkCore;
using NexWearAPI.Data;
using NexWearAPI.DTOs;
using NexWearAPI.Models;

namespace NexWearAPI.Services
{
    public interface ICartService
    {
        Task<CartResponseDto> GetCartAsync(Guid userId);
        Task<CartResponseDto> AddItemAsync(Guid userId, AddToCartDto dto);
        Task<CartResponseDto> UpdateItemAsync(Guid userId, Guid cartItemId, UpdateCartItemDto dto);
        Task<CartResponseDto> RemoveItemAsync(Guid userId, Guid cartItemId);
        Task ClearCartAsync(Guid userId);
    }

    public class CartService : ICartService
    {
        private readonly AppDbContext _context;

        public CartService(AppDbContext context)
        {
            _context = context;
        }

        // ── Obtener carrito del usuario ───────────────────────────
        public async Task<CartResponseDto> GetCartAsync(Guid userId)
        {
            var items = await _context.CartItems
                .Include(c => c.Product)
                .Include(c => c.Variant)
                .Where(c => c.UserId == userId)
                .ToListAsync();

            return BuildCartResponse(items);
        }

        // ── Agregar item al carrito ───────────────────────────────
        public async Task<CartResponseDto> AddItemAsync(Guid userId, AddToCartDto dto)
        {
            // Verificar que la variante existe y tiene stock
            var variant = await _context.ProductVariants
                .FirstOrDefaultAsync(v => v.Id == dto.VariantId && v.ProductId == dto.ProductId && v.IsActive);

            if (variant is null)
                throw new InvalidOperationException("El producto no existe o no se encuentra disponible");

            if (variant.Stock < dto.Quantity)
                throw new InvalidOperationException($"Stock insuficiente. Disponible {variant.Stock}");

            // Si el item ya existe en el carrito, verificar stock antes de actualizar la cantidad
            var existingItem = await _context.CartItems
                .FirstOrDefaultAsync(c => c.UserId == userId && c.VariantId == dto.VariantId);

            if (existingItem is not null)
            {
                var newQuantity = existingItem.Quantity + dto.Quantity;

                if (variant.Stock < newQuantity)
                    throw new InvalidOperationException($"Stock insuficiente. Disponible {variant.Stock}");

                existingItem.Quantity = newQuantity;
            }
            else
            {
                _context.CartItems.Add(new CartItem
                {
                    UserId = userId,
                    ProductId = dto.ProductId,
                    VariantId = dto.VariantId,
                    Quantity = dto.Quantity,
                });
            }

            await _context.SaveChangesAsync();

            return await GetCartAsync(userId);
        }

        // ── Actualizar cantidad ───────────────────────────────────
        public async Task<CartResponseDto> UpdateItemAsync(Guid userId, Guid cartItemId, UpdateCartItemDto dto)
        {
            var item = await _context.CartItems
                .Include(c => c.Variant)
                .FirstOrDefaultAsync(c => c.Id == cartItemId && c.UserId == userId);

            if (item is null)
                throw new InvalidOperationException("Item no encontrado");

            if (item.Variant.Stock < dto.Quantity)
                throw new InvalidOperationException($"Stock insuficiente. Disponible {item.Variant.Stock}");

            item.Quantity = dto.Quantity;
            await _context.SaveChangesAsync();
            return await GetCartAsync(userId);
        }

        // ── Eliminar item ─────────────────────────────────────────
        public async Task<CartResponseDto> RemoveItemAsync(Guid userId, Guid cartItemId)
        {
            var item = await _context.CartItems
                .FirstOrDefaultAsync(c => c.Id == cartItemId && c.UserId == userId);

            if (item is not null)
            {
                _context.CartItems.Remove(item);
                await _context.SaveChangesAsync();
            }

            return await GetCartAsync(userId);
        }

        // ── Vaciar carrito ────────────────────────────────────────
        public async Task ClearCartAsync(Guid userId)
        {
            var items = await _context.CartItems
                .Where(c => c.UserId == userId)
                .ToListAsync();

            _context.CartItems.RemoveRange(items);
            await _context.SaveChangesAsync();
        }

        // ── Mapper ────────────────────────────────────────────────
        private static CartResponseDto BuildCartResponse(IEnumerable<CartItem> items)
        {
            var dtos = items.Select(c =>
            {
                var price = c.Variant.IsOnSale && c.Variant.SalePrice.HasValue
                    ? c.Variant.SalePrice.Value
                    : c.Product.Price + c.Variant.PriceModifier;

                return new CartItemResponseDto
                {
                    Id = c.Id,
                    ProductId = c.ProductId,
                    VariantId = c.VariantId,
                    ProductName = c.Product.Name,
                    ImageUrl = c.Variant.ImageUrl ?? c.Product.ImageUrl,
                    Color = c.Variant.Color,
                    ColorHex = c.Variant.ColorHex,
                    Size = c.Variant.Size,
                    UnitPrice = price,
                    Quantity = c.Quantity,
                    Subtotal = price * c.Quantity,
                    Stock = c.Variant.Stock,
                };
            }).ToList();

            return new CartResponseDto
            {
                Items = dtos,
                TotalItems = dtos.Sum(x => x.Quantity),
                Total = dtos.Sum(x => x.Subtotal)
            };
        }
    }
}

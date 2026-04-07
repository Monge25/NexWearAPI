using Microsoft.EntityFrameworkCore;
using NexWearAPI.Data;
using NexWearAPI.DTOs;
using NexWearAPI.Models;

namespace NexWearAPI.Services
{
    public interface IOrderService
    {
        Task<OrderResponseDto> CheckoutAsync(Guid userId, CheckoutDto dto);
        Task<IEnumerable<OrderResponseDto>> GetMyOrdersAsync(Guid userId);
        Task<OrderResponseDto?> GetByIdAsync(Guid userId, Guid orderId);
    }
    public class OrderService : IOrderService
    {
        private readonly AppDbContext _context;
        public OrderService(AppDbContext context)
        {
            _context = context;
        }

        // ── Checkout convierte el carrito en una orden ──────────
        public async Task<OrderResponseDto> CheckoutAsync(Guid userId, CheckoutDto dto)
        {
            // 1. Obtener carrito del usuario
            var cartItems = await _context.CartItems
                .Include(c => c.Product)
                .Include(c => c.Variant)
                .Where(c => c.UserId == userId)
                .ToListAsync();

            if (!cartItems.Any())
                throw new InvalidOperationException("El carrito se encuentra vacío");

            // 2. Verificar stock de cada item
            foreach (var item in cartItems)
            {
                if (!item.Variant.IsActive)
                    throw new InvalidOperationException($"'{item.Product.Name} - {item.Variant.Color} {item.Variant.Size}' ya no está disponible.");

                if (item.Variant.Stock < item.Quantity)
                    throw new InvalidOperationException($"Stock insuficiente para '{item.Product.Name} - {item.Variant.Size}'. Disponible: {item.Variant.Stock}");
            }

            // 3. Calcular el total a pagar para realizar la orden
            var orderItems = cartItems.Select(c =>
            {
                var price = c.Variant.IsOnSale && c.Variant.SalePrice.HasValue
                    ? c.Variant.SalePrice.Value
                    : c.Product.Price + c.Variant.PriceModifier;

                return new OrderItem
                {
                    ProductId = c.ProductId,
                    VariantId = c.VariantId,
                    Quantity = c.Quantity,
                    UnitPrice = price,
                    ProductName = c.Product.Name, // snapshot
                    VariantColor = c.Variant.Color, // snapshot
                    VariantSize = c.Variant.Size // snapshot
                };
            }).ToList();

            var total = orderItems.Sum(i => i.UnitPrice * i.Quantity);

            // 4. Crear la orden
            var order = new Order
            {
                UserId = userId,
                Status = OrderStatus.Paid,
                Total = total,
                ShippingAddress = dto.ShippingAddress,
                OrderItems = orderItems
            };

            _context.Orders.Add(order);

            // 5. Descontar producto del stock de cada variante
            foreach (var item in cartItems)
            {
                item.Variant.Stock -= item.Quantity;
            }

            // 6. Vaciar el carrito automaticamente
            _context.CartItems.RemoveRange(cartItems);

            await _context.SaveChangesAsync();

            return MapToDto(order);
        }

        public async Task<IEnumerable<OrderResponseDto>> GetMyOrdersAsync(Guid userId)
        {
            var orders = await _context.Orders
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Variant)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                .Where(o => o.UserId == userId)
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync();

            return orders.Select(MapToDto);
        }

        // ── Detalle de una orden ──────────────────────────────────
        public async Task<OrderResponseDto?> GetByIdAsync(Guid userId, Guid orderId)
        {
            var order = await _context.Orders
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Variant)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                .FirstOrDefaultAsync(o => o.Id == orderId && o.UserId == userId);

            return order is null ? null : MapToDto(order);
        }

        // ── Mapper ────────────────────────────────────────────────
        private static OrderResponseDto MapToDto(Order order) => new()
        {
            Id = order.Id,
            OrderNumber = $"ORD-{order.Id.ToString()[..8].ToUpper()}",
            Status = order.Status.ToString(),
            Total = order.Total,
            ShippingAddress = order.ShippingAddress,
            CreatedAt = order.CreatedAt,
            Items = order.OrderItems.Select(i => new OrderItemResponseDto
            {
                Id = i.Id,
                ProductId = i.ProductId,
                VariantId =i.VariantId,
                ProductName = i.ProductName,
                VariantColor = i.VariantColor,
                VariantSize = i.VariantSize,
                ImageUrl = i.Variant?.ImageUrl ?? i.Product?.ImageUrl,
                Quantity = i.Quantity,
                UnitPrice = i.UnitPrice,
                Total = i.UnitPrice * i.Quantity,
            })
        };
    }
}

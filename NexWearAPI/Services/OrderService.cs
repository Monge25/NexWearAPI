using Microsoft.EntityFrameworkCore;
using NexWearAPI.Data;
using NexWearAPI.DTOs;
using NexWearAPI.Models;
using System.Net;

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
                    throw new InvalidOperationException(
                        $"'{item.Product.Name} - {item.Variant.Color} {item.Variant.Size}' ya no está disponible.");

                if (item.Variant.Stock < item.Quantity)
                    throw new InvalidOperationException(
                        $"Stock insuficiente para '{item.Product.Name} - {item.Variant.Size}'. Disponible: {item.Variant.Stock}");
            }

            // 3. Resolver dirección — variables separadas para el snapshot
            string street, city, state, zipCode, country;
            string? interior, phone;

            if (dto.AddressId.HasValue)
            {
                // Usar dirección guardada
                var saved = await _context.Addresses
                    .FirstOrDefaultAsync(a => a.Id == dto.AddressId && a.UserId == userId);

                if (saved is null)
                    throw new InvalidOperationException("Dirección no encontrada.");

                street = saved.Street;
                interior = saved.Interior;
                city = saved.City;
                state = saved.State;
                zipCode = saved.ZipCode;
                country = saved.Country;
                phone = saved.Phone;
            }
            else
            {
                // Usar dirección escrita en el checkout
                if (string.IsNullOrWhiteSpace(dto.Street) || string.IsNullOrWhiteSpace(dto.City))
                    throw new InvalidOperationException("Se requiere una dirección de envío.");

                street = dto.Street!;
                interior = dto.Interior;
                city = dto.City!;
                state = dto.State ?? "";
                zipCode = dto.ZipCode ?? "";
                country = dto.Country ?? "México";
                phone = dto.Phone;

                // Guardar como nueva dirección si el usuario lo pidió
                if (dto.SaveAddress && !string.IsNullOrWhiteSpace(dto.AddressAlias))
                {
                    _context.Addresses.Add(new Address
                    {
                        UserId = userId,
                        Alias = dto.AddressAlias,
                        Street = street,
                        Interior = interior,
                        City = city,
                        State = state,
                        ZipCode = zipCode,
                        Country = country,
                        Phone = phone,
                    });
                }
            }

            // 4. Calcular items y total
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
                    ProductName = c.Product.Name,
                    VariantColor = c.Variant.Color,
                    VariantSize = c.Variant.Size,
                };
            }).ToList();

            var total = orderItems.Sum(i => i.UnitPrice * i.Quantity);

            // 5. Crear la orden con snapshot de dirección
            var order = new Order
            {
                UserId = userId,
                Status = OrderStatus.Paid,
                Total = total,
                Street = street,
                Interior = interior,
                City = city,
                State = state,
                ZipCode = zipCode,
                Country = country,
                Phone = phone,
                OrderItems = orderItems,
            };

            _context.Orders.Add(order);

            // 6. Descontar stock de cada variante
            foreach (var item in cartItems)
                item.Variant.Stock -= item.Quantity;

            // 7. Vaciar carrito automáticamente
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
            CreatedAt = order.CreatedAt,
            Street = order.Street,
            Interior = order.Interior,
            City = order.City,
            State = order.State,
            ZipCode = order.ZipCode,
            Country = order.Country,
            Phone = order.Phone,
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

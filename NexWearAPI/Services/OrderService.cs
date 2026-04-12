using Microsoft.EntityFrameworkCore;
using NexWearAPI.Data;
using NexWearAPI.DTOs;
using NexWearAPI.Models;

namespace NexWearAPI.Services
{
    public interface IOrderService
    {
        Task<OrderResponseDto> CheckoutAsync(Guid userId, CheckoutDto dto);
        Task<OrderResponseDto> CheckoutWithPaypalAsync(Guid userId, PaypalCheckoutDto dto);
        Task<IEnumerable<OrderResponseDto>> GetMyOrdersAsync(Guid userId);
        Task<OrderResponseDto?> GetByIdAsync(Guid userId, Guid orderId);
    }

    public class OrderService : IOrderService
    {
        private readonly AppDbContext _context;
        private readonly IPayPalService _paypal;

        public OrderService(AppDbContext context, IPayPalService paypal)
        {
            _context = context;
            _paypal = paypal;
        }

        // ── Checkout legacy (sin PayPal) ──────────────────────
        public async Task<OrderResponseDto> CheckoutAsync(Guid userId, CheckoutDto dto)
        {
            var cartItems = await GetAndValidateCart(userId);
            var orderItems = BuildOrderItems(cartItems);
            var total = orderItems.Sum(i => i.UnitPrice * i.Quantity);

            var order = new Order
            {
                UserId = userId,
                Status = OrderStatus.Pending,
                Total = total,
                ShippingAddress = dto.ShippingAddress,
                PaymentMethod = dto.PaymentMethod ?? "card",
                OrderItems = orderItems
            };

            await SaveOrder(order, cartItems);
            return MapToDto(order);
        }

        // ── Checkout con PayPal ───────────────────────────────
        public async Task<OrderResponseDto> CheckoutWithPaypalAsync(Guid userId, PaypalCheckoutDto dto)
        {
            // Validar método de pago
            if (dto.PaymentMethod != "card" && dto.PaymentMethod != "paypal")
                throw new InvalidOperationException("Método de pago inválido.");

            // 1. Verificar carrito
            var cartItems = await GetAndValidateCart(userId);
            var orderItems = BuildOrderItems(cartItems);
            var total = orderItems.Sum(i => i.UnitPrice * i.Quantity);

            // 2. Verificar pago con PayPal desde el servidor
            var isValid = await _paypal.VerifyOrderAsync(dto.PaypalOrderId, total);
            if (!isValid)
                throw new InvalidOperationException(
                    "No se pudo verificar el pago. Por favor intenta de nuevo.");

            // 3. Crear la orden como Pagada
            var order = new Order
            {
                UserId = userId,
                Status = OrderStatus.Paid,
                Total = total,
                ShippingAddress = dto.ShippingAddress,
                PaymentMethod = dto.PaymentMethod,
                PaypalOrderId = dto.PaypalOrderId,
                PaidAt = DateTime.UtcNow,
                OrderItems = orderItems
            };

            await SaveOrder(order, cartItems);
            return MapToDto(order);
        }

        public async Task<IEnumerable<OrderResponseDto>> GetMyOrdersAsync(Guid userId)
        {
            var orders = await _context.Orders
                .Include(o => o.OrderItems).ThenInclude(oi => oi.Variant)
                .Include(o => o.OrderItems).ThenInclude(oi => oi.Product)
                .Where(o => o.UserId == userId)
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync();

            return orders.Select(MapToDto);
        }

        public async Task<OrderResponseDto?> GetByIdAsync(Guid userId, Guid orderId)
        {
            var order = await _context.Orders
                .Include(o => o.OrderItems).ThenInclude(oi => oi.Variant)
                .Include(o => o.OrderItems).ThenInclude(oi => oi.Product)
                .FirstOrDefaultAsync(o => o.Id == orderId && o.UserId == userId);

            return order is null ? null : MapToDto(order);
        }

        // ── Helpers ───────────────────────────────────────────
        private async Task<List<CartItem>> GetAndValidateCart(Guid userId)
        {
            var cartItems = await _context.CartItems
                .Include(c => c.Product)
                .Include(c => c.Variant)
                .Where(c => c.UserId == userId)
                .ToListAsync();

            if (!cartItems.Any())
                throw new InvalidOperationException("El carrito se encuentra vacío.");

            foreach (var item in cartItems)
            {
                if (!item.Variant.IsActive)
                    throw new InvalidOperationException(
                        $"'{item.Product.Name} - {item.Variant.Color} {item.Variant.Size}' ya no está disponible.");

                if (item.Variant.Stock < item.Quantity)
                    throw new InvalidOperationException(
                        $"Stock insuficiente para '{item.Product.Name} - {item.Variant.Size}'. Disponible: {item.Variant.Stock}");
            }

            return cartItems;
        }

        private static List<OrderItem> BuildOrderItems(List<CartItem> cartItems) =>
            cartItems.Select(c =>
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
                    VariantSize = c.Variant.Size
                };
            }).ToList();

        private async Task SaveOrder(Order order, List<CartItem> cartItems)
        {
            _context.Orders.Add(order);

            foreach (var item in cartItems)
                item.Variant.Stock -= item.Quantity;

            _context.CartItems.RemoveRange(cartItems);
            await _context.SaveChangesAsync();
        }

        private static OrderResponseDto MapToDto(Order order) => new()
        {
            Id = order.Id,
            OrderNumber = $"ORD-{order.Id.ToString()[..8].ToUpper()}",
            Status = order.Status.ToString(),
            Total = order.Total,
            ShippingAddress = order.ShippingAddress,
            PaymentMethod = order.PaymentMethod,
            PaypalOrderId = order.PaypalOrderId,
            CreatedAt = order.CreatedAt,
            PaidAt = order.PaidAt,
            Items = order.OrderItems.Select(i => new OrderItemResponseDto
            {
                Id = i.Id,
                ProductId = i.ProductId,
                VariantId = i.VariantId,
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
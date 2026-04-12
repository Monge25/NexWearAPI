using Microsoft.EntityFrameworkCore;
using NexWearAPI.Data;
using NexWearAPI.DTOs;
using NexWearAPI.Models;

namespace NexWearAPI.Services
{
    public interface IOrderService
    {
        Task<OrderResponseDto> CheckoutAsync(Guid userId, MpCheckoutDto dto);
        Task<IEnumerable<OrderResponseDto>> GetMyOrdersAsync(Guid userId);
        Task<OrderResponseDto?> GetByIdAsync(Guid userId, Guid orderId);
    }

    public class OrderService : IOrderService
    {
        private readonly AppDbContext _context;
        private readonly IMercadoPagoService _mercadoPago;
        private readonly ILogger<OrderService> _logger;

        public OrderService(
            AppDbContext context,
            IMercadoPagoService mercadoPago,
            ILogger<OrderService> logger)
        {
            _context = context;
            _mercadoPago = mercadoPago;
            _logger = logger;
        }

        // ── Checkout: crear pago y guardar orden ──────────────────────────────────

        public async Task<OrderResponseDto> CheckoutAsync(Guid userId, MpCheckoutDto dto)
        {
            var cartItems = await GetAndValidateCart(userId);
            var orderItems = BuildOrderItems(cartItems);
            var total = orderItems.Sum(i => i.UnitPrice * i.Quantity);

            // Obtener email del usuario
            var user = await _context.Users.FindAsync(userId)
                ?? throw new InvalidOperationException("Usuario no encontrado.");

            // Crear pago en Mercado Pago
            var paymentId = await _mercadoPago.CreatePaymentAsync(
                total,
                "Compra NexWear",
                dto.Token,
                user.Email);

            // Verificar que el pago fue aprobado
            var payment = await _mercadoPago.GetPaymentAsync(long.Parse(paymentId));

            if (!payment.Success)
                throw new InvalidOperationException("El pago no fue aprobado. Intenta de nuevo.");

            // Guardar orden en BD
            var order = new Order
            {
                UserId = userId,
                Status = OrderStatus.Paid,
                Total = total,
                ShippingAddress = dto.ShippingAddress,
                PaymentMethod = "mercadopago",
                PaypalOrderId = paymentId,
                PaidAt = DateTime.UtcNow,
                OrderItems = orderItems
            };

            await SaveOrder(order, cartItems);

            _logger.LogInformation("Orden {OrderId} pagada con MP. PaymentId: {PaymentId}",
                order.Id, paymentId);

            return MapToDto(order);
        }

        // ── Consultas ─────────────────────────────────────────────────────────────

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

        // ── Helpers ───────────────────────────────────────────────────────────────

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
                        $"Stock insuficiente para '{item.Product.Name} - {item.Variant.Size}'. " +
                        $"Disponible: {item.Variant.Stock}");
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
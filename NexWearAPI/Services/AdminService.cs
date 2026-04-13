using Microsoft.EntityFrameworkCore;
using NexWearAPI.Data;
using NexWearAPI.DTOs;
using NexWearAPI.Models;

namespace NexWearAPI.Services
{
    public interface IAdminService
    {
        // Usuarios
        Task<AdminUsersResponseDto> GetUsersAsync(int page, int pageSize, string? search, string? role);
        Task<AdminUserResponseDto?> UpdateUserRoleAsync(Guid userId, string role);

        // Órdenes
        Task<AdminOrdersResponseDto> GetOrdersAsync(int page, int pageSize, string? status);
        Task<AdminOrderResponseDto?> GetOrderByIdAsync(Guid orderId);
        Task<AdminOrderResponseDto?> UpdateOrderStatusAsync(Guid orderId, string status);
    }

    public class AdminService : IAdminService
    {
        private readonly AppDbContext _context;

        public AdminService(AppDbContext context)
        {
            _context = context;
        }

        // ── Listar usuarios ───────────────────────────────────────
        public async Task<AdminUsersResponseDto> GetUsersAsync(
            int page, int pageSize, string? search, string? role)
        {
            var query = _context.Users.AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.ToLower().Trim();
                query = query.Where(u =>
                    u.Email.ToLower().Contains(s) ||
                    u.FirstName.ToLower().Contains(s) ||
                    u.LastName.ToLower().Contains(s));
            }

            if (!string.IsNullOrWhiteSpace(role) &&
                Enum.TryParse<UserRole>(role, true, out var roleEnum))
                query = query.Where(u => u.Role == roleEnum);

            var total = await query.CountAsync();

            var users = await query
                .OrderByDescending(u => u.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var userIds = users.Select(u => u.Id).ToList();
            var orderStats = await _context.Orders
                .Where(o => userIds.Contains(o.UserId))
                .GroupBy(o => o.UserId)
                .Select(g => new
                {
                    UserId = g.Key,
                    TotalOrders = g.Count(),
                    TotalSpent = g.Sum(o => o.Total),
                    LastOrderDate = g.Max(o => o.CreatedAt)
                })
                .ToListAsync();

            var dtos = users.Select(u =>
            {
                var stats = orderStats.FirstOrDefault(s => s.UserId == u.Id);
                return new AdminUserResponseDto
                {
                    Id = u.Id,
                    Email = u.Email,
                    FirstName = u.FirstName,
                    LastName = u.LastName,
                    Role = u.Role.ToString(),
                    // IsActive = u.IsActive,
                    CreatedAt = u.CreatedAt,
                    TotalOrders = stats?.TotalOrders ?? 0,
                    TotalSpent = stats?.TotalSpent ?? 0,
                    LastOrderDate = stats?.LastOrderDate.ToString("dd/MM/yyyy") ?? "Sin órdenes",
                };
            });

            return new AdminUsersResponseDto
            {
                Users = dtos,
                Total = total,
                Page = page,
                PageSize = pageSize,
                TotalPages = (int)Math.Ceiling((double)total / pageSize),
            };
        }

        // ── Cambiar rol de usuario ────────────────────────────────
        public async Task<AdminUserResponseDto?> UpdateUserRoleAsync(Guid userId, string role)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user is null) return null;

            if (!Enum.TryParse<UserRole>(role, true, out var roleEnum))
                return null;

            user.Role = roleEnum;
            await _context.SaveChangesAsync();

            return new AdminUserResponseDto
            {
                Id = user.Id,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Role = user.Role.ToString(),
                // IsActive = user.IsActive,
                CreatedAt = user.CreatedAt,
            };
        }

        // ── Listar todas las órdenes ──────────────────────────────
        public async Task<AdminOrdersResponseDto> GetOrdersAsync(
            int page, int pageSize, string? status)
        {
            var query = _context.Orders
                .Include(o => o.User)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(status) &&
                Enum.TryParse<OrderStatus>(status, true, out var statusEnum))
                query = query.Where(o => o.Status == statusEnum);

            var total = await query.CountAsync();

            var orders = await query
                .OrderByDescending(o => o.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Variant)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                .ToListAsync();

            return new AdminOrdersResponseDto
            {
                Orders = orders.Select(MapOrderToDto),
                Total = total,
                Page = page,
                PageSize = pageSize,
                TotalPages = (int)Math.Ceiling((double)total / pageSize),
            };
        }

        // ── Detalle de una orden ──────────────────────────────────
        public async Task<AdminOrderResponseDto?> GetOrderByIdAsync(Guid orderId)
        {
            var order = await _context.Orders
                .Include(o => o.User)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Variant)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                .FirstOrDefaultAsync(o => o.Id == orderId);

            return order is null ? null : MapOrderToDto(order);
        }

        // ── Cambiar estado de una orden ───────────────────────────
        public async Task<AdminOrderResponseDto?> UpdateOrderStatusAsync(Guid orderId, string status)
        {
            var order = await _context.Orders
                .Include(o => o.User)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Variant)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                .FirstOrDefaultAsync(o => o.Id == orderId);

            if (order is null) return null;

            if (!Enum.TryParse<OrderStatus>(status, true, out var statusEnum))
                return null;

            order.Status = statusEnum;
            await _context.SaveChangesAsync();

            return MapOrderToDto(order);
        }

        // ── Mapper ────────────────────────────────────────────────
        private static AdminOrderResponseDto MapOrderToDto(Order o) => new()
        {
            Id = o.Id,
            OrderNumber = $"ORD-{o.Id.ToString()[..8].ToUpper()}",
            Status = o.Status.ToString(),
            Total = o.Total,
            CreatedAt = o.CreatedAt,
            CustomerEmail = o.User?.Email ?? "",
            CustomerName = $"{o.User?.FirstName} {o.User?.LastName}".Trim(),
            Street = o.Street,
            Interior = o.Interior,
            City = o.City,
            State = o.State,
            ZipCode = o.ZipCode,
            Country = o.Country,
            Phone = o.Phone,
            Items = o.OrderItems.Select(i => new AdminOrderItemDto
            {
                Id = i.Id,
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

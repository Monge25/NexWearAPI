namespace NexWearAPI.DTOs
{
    // ── Usuarios ──────────────────────────────────────────────────
    public class AdminUserResponseDto
    {
        public Guid Id { get; set; }
        public string Email { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        // public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public int TotalOrders { get; set; }
        public decimal TotalSpent { get; set; }
        public string LastOrderDate { get; set; } = "Sin órdenes";
    }

    public class AdminUsersResponseDto
    {
        public IEnumerable<AdminUserResponseDto> Users { get; set; } = new List<AdminUserResponseDto>();
        public int Total { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }
    }

    // ── Órdenes ───────────────────────────────────────────────────
    public class AdminOrderResponseDto
    {
        public Guid Id { get; set; }
        public string OrderNumber { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public decimal Total { get; set; }
        public DateTime CreatedAt { get; set; }

        // Info del cliente
        public string CustomerEmail { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;

        // Dirección de envío
        public string Street { get; set; } = string.Empty;
        public string? Interior { get; set; }
        public string City { get; set; } = string.Empty;
        public string State { get; set; } = string.Empty;
        public string ZipCode { get; set; } = string.Empty;
        public string Country { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public string FullAddress =>
            $"{Street}{(Interior != null ? $", {Interior}" : "")}, {City}, {State} {ZipCode}, {Country}";

        // Items de la orden
        public IEnumerable<AdminOrderItemDto> Items { get; set; } = new List<AdminOrderItemDto>();
    }

    public class AdminOrderItemDto
    {
        public Guid Id { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string? VariantColor { get; set; }
        public string? VariantSize { get; set; }
        public string? ImageUrl { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal Total { get; set; }
    }

    public class AdminOrdersResponseDto
    {
        public IEnumerable<AdminOrderResponseDto> Orders { get; set; } = new List<AdminOrderResponseDto>();
        public int Total { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }
    }

    // ── Cambiar estado de orden ───────────────────────────────────
    public class UpdateOrderStatusDto
    {
        [System.ComponentModel.DataAnnotations.Required]
        public string Status { get; set; } = string.Empty;
    }

    // ── Cambiar rol de usuario ────────────────────────────────────
    public class UpdateUserRoleDto
    {
        [System.ComponentModel.DataAnnotations.Required]
        public string Role { get; set; } = string.Empty;
    }

    // Auditlogs
    public class AuditLogResponseDto
    {
        public Guid Id { get; set; }
        public string? UserEmail { get; set; }
        public Guid? UserId { get; set; }
        public string Action { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string Result { get; set; } = string.Empty;
        public string? Details { get; set; }
        public string? IpAddress { get; set; }
        public string? UserAgent { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class AuditLogsResponseDto
    {
        public IEnumerable<AuditLogResponseDto> Logs { get; set; } = new List<AuditLogResponseDto>();
        public int Total { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }
    }
}

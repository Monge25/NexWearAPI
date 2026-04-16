using NexWearAPI.Data;
using NexWearAPI.Models;
using System.Text.Json;

namespace NexWearAPI.Services
{
    public interface IAuditService
    {
        Task LogAsync(
            string action,
            string category,
            string result = "SUCCESS",
            object? details = null,
            Guid? userId = null,
            string? userEmail = null,
            string? ipAddress = null,
            string? userAgent = null
        );
    }
    public class AuditService : IAuditService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<AuditService> _logger;

        public AuditService(AppDbContext context, ILogger<AuditService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task LogAsync(
            string action,
            string category,
            string result = "SUCCESS",
            object? details = null,
            Guid? userId = null,
            string? userEmail = null,
            string? ipAddress = null,
            string? userAgent = null)
        {
            try
            {
                _context.AuditLogs.Add(new AuditLog
                {
                    Action = action,
                    Category = category,
                    Result = result,
                    Details = details is string s ? s : JsonSerializer.Serialize(details),
                    UserId = userId,
                    UserEmail = userEmail,
                    IpAddress = ipAddress,
                    UserAgent = userAgent,
                });
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                // Nunca fallar la operacion principal por un log fallido
                _logger.LogError("Error al guardar AuditLog: {Error}", ex.Message);
            }
        }
    }
}

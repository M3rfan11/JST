using Microsoft.EntityFrameworkCore;
using Api.Data;
using Api.Models;

namespace Api.Services;

public class AuditService : IAuditService
{
    private readonly ApplicationDbContext _context;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public AuditService(ApplicationDbContext context, IHttpContextAccessor httpContextAccessor)
    {
        _context = context;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task LogAsync(string entity, string entityId, string action, string? before = null, string? after = null, int? actorUserId = null)
    {
        try
        {
            var httpContext = _httpContextAccessor.HttpContext;
            var ipAddress = httpContext?.Connection.RemoteIpAddress?.ToString();
            var userAgent = httpContext?.Request.Headers.UserAgent.ToString();

            var auditLog = new AuditLog
            {
                ActorUserId = actorUserId,
                Entity = entity,
                EntityId = entityId,
                Action = action,
                Before = before,
                After = after,
                At = DateTime.UtcNow,
                IpAddress = ipAddress,
                UserAgent = userAgent
            };

            _context.AuditLogs.Add(auditLog);
            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            // Log the error but don't throw to avoid breaking the main operation
            // In a real application, you might want to use a proper logging framework
            Console.WriteLine($"Audit logging failed: {ex.Message}");
        }
    }
}

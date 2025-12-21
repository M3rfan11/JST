using System.Security.Claims;
using Api.Services;

namespace Api.Middleware;

public class AuditMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<AuditMiddleware> _logger;

    public AuditMiddleware(RequestDelegate next, ILogger<AuditMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, IAuditService auditService)
    {
        // Skip audit logging for certain paths
        if (ShouldSkipAudit(context.Request.Path))
        {
            await _next(context);
            return;
        }

        var userId = GetUserId(context);
        var method = context.Request.Method;
        var path = context.Request.Path.Value ?? "";

        // Log the request
        await auditService.LogAsync(
            "Request",
            $"{method}:{path}",
            "Request",
            actorUserId: userId
        );

        await _next(context);
    }

    private static bool ShouldSkipAudit(PathString path)
    {
        var skipPaths = new[]
        {
            "/swagger",
            "/health",
            "/favicon.ico",
            "/auth/login",
            "/auth/refresh"
        };

        return skipPaths.Any(skipPath => path.StartsWithSegments(skipPath));
    }

    private static int? GetUserId(HttpContext context)
    {
        var userIdClaim = context.User.FindFirst(ClaimTypes.NameIdentifier);
        return userIdClaim != null && int.TryParse(userIdClaim.Value, out var userId) ? userId : null;
    }
}

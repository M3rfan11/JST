using Api.Models;

namespace Api.Services;

public interface IAuditService
{
    Task LogAsync(string entity, string entityId, string action, string? before = null, string? after = null, int? actorUserId = null);
}

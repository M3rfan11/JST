using Microsoft.AspNetCore.Authorization;
using Api.Handlers;

namespace Api.Attributes;

/// <summary>
/// Custom authorization attribute that requires a specific permission
/// SuperAdmin role automatically bypasses all permission checks
/// Usage: [RequirePermission("Products", "Create")]
/// </summary>
public class RequirePermissionAttribute : AuthorizeAttribute
{
    public string Resource { get; }
    public string Action { get; }

    public RequirePermissionAttribute(string resource, string action)
    {
        Resource = resource;
        Action = action;
        // Use a policy name that can be dynamically handled
        Policy = $"Permission:{resource}:{action}";
    }
}


using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Api.Data;
using Api.DTOs;
using Api.Services;

namespace Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PermissionsController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IAuditService _auditService;
    private readonly ILogger<PermissionsController> _logger;

    public PermissionsController(ApplicationDbContext context, IAuditService auditService, ILogger<PermissionsController> logger)
    {
        _context = context;
        _auditService = auditService;
        _logger = logger;
    }

    [HttpGet]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<ActionResult<IEnumerable<PermissionResponse>>> GetPermissions()
    {
        try
        {
            var permissions = await _context.Permissions
                .OrderBy(p => p.Resource)
                .ThenBy(p => p.Action)
                .Select(p => new PermissionResponse
                {
                    Id = p.Id,
                    Name = p.Name,
                    Description = p.Description,
                    Resource = p.Resource,
                    Action = p.Action,
                    CreatedAt = p.CreatedAt
                })
                .ToListAsync();

            return Ok(permissions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving permissions");
            return StatusCode(500, new { message = "An error occurred while retrieving permissions" });
        }
    }

    [HttpGet("by-resource")]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<ActionResult<Dictionary<string, List<PermissionResponse>>>> GetPermissionsByResource()
    {
        try
        {
            var permissions = await _context.Permissions
                .OrderBy(p => p.Resource)
                .ThenBy(p => p.Action)
                .Select(p => new PermissionResponse
                {
                    Id = p.Id,
                    Name = p.Name,
                    Description = p.Description,
                    Resource = p.Resource,
                    Action = p.Action,
                    CreatedAt = p.CreatedAt
                })
                .ToListAsync();

            var grouped = permissions
                .GroupBy(p => p.Resource)
                .ToDictionary(g => g.Key, g => g.ToList());

            return Ok(grouped);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving permissions by resource");
            return StatusCode(500, new { message = "An error occurred while retrieving permissions by resource" });
        }
    }

    [HttpGet("my-permissions")]
    [Authorize] // Any authenticated user can check their own permissions
    public async Task<ActionResult<Dictionary<string, List<string>>>> GetMyPermissions()
    {
        try
        {
            _logger.LogInformation("GetMyPermissions called. User authenticated: {IsAuthenticated}, Identity name: {IdentityName}", 
                User.Identity?.IsAuthenticated ?? false, User.Identity?.Name);
            
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out var userId))
            {
                _logger.LogWarning("GetMyPermissions: User ID claim not found or invalid. Claims: {Claims}", 
                    string.Join(", ", User.Claims.Select(c => $"{c.Type}={c.Value}")));
                return Unauthorized(new { message = "User not found" });
            }
            
            _logger.LogInformation("GetMyPermissions: User ID {UserId} found", userId);

            // Check if user has SuperAdmin role - SuperAdmin has all permissions
            var userRoles = await _context.UserRoles
                .Where(ur => ur.UserId == userId)
                .Include(ur => ur.Role)
                .Select(ur => ur.Role.Name)
                .ToListAsync();

            if (userRoles.Contains("SuperAdmin"))
            {
                // SuperAdmin has access to all resources
                var allResources = new[] { "Products", "Categories", "Users", "Roles", "Inventory", "Orders", "Warehouses", "Reports" };
                var allActions = new[] { "Create", "Update", "Delete", "Read" };
                
                var result = allResources.ToDictionary(
                    resource => resource,
                    resource => allActions.ToList()
                );
                
                return Ok(result);
            }

            // Get user's permissions through their roles
            var permissions = await _context.UserRoles
                .Where(ur => ur.UserId == userId)
                .SelectMany(ur => ur.Role.RolePermissions)
                .Select(rp => new { rp.Permission.Resource, rp.Permission.Action })
                .Distinct()
                .ToListAsync();

            // Group by resource
            var grouped = permissions
                .GroupBy(p => p.Resource)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(p => p.Action).Distinct().ToList()
                );

            return Ok(grouped);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving user permissions");
            return StatusCode(500, new { message = "An error occurred while retrieving permissions" });
        }
    }

    [HttpGet("{id:int}")]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<ActionResult<PermissionResponse>> GetPermission(int id)
    {
        try
        {
            var permission = await _context.Permissions
                .Where(p => p.Id == id)
                .Select(p => new PermissionResponse
                {
                    Id = p.Id,
                    Name = p.Name,
                    Description = p.Description,
                    Resource = p.Resource,
                    Action = p.Action,
                    CreatedAt = p.CreatedAt
                })
                .FirstOrDefaultAsync();

            if (permission == null)
            {
                return NotFound(new { message = "Permission not found" });
            }

            return Ok(permission);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving permission with ID: {PermissionId}", id);
            return StatusCode(500, new { message = "An error occurred while retrieving the permission" });
        }
    }

    private int? GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        return userIdClaim != null && int.TryParse(userIdClaim.Value, out var userId) ? userId : null;
    }
}

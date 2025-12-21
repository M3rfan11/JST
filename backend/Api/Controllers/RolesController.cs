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
[Authorize(Roles = "SuperAdmin")]
public class RolesController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IAuditService _auditService;
    private readonly ILogger<RolesController> _logger;

    public RolesController(ApplicationDbContext context, IAuditService auditService, ILogger<RolesController> logger)
    {
        _context = context;
        _auditService = auditService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<RoleResponse>>> GetRoles()
    {
        try
        {
            var roles = await _context.Roles
                .Select(r => new RoleResponse
                {
                    Id = r.Id,
                    Name = r.Name,
                    Description = r.Description,
                    CreatedAt = r.CreatedAt
                })
                .ToListAsync();

            return Ok(roles);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving roles");
            return StatusCode(500, new { message = "An error occurred while retrieving roles" });
        }
    }

    [HttpGet("{id}/with-permissions")]
    public async Task<ActionResult<RoleWithPermissionsResponse>> GetRoleWithPermissions(int id)
    {
        try
        {
            var role = await _context.Roles
                .Include(r => r.RolePermissions)
                .ThenInclude(rp => rp.Permission)
                .Where(r => r.Id == id)
                .FirstOrDefaultAsync();

            if (role == null)
            {
                return NotFound(new { message = "Role not found" });
            }

            var response = new RoleWithPermissionsResponse
            {
                Id = role.Id,
                Name = role.Name,
                Description = role.Description,
                CreatedAt = role.CreatedAt,
                Permissions = role.RolePermissions
                    .Select(rp => new PermissionResponse
                    {
                        Id = rp.Permission.Id,
                        Name = rp.Permission.Name,
                        Description = rp.Permission.Description,
                        Resource = rp.Permission.Resource,
                        Action = rp.Permission.Action,
                        CreatedAt = rp.Permission.CreatedAt
                    })
                    .ToList()
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving role with permissions for ID: {RoleId}", id);
            return StatusCode(500, new { message = "An error occurred while retrieving the role with permissions" });
        }
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<RoleResponse>> GetRole(int id)
    {
        try
        {
            var role = await _context.Roles
                .Where(r => r.Id == id)
                .Select(r => new RoleResponse
                {
                    Id = r.Id,
                    Name = r.Name,
                    Description = r.Description,
                    CreatedAt = r.CreatedAt
                })
                .FirstOrDefaultAsync();

            if (role == null)
            {
                return NotFound(new { message = "Role not found" });
            }

            return Ok(role);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving role with ID: {RoleId}", id);
            return StatusCode(500, new { message = "An error occurred while retrieving the role" });
        }
    }

    [HttpPost]
    public async Task<ActionResult<RoleResponse>> CreateRole([FromBody] CreateRoleRequest request)
    {
        try
        {
            // Check if role name already exists
            if (await _context.Roles.AnyAsync(r => r.Name == request.Name))
            {
                return Conflict(new { message = "Role name already exists" });
            }

            var role = new Api.Models.Role
            {
                Name = request.Name,
                Description = request.Description,
                CreatedAt = DateTime.UtcNow
            };

            _context.Roles.Add(role);
            await _context.SaveChangesAsync();

            var response = new RoleResponse
            {
                Id = role.Id,
                Name = role.Name,
                Description = role.Description,
                CreatedAt = role.CreatedAt
            };

            // Audit log
            var currentUserId = GetCurrentUserId();
            await _auditService.LogAsync(
                "Role",
                role.Id.ToString(),
                "Create",
                after: System.Text.Json.JsonSerializer.Serialize(response),
                actorUserId: currentUserId
            );

            return CreatedAtAction(nameof(GetRole), new { id = role.Id }, response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating role with name: {RoleName}", request.Name);
            return StatusCode(500, new { message = "An error occurred while creating the role" });
        }
    }

    [HttpPatch("{id}")]
    public async Task<ActionResult<RoleResponse>> UpdateRole(int id, [FromBody] UpdateRoleRequest request)
    {
        try
        {
            var role = await _context.Roles.FindAsync(id);
            if (role == null)
            {
                return NotFound(new { message = "Role not found" });
            }

            var before = System.Text.Json.JsonSerializer.Serialize(new RoleResponse
            {
                Id = role.Id,
                Name = role.Name,
                Description = role.Description,
                CreatedAt = role.CreatedAt
            });

            // Update fields if provided
            if (!string.IsNullOrEmpty(request.Name))
            {
                if (await _context.Roles.AnyAsync(r => r.Name == request.Name && r.Id != id))
                {
                    return Conflict(new { message = "Role name already exists" });
                }
                role.Name = request.Name;
            }
            
            if (request.Description != null)
                role.Description = request.Description;

            await _context.SaveChangesAsync();

            var after = System.Text.Json.JsonSerializer.Serialize(new RoleResponse
            {
                Id = role.Id,
                Name = role.Name,
                Description = role.Description,
                CreatedAt = role.CreatedAt
            });

            // Audit log
            var currentUserId = GetCurrentUserId();
            await _auditService.LogAsync(
                "Role",
                role.Id.ToString(),
                "Update",
                before: before,
                after: after,
                actorUserId: currentUserId
            );

            return Ok(new RoleResponse
            {
                Id = role.Id,
                Name = role.Name,
                Description = role.Description,
                CreatedAt = role.CreatedAt
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating role with ID: {RoleId}", id);
            return StatusCode(500, new { message = "An error occurred while updating the role" });
        }
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteRole(int id)
    {
        try
        {
            var role = await _context.Roles.FindAsync(id);
            if (role == null)
            {
                return NotFound(new { message = "Role not found" });
            }

            // Check if role is assigned to any users
            var hasUsers = await _context.UserRoles.AnyAsync(ur => ur.RoleId == id);
            if (hasUsers)
            {
                return Conflict(new { message = "Cannot delete role that is assigned to users" });
            }

            var before = System.Text.Json.JsonSerializer.Serialize(new RoleResponse
            {
                Id = role.Id,
                Name = role.Name,
                Description = role.Description,
                CreatedAt = role.CreatedAt
            });

            _context.Roles.Remove(role);
            await _context.SaveChangesAsync();

            // Audit log
            var currentUserId = GetCurrentUserId();
            await _auditService.LogAsync(
                "Role",
                id.ToString(),
                "Delete",
                before: before,
                actorUserId: currentUserId
            );

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting role with ID: {RoleId}", id);
            return StatusCode(500, new { message = "An error occurred while deleting the role" });
        }
    }

    [HttpPost("{id}/permissions")]
    public async Task<ActionResult> AssignPermissionToRole(int id, [FromBody] AssignPermissionToRoleRequest request)
    {
        try
        {
            var role = await _context.Roles.FindAsync(id);
            if (role == null)
            {
                return NotFound(new { message = "Role not found" });
            }

            var permission = await _context.Permissions.FindAsync(request.PermissionId);
            if (permission == null)
            {
                return NotFound(new { message = "Permission not found" });
            }

            // Check if permission is already assigned to this role
            var existingRolePermission = await _context.RolePermissions
                .FirstOrDefaultAsync(rp => rp.RoleId == id && rp.PermissionId == request.PermissionId);

            if (existingRolePermission != null)
            {
                return Conflict(new { message = "Permission is already assigned to this role" });
            }

            var rolePermission = new Api.Models.RolePermission
            {
                RoleId = id,
                PermissionId = request.PermissionId,
                AssignedAt = DateTime.UtcNow
            };

            _context.RolePermissions.Add(rolePermission);
            await _context.SaveChangesAsync();

            // Audit log
            var currentUserId = GetCurrentUserId();
            await _auditService.LogAsync(
                "RolePermission",
                $"{id}:{request.PermissionId}",
                "Assign",
                after: $"Role {role.Name} assigned permission {permission.Name}",
                actorUserId: currentUserId
            );

            return Ok(new { message = "Permission assigned to role successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error assigning permission {PermissionId} to role {RoleId}", request.PermissionId, id);
            return StatusCode(500, new { message = "An error occurred while assigning the permission to role" });
        }
    }

    [HttpDelete("{id}/permissions/{permissionId}")]
    public async Task<ActionResult> RemovePermissionFromRole(int id, int permissionId)
    {
        try
        {
            var rolePermission = await _context.RolePermissions
                .Include(rp => rp.Role)
                .Include(rp => rp.Permission)
                .FirstOrDefaultAsync(rp => rp.RoleId == id && rp.PermissionId == permissionId);

            if (rolePermission == null)
            {
                return NotFound(new { message = "Role permission not found" });
            }

            _context.RolePermissions.Remove(rolePermission);
            await _context.SaveChangesAsync();

            // Audit log
            var currentUserId = GetCurrentUserId();
            await _auditService.LogAsync(
                "RolePermission",
                $"{id}:{permissionId}",
                "Remove",
                before: $"Role {rolePermission.Role.Name} had permission {rolePermission.Permission.Name}",
                actorUserId: currentUserId
            );

            return Ok(new { message = "Permission removed from role successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing permission {PermissionId} from role {RoleId}", permissionId, id);
            return StatusCode(500, new { message = "An error occurred while removing the permission from role" });
        }
    }

    private int? GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        return userIdClaim != null && int.TryParse(userIdClaim.Value, out var userId) ? userId : null;
    }
}

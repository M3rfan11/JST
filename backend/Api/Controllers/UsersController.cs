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
[Authorize]
public class UsersController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IAuditService _auditService;
    private readonly ILogger<UsersController> _logger;

    public UsersController(ApplicationDbContext context, IAuditService auditService, ILogger<UsersController> logger)
    {
        _context = context;
        _auditService = auditService;
        _logger = logger;
    }

    [HttpGet]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<ActionResult<IEnumerable<UserResponse>>> GetUsers()
    {
        try
        {
            var currentUserId = GetCurrentUserId();
            var currentUserRoles = await GetCurrentUserRoles(currentUserId);
            
            IQueryable<Api.Models.User> usersQuery = _context.Users
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role);

            // Apply role-based filtering
            if (currentUserRoles.Contains("SuperAdmin"))
            {
                // SuperAdmin can see all users
                usersQuery = usersQuery.Where(u => u.IsActive);
            }
            else
            {
                return StatusCode(403, new { Message = "You don't have permission to view users" });
            }

            var users = await usersQuery
                .Select(u => new UserResponse
                {
                    Id = u.Id,
                    FullName = u.FullName,
                    Email = u.Email,
                    IsActive = u.IsActive,
                    CreatedAt = u.CreatedAt,
                    UpdatedAt = u.UpdatedAt,
                    Roles = u.UserRoles.Select(ur => ur.Role.Name).ToList(),
                })
                .ToListAsync();

            return Ok(users);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving users");
            return StatusCode(500, new { message = "An error occurred while retrieving users" });
        }
    }

    [HttpGet("{id}")]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<ActionResult<UserResponse>> GetUser(int id)
    {
        try
        {
            var currentUserId = GetCurrentUserId();
            var currentUserRoles = await GetCurrentUserRoles(currentUserId);
            
            IQueryable<Api.Models.User> userQuery = _context.Users
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .Where(u => u.Id == id);

            // Apply role-based filtering
            if (currentUserRoles.Contains("SuperAdmin"))
            {
                // SuperAdmin can see any user
                userQuery = userQuery.Where(u => u.IsActive);
            }
            else
            {
                return StatusCode(403, new { Message = "You don't have permission to view this user" });
            }

            var user = await userQuery
                .Select(u => new UserResponse
                {
                    Id = u.Id,
                    FullName = u.FullName,
                    Email = u.Email,
                    IsActive = u.IsActive,
                    CreatedAt = u.CreatedAt,
                    UpdatedAt = u.UpdatedAt,
                    Roles = u.UserRoles.Select(ur => ur.Role.Name).ToList(),
                })
                .FirstOrDefaultAsync();

            if (user == null)
            {
                return NotFound(new { message = "User not found" });
            }

            return Ok(user);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving user with ID: {UserId}", id);
            return StatusCode(500, new { message = "An error occurred while retrieving the user" });
        }
    }

    [HttpPost]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<ActionResult<UserResponse>> CreateUser([FromBody] CreateUserRequest request)
    {
        try
        {
            // Check if email already exists
            if (await _context.Users.AnyAsync(u => u.Email == request.Email))
            {
                return Conflict(new { message = "Email already exists" });
            }

            var user = new Api.Models.User
            {
                FullName = request.FullName,
                Email = request.Email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
                IsActive = request.IsActive,
                CreatedAt = DateTime.UtcNow
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            // Assign default "Customer" role if no roles are explicitly provided
            // (SuperAdmin can assign roles manually through role management)
            var customerRole = await _context.Roles.FirstOrDefaultAsync(r => r.Name == "Customer");
            if (customerRole != null)
            {
                var userRole = new Api.Models.UserRole
                {
                    UserId = user.Id,
                    RoleId = customerRole.Id,
                    AssignedAt = DateTime.UtcNow
                };
                _context.UserRoles.Add(userRole);
                await _context.SaveChangesAsync();
            }

            // Load the user with roles for response
            await _context.Entry(user)
                .Collection(u => u.UserRoles)
                .LoadAsync();

            var response = new UserResponse
            {
                Id = user.Id,
                FullName = user.FullName,
                Email = user.Email,
                IsActive = user.IsActive,
                CreatedAt = user.CreatedAt,
                UpdatedAt = user.UpdatedAt,
                Roles = user.UserRoles.Select(ur => ur.Role.Name).ToList()
            };

            // Audit log
            var currentUserId = GetCurrentUserId();
            await _auditService.LogAsync(
                "User",
                user.Id.ToString(),
                "Create",
                after: System.Text.Json.JsonSerializer.Serialize(response),
                actorUserId: currentUserId
            );

            return CreatedAtAction(nameof(GetUser), new { id = user.Id }, response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating user with email: {Email}", request.Email);
            return StatusCode(500, new { message = "An error occurred while creating the user" });
        }
    }

    [HttpPatch("{id}")]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<ActionResult<UserResponse>> UpdateUser(int id, [FromBody] UpdateUserRequest request)
    {
        try
        {
            var user = await _context.Users
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.Id == id);

            if (user == null)
            {
                return NotFound(new { message = "User not found" });
            }

            var before = System.Text.Json.JsonSerializer.Serialize(new UserResponse
            {
                Id = user.Id,
                FullName = user.FullName,
                Email = user.Email,
                IsActive = user.IsActive,
                CreatedAt = user.CreatedAt,
                UpdatedAt = user.UpdatedAt,
                Roles = user.UserRoles.Select(ur => ur.Role.Name).ToList()
            });

            // Update fields if provided
            if (!string.IsNullOrEmpty(request.FullName))
                user.FullName = request.FullName;
            
            if (!string.IsNullOrEmpty(request.Email))
            {
                if (await _context.Users.AnyAsync(u => u.Email == request.Email && u.Id != id))
                {
                    return Conflict(new { message = "Email already exists" });
                }
                user.Email = request.Email;
            }
            
            if (!string.IsNullOrEmpty(request.Password))
                user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);
            
            if (request.IsActive.HasValue)
                user.IsActive = request.IsActive.Value;

            user.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            var after = System.Text.Json.JsonSerializer.Serialize(new UserResponse
            {
                Id = user.Id,
                FullName = user.FullName,
                Email = user.Email,
                IsActive = user.IsActive,
                CreatedAt = user.CreatedAt,
                UpdatedAt = user.UpdatedAt,
                Roles = user.UserRoles.Select(ur => ur.Role.Name).ToList()
            });

            // Audit log
            var currentUserId = GetCurrentUserId();
            await _auditService.LogAsync(
                "User",
                user.Id.ToString(),
                "Update",
                before: before,
                after: after,
                actorUserId: currentUserId
            );

            return Ok(new UserResponse
            {
                Id = user.Id,
                FullName = user.FullName,
                Email = user.Email,
                IsActive = user.IsActive,
                CreatedAt = user.CreatedAt,
                UpdatedAt = user.UpdatedAt,
                Roles = user.UserRoles.Select(ur => ur.Role.Name).ToList()
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating user with ID: {UserId}", id);
            return StatusCode(500, new { message = "An error occurred while updating the user" });
        }
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<ActionResult> DeleteUser(int id)
    {
        try
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                return NotFound(new { message = "User not found" });
            }

            var before = System.Text.Json.JsonSerializer.Serialize(new UserResponse
            {
                Id = user.Id,
                FullName = user.FullName,
                Email = user.Email,
                IsActive = user.IsActive,
                CreatedAt = user.CreatedAt,
                UpdatedAt = user.UpdatedAt
            });

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();

            // Audit log
            var currentUserId = GetCurrentUserId();
            await _auditService.LogAsync(
                "User",
                id.ToString(),
                "Delete",
                before: before,
                actorUserId: currentUserId
            );

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting user with ID: {UserId}", id);
            return StatusCode(500, new { message = "An error occurred while deleting the user" });
        }
    }

    [HttpPost("{id}/roles")]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<ActionResult> AssignRole(int id, [FromBody] AssignRoleRequest request)
    {
        try
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                return NotFound(new { message = "User not found" });
            }

            var role = await _context.Roles.FindAsync(request.RoleId);
            if (role == null)
            {
                return NotFound(new { message = "Role not found" });
            }

            // Check if user already has this role
            var existingUserRole = await _context.UserRoles
                .FirstOrDefaultAsync(ur => ur.UserId == id && ur.RoleId == request.RoleId);

            if (existingUserRole != null)
            {
                return Conflict(new { message = "User already has this role" });
            }

            var userRole = new Api.Models.UserRole
            {
                UserId = id,
                RoleId = request.RoleId,
                AssignedAt = DateTime.UtcNow
            };

            _context.UserRoles.Add(userRole);
            await _context.SaveChangesAsync();

            // Audit log
            var currentUserId = GetCurrentUserId();
            await _auditService.LogAsync(
                "UserRole",
                $"{id}:{request.RoleId}",
                "Assign",
                after: $"User {user.FullName} assigned role {role.Name}",
                actorUserId: currentUserId
            );

            return Ok(new { message = "Role assigned successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error assigning role {RoleId} to user {UserId}", request.RoleId, id);
            return StatusCode(500, new { message = "An error occurred while assigning the role" });
        }
    }

    [HttpDelete("{id}/roles/{roleId}")]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<ActionResult> RemoveRole(int id, int roleId)
    {
        try
        {
            var userRole = await _context.UserRoles
                .Include(ur => ur.User)
                .Include(ur => ur.Role)
                .FirstOrDefaultAsync(ur => ur.UserId == id && ur.RoleId == roleId);

            if (userRole == null)
            {
                return NotFound(new { message = "User role not found" });
            }

            _context.UserRoles.Remove(userRole);
            await _context.SaveChangesAsync();

            // Audit log
            var currentUserId = GetCurrentUserId();
            await _auditService.LogAsync(
                "UserRole",
                $"{id}:{roleId}",
                "Remove",
                before: $"User {userRole.User.FullName} had role {userRole.Role.Name}",
                actorUserId: currentUserId
            );

            return Ok(new { message = "Role removed successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing role {RoleId} from user {UserId}", roleId, id);
            return StatusCode(500, new { message = "An error occurred while removing the role" });
        }
    }

    private int? GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        return userIdClaim != null && int.TryParse(userIdClaim.Value, out var userId) ? userId : null;
    }

    private async Task<List<string>> GetCurrentUserRoles(int? userId)
    {
        if (userId == null) return new List<string>();
        
        return await _context.UserRoles
            .Where(ur => ur.UserId == userId)
            .Select(ur => ur.Role.Name)
            .ToListAsync();
    }

    /// <summary>
    /// Assign user to store (StoreManager only)
    /// </summary>
    [HttpPost("{id}/assign-store")]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<ActionResult> AssignUserToStore(int id, [FromBody] AssignUserToStoreRequest request)
    {
        try
        {
            var currentUserId = GetCurrentUserId();
            var currentUserRoles = await GetCurrentUserRoles(currentUserId);
            
            // Check if user has permission to assign users to stores
            if (!currentUserRoles.Contains("SuperAdmin") && !currentUserRoles.Contains("StoreManager"))
            {
                return StatusCode(403, new { Message = "You don't have permission to assign users to stores" });
            }

            var userToAssign = await _context.Users.FindAsync(id);
            if (userToAssign == null)
            {
                return NotFound(new { message = "User not found" });
            }

            var store = await _context.Warehouses.FindAsync(request.StoreId);
            if (store == null)
            {
                return NotFound(new { message = "Store not found" });
            }

            // If current user is StoreManager, they can only assign users to their own store
            if (currentUserRoles.Contains("StoreManager") && !currentUserRoles.Contains("SuperAdmin"))
            {
                var currentUser = await _context.Users.FindAsync(currentUserId);
                // Store assignment removed - no multi-tenancy
            }

            // Check if user is a StoreManager and if they're already managing another store
            var userRoles = await _context.UserRoles
                .Where(ur => ur.UserId == id)
                .Include(ur => ur.Role)
                .Select(ur => ur.Role.Name)
                .ToListAsync();

            if (userRoles.Contains("StoreManager"))
            {
                var existingManagerStore = await _context.Warehouses
                    .FirstOrDefaultAsync(w => w.ManagerUserId == id && w.IsActive && w.Id != request.StoreId);
                
                if (existingManagerStore != null)
                {
                    return BadRequest(new { Message = $"User is already managing store '{existingManagerStore.Name}'. A manager can only manage one store at a time." });
                }
            }

            // Store assignment removed - no multi-tenancy
            await _context.SaveChangesAsync();

            return Ok(new { message = "User assigned to store successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error assigning user {UserId} to store {StoreId}", id, request.StoreId);
            return StatusCode(500, new { message = "An error occurred while assigning the user to store" });
        }
    }

    /// <summary>
    /// Remove user from store (StoreManager only)
    /// </summary>
    [HttpPost("{id}/remove-store")]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<ActionResult> RemoveUserFromStore(int id)
    {
        try
        {
            var currentUserId = GetCurrentUserId();
            var currentUserRoles = await GetCurrentUserRoles(currentUserId);
            
            // Check if user has permission to remove users from stores
            if (!currentUserRoles.Contains("SuperAdmin") && !currentUserRoles.Contains("StoreManager"))
            {
                return StatusCode(403, new { Message = "You don't have permission to remove users from stores" });
            }

            var userToRemove = await _context.Users.FindAsync(id);
            if (userToRemove == null)
            {
                return NotFound(new { message = "User not found" });
            }

            // If current user is StoreManager, they can only remove users from their own store
            if (currentUserRoles.Contains("StoreManager") && !currentUserRoles.Contains("SuperAdmin"))
            {
                var currentUser = await _context.Users.FindAsync(currentUserId);
            }

            // Store assignment removed - no multi-tenancy
            await _context.SaveChangesAsync();

            // Audit log
            await _auditService.LogAsync(
                "User",
                id.ToString(),
                "RemoveFromStore",
                before: "User store assignment",
                after: "User removed from store",
                actorUserId: currentUserId
            );

            return Ok(new { message = "User removed from store successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing user {UserId} from store", id);
            return StatusCode(500, new { message = "An error occurred while removing the user from store" });
        }
    }
}

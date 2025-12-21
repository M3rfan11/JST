using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Api.DTOs;
using Api.Services;
using Api.Data;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IJwtService _jwtService;
    private readonly ILogger<AuthController> _logger;
    private readonly ApplicationDbContext _context;

    public AuthController(IJwtService jwtService, ILogger<AuthController> logger, ApplicationDbContext context)
    {
        _jwtService = jwtService;
        _logger = logger;
        _context = context;
    }

    [HttpPost("login")]
    public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest request)
    {
        try
        {
            var result = await _jwtService.LoginAsync(request);
            
            if (result == null)
            {
                return Unauthorized(new { message = "Invalid email or password" });
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during login for email: {Email}", request.Email);
            return StatusCode(500, new { message = "An error occurred during login" });
        }
    }

    [HttpPost("refresh")]
    public async Task<ActionResult<LoginResponse>> RefreshToken([FromBody] RefreshTokenRequest request)
    {
        try
        {
            var result = await _jwtService.RefreshTokenAsync(request);
            
            if (result == null)
            {
                return Unauthorized(new { message = "Invalid refresh token" });
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during token refresh");
            return StatusCode(500, new { message = "An error occurred during token refresh" });
        }
    }

    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<ActionResult<UserResponse>> Register([FromBody] CreateUserRequest request)
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
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            // Assign default "Customer" role to all new users
            var customerRole = await _context.Roles.FirstOrDefaultAsync(r => r.Name == "Customer");
            if (customerRole != null)
            {
                var userRoleAssignment = new Api.Models.UserRole
                {
                    UserId = user.Id,
                    RoleId = customerRole.Id,
                    AssignedAt = DateTime.UtcNow
                };
                _context.UserRoles.Add(userRoleAssignment);
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
                Roles = user.UserRoles.Select(ur => ur.Role.Name).ToList(),
            };

            return CreatedAtAction(nameof(GetCurrentUser), new { id = user.Id }, response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during registration for email: {Email}", request.Email);
            return StatusCode(500, new { message = "An error occurred during registration" });
        }
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult<UserResponse>> GetCurrentUser()
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return Unauthorized(new { message = "User not found" });
            }

            var user = await _context.Users
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .Where(u => u.Id == userId)
                .Select(u => new UserResponse
                {
                    Id = u.Id,
                    FullName = u.FullName,
                    Email = u.Email,
                    IsActive = u.IsActive,
                    CreatedAt = u.CreatedAt,
                    UpdatedAt = u.UpdatedAt,
                    Roles = u.UserRoles.Select(ur => ur.Role.Name).ToList()
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
            _logger.LogError(ex, "Error retrieving current user");
            return StatusCode(500, new { message = "An error occurred while retrieving user information" });
        }
    }

    [HttpGet("debug-roles")]
    [Authorize]
    public async Task<ActionResult> DebugRoles()
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return Unauthorized(new { message = "User not found" });
            }

            var user = await _context.Users
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
            {
                return NotFound(new { message = "User not found" });
            }

            var rolesFromDb = user.UserRoles.Select(ur => ur.Role.Name).ToList();
            var rolesFromToken = User.FindAll(System.Security.Claims.ClaimTypes.Role).Select(c => c.Value).ToList();

            return Ok(new
            {
                userId = user.Id,
                email = user.Email,
                rolesFromDatabase = rolesFromDb,
                rolesFromToken = rolesFromToken,
                hasSuperAdminInDb = rolesFromDb.Contains("SuperAdmin"),
                hasSuperAdminInToken = rolesFromToken.Contains("SuperAdmin")
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in debug roles");
            return StatusCode(500, new { message = "An error occurred" });
        }
    }

    [HttpPost("google")]
    [AllowAnonymous]
    public async Task<ActionResult<LoginResponse>> GoogleSignIn([FromBody] GoogleSignInRequest request)
    {
        try
        {
            if (string.IsNullOrEmpty(request.Credential))
            {
                return BadRequest(new { message = "Google credential is required" });
            }

            // Decode the Google JWT token (simplified - in production, verify the token signature)
            var parts = request.Credential.Split('.');
            if (parts.Length != 3)
            {
                return BadRequest(new { message = "Invalid Google credential format" });
            }

            // Decode the payload (second part)
            var payload = parts[1];
            // Add padding if needed
            switch (payload.Length % 4)
            {
                case 2: payload += "=="; break;
                case 3: payload += "="; break;
            }

            var jsonBytes = Convert.FromBase64String(payload);
            var json = System.Text.Encoding.UTF8.GetString(jsonBytes);
            var googleUser = JsonSerializer.Deserialize<GoogleUserInfo>(json);

            if (googleUser == null || string.IsNullOrEmpty(googleUser.Email))
            {
                return BadRequest(new { message = "Invalid Google credential" });
            }

            // Check if user exists
            var user = await _context.Users
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.Email == googleUser.Email);

            if (user == null)
            {
                // Create new user from Google account
                user = new Api.Models.User
                {
                    FullName = googleUser.Name ?? googleUser.Email.Split('@')[0],
                    Email = googleUser.Email,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(Guid.NewGuid().ToString()), // Random password since using Google
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                // Assign Customer role
                var customerRole = await _context.Roles.FirstOrDefaultAsync(r => r.Name == "Customer");
                if (customerRole != null)
                {
                    var userRoleAssignment = new Api.Models.UserRole
                    {
                        UserId = user.Id,
                        RoleId = customerRole.Id,
                        AssignedAt = DateTime.UtcNow
                    };
                    _context.UserRoles.Add(userRoleAssignment);
                    await _context.SaveChangesAsync();
                }

                // Reload user with roles
                await _context.Entry(user)
                    .Collection(u => u.UserRoles)
                    .LoadAsync();
            }

            if (!user.IsActive)
            {
                return Unauthorized(new { message = "Account is inactive" });
            }

            // Generate JWT token
            var roles = user.UserRoles.Select(ur => ur.Role.Name).ToList();
            var token = _jwtService.GenerateAccessToken(user, roles);

            var response = new LoginResponse
            {
                AccessToken = token,
                RefreshToken = _jwtService.GenerateRefreshToken(),
                ExpiresAt = DateTime.UtcNow.AddHours(24),
                User = new UserDto
                {
                    Id = user.Id,
                    FullName = user.FullName,
                    Email = user.Email,
                    IsActive = user.IsActive,
                    CreatedAt = user.CreatedAt,
                    Roles = roles,
                }
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during Google sign-in");
            return StatusCode(500, new { message = "An error occurred during Google sign-in" });
        }
    }

    [HttpPost("fix-admin-role")]
    [AllowAnonymous] // Allow this without auth for emergency fixes
    public async Task<ActionResult> FixAdminRole()
    {
        try
        {
            // Find admin@example.com user
            var adminUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == "admin@example.com");
            if (adminUser == null)
            {
                return NotFound(new { message = "admin@example.com user not found" });
            }

            // Find SuperAdmin role
            var superAdminRole = await _context.Roles.FirstOrDefaultAsync(r => r.Name == "SuperAdmin");
            if (superAdminRole == null)
            {
                return NotFound(new { message = "SuperAdmin role not found" });
            }

            // Check if user already has SuperAdmin role
            var hasSuperAdmin = await _context.UserRoles
                .AnyAsync(ur => ur.UserId == adminUser.Id && ur.RoleId == superAdminRole.Id);

            if (hasSuperAdmin)
            {
                return Ok(new { message = "User already has SuperAdmin role", userId = adminUser.Id, email = adminUser.Email });
            }

            // Assign SuperAdmin role
            var userRole = new Api.Models.UserRole
            {
                UserId = adminUser.Id,
                RoleId = superAdminRole.Id,
                AssignedAt = DateTime.UtcNow
            };

            _context.UserRoles.Add(userRole);
            await _context.SaveChangesAsync();

            return Ok(new { 
                message = "SuperAdmin role assigned successfully", 
                userId = adminUser.Id, 
                email = adminUser.Email,
                note = "Please log out and log back in to get a new token with the SuperAdmin role"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fixing admin role");
            return StatusCode(500, new { message = "An error occurred", error = ex.Message });
        }
    }

    private int? GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
        return userIdClaim != null && int.TryParse(userIdClaim.Value, out var userId) ? userId : null;
    }
}

// DTOs for Google OAuth
public class GoogleSignInRequest
{
    public string Credential { get; set; } = string.Empty;
}

public class GoogleUserInfo
{
    [System.Text.Json.Serialization.JsonPropertyName("email")]
    public string Email { get; set; } = string.Empty;
    
    [System.Text.Json.Serialization.JsonPropertyName("name")]
    public string? Name { get; set; }
    
    [System.Text.Json.Serialization.JsonPropertyName("picture")]
    public string? Picture { get; set; }
}

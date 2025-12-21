using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Api.Data;
using Api.DTOs;
using Api.Models;

namespace Api.Services;

public class JwtService : IJwtService
{
    private readonly ApplicationDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly ILogger<JwtService> _logger;

    public JwtService(ApplicationDbContext context, IConfiguration configuration, ILogger<JwtService> logger)
    {
        _context = context;
        _configuration = configuration;
        _logger = logger;
    }

    public string GenerateAccessToken(User user, List<string> roles)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]!));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.FullName),
            new(ClaimTypes.Email, user.Email)
        };

        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

        var token = new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"],
            audience: _configuration["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(int.Parse(_configuration["Jwt:AccessTokenExpirationMinutes"]!)),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public string GenerateRefreshToken()
    {
        var randomBytes = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);
        return Convert.ToBase64String(randomBytes);
    }

    public bool ValidateRefreshToken(string refreshToken)
    {
        // In a real application, you would store refresh tokens in the database
        // and validate them against the stored tokens with expiration dates
        return !string.IsNullOrEmpty(refreshToken);
    }

    public async Task<LoginResponse?> LoginAsync(LoginRequest request)
    {
        var user = await _context.Users
            .Include(u => u.UserRoles)
            .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Email == request.Email && u.IsActive);

        if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
        {
            return null;
        }

        var roles = user.UserRoles.Select(ur => ur.Role.Name).ToList();
        
        // Log roles for debugging
        _logger.LogInformation("User {Email} logging in with roles: {Roles}", user.Email, string.Join(", ", roles));
        
        // Auto-assign SuperAdmin to admin@example.com if missing
        if (user.Email == "admin@example.com" && !roles.Contains("SuperAdmin"))
        {
            _logger.LogWarning("admin@example.com doesn't have SuperAdmin role. Attempting to assign...");
            var superAdminRole = await _context.Roles.FirstOrDefaultAsync(r => r.Name == "SuperAdmin");
            if (superAdminRole != null)
            {
                var hasSuperAdmin = await _context.UserRoles
                    .AnyAsync(ur => ur.UserId == user.Id && ur.RoleId == superAdminRole.Id);
                
                if (!hasSuperAdmin)
                {
                    var userRole = new Api.Models.UserRole
                    {
                        UserId = user.Id,
                        RoleId = superAdminRole.Id,
                        AssignedAt = DateTime.UtcNow
                    };
                    _context.UserRoles.Add(userRole);
                    await _context.SaveChangesAsync();
                    roles.Add("SuperAdmin");
                    _logger.LogInformation("SuperAdmin role assigned to admin@example.com");
                }
            }
        }
        
        // Auto-assign Customer role to any user without any roles
        if (roles.Count == 0)
        {
            _logger.LogInformation("User {Email} has no roles. Assigning default Customer role...", user.Email);
            var customerRole = await _context.Roles.FirstOrDefaultAsync(r => r.Name == "Customer");
            if (customerRole != null)
            {
                var hasCustomer = await _context.UserRoles
                    .AnyAsync(ur => ur.UserId == user.Id && ur.RoleId == customerRole.Id);
                
                if (!hasCustomer)
                {
                    var userRole = new Api.Models.UserRole
                    {
                        UserId = user.Id,
                        RoleId = customerRole.Id,
                        AssignedAt = DateTime.UtcNow
                    };
                    _context.UserRoles.Add(userRole);
                    await _context.SaveChangesAsync();
                    roles.Add("Customer");
                    _logger.LogInformation("Customer role assigned to user {Email}", user.Email);
                }
            }
            else
            {
                _logger.LogWarning("Customer role not found in database. Please ensure roles are seeded.");
            }
        }
        
        var accessToken = GenerateAccessToken(user, roles);
        var refreshToken = GenerateRefreshToken();

        return new LoginResponse
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            ExpiresAt = DateTime.UtcNow.AddMinutes(int.Parse(_configuration["Jwt:AccessTokenExpirationMinutes"]!)),
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
    }

    public async Task<LoginResponse?> RefreshTokenAsync(RefreshTokenRequest request)
    {
        // In a real application, you would validate the refresh token against the database
        if (!ValidateRefreshToken(request.RefreshToken))
        {
            return null;
        }

        // For simplicity, we'll extract user info from the current token
        // In a real app, you'd store refresh tokens with user associations
        var tokenHandler = new JwtSecurityTokenHandler();
        var token = tokenHandler.ReadJwtToken(request.RefreshToken);
        var userIdClaim = token.Claims.FirstOrDefault(x => x.Type == ClaimTypes.NameIdentifier);

        if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out var userId))
        {
            return null;
        }

        var user = await _context.Users
            .Include(u => u.UserRoles)
            .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Id == userId && u.IsActive);

        if (user == null)
        {
            return null;
        }

        var roles = user.UserRoles.Select(ur => ur.Role.Name).ToList();
        
        // Auto-assign Customer role to any user without any roles
        if (roles.Count == 0)
        {
            _logger.LogInformation("User {Email} has no roles during token refresh. Assigning default Customer role...", user.Email);
            var customerRole = await _context.Roles.FirstOrDefaultAsync(r => r.Name == "Customer");
            if (customerRole != null)
            {
                var hasCustomer = await _context.UserRoles
                    .AnyAsync(ur => ur.UserId == user.Id && ur.RoleId == customerRole.Id);
                
                if (!hasCustomer)
                {
                    var userRole = new Api.Models.UserRole
                    {
                        UserId = user.Id,
                        RoleId = customerRole.Id,
                        AssignedAt = DateTime.UtcNow
                    };
                    _context.UserRoles.Add(userRole);
                    await _context.SaveChangesAsync();
                    roles.Add("Customer");
                    _logger.LogInformation("Customer role assigned to user {Email} during token refresh", user.Email);
                }
            }
        }
        
        var accessToken = GenerateAccessToken(user, roles);
        var newRefreshToken = GenerateRefreshToken();

        return new LoginResponse
        {
            AccessToken = accessToken,
            RefreshToken = newRefreshToken,
            ExpiresAt = DateTime.UtcNow.AddMinutes(int.Parse(_configuration["Jwt:AccessTokenExpirationMinutes"]!)),
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
    }
}

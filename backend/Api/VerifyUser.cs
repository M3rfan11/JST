using Microsoft.EntityFrameworkCore;
using Api.Data;
using Api.Models;
using BCrypt.Net;

namespace Api;

// Quick verification script - can be called from a controller endpoint for testing
public static class VerifyUser
{
    public static async Task<string> VerifyAdminUserAsync(ApplicationDbContext context)
    {
        var result = new System.Text.StringBuilder();
        result.AppendLine("=== User Verification ===");
        
        var user = await context.Users.FirstOrDefaultAsync(u => u.Email == "admin@jst.com");
        
        if (user == null)
        {
            result.AppendLine("❌ User NOT found!");
            result.AppendLine("Creating user now...");
            
            // Create user directly
            var passwordHash = BCrypt.Net.BCrypt.HashPassword("Admin123!");
            var superAdmin = new User
            {
                FullName = "Super Administrator",
                Email = "admin@jst.com",
                PasswordHash = passwordHash,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            
            context.Users.Add(superAdmin);
            await context.SaveChangesAsync();
            
            // Get or create SuperAdmin role
            var role = await context.Roles.FirstOrDefaultAsync(r => r.Name == "SuperAdmin");
            if (role == null)
            {
                role = new Role
                {
                    Name = "SuperAdmin",
                    Description = "Super Administrator with full access"
                };
                context.Roles.Add(role);
                await context.SaveChangesAsync();
            }
            
            // Assign role
            var userRole = new UserRole
            {
                UserId = superAdmin.Id,
                RoleId = role.Id,
                AssignedAt = DateTime.UtcNow
            };
            context.UserRoles.Add(userRole);
            await context.SaveChangesAsync();
            
            result.AppendLine("✅ User created successfully!");
        }
        else
        {
            result.AppendLine($"✅ User found: {user.FullName}");
            result.AppendLine($"   Email: {user.Email}");
            result.AppendLine($"   ID: {user.Id}");
            result.AppendLine($"   Active: {user.IsActive}");
            result.AppendLine($"   Password Hash: {user.PasswordHash?.Substring(0, 20)}...");
            
            // Test password verification
            var testPassword = "Admin123!";
            var isValid = BCrypt.Net.BCrypt.Verify(testPassword, user.PasswordHash);
            result.AppendLine($"   Password 'Admin123!' verification: {(isValid ? "✅ VALID" : "❌ INVALID")}");
            
            // Check roles
            var roles = await context.UserRoles
                .Include(ur => ur.Role)
                .Where(ur => ur.UserId == user.Id)
                .Select(ur => ur.Role.Name)
                .ToListAsync();
            
            result.AppendLine($"   Roles: {string.Join(", ", roles)}");
        }
        
        return result.ToString();
    }
}







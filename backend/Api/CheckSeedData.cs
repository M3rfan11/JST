using Microsoft.EntityFrameworkCore;
using Api.Data;
using Api.Models;

namespace Api;

// Temporary class to check and fix seed data
public static class CheckSeedData
{
    public static async Task CheckAndFixAsync(ApplicationDbContext context)
    {
        Console.WriteLine("=== Checking Seed Data ===");
        
        // Check if user exists
        var user = await context.Users.FirstOrDefaultAsync(u => u.Email == "admin@jst.com");
        if (user == null)
        {
            Console.WriteLine("❌ SuperAdmin user NOT found! Creating now...");
            await SeedData.SeedAsync(context);
        }
        else
        {
            Console.WriteLine($"✅ SuperAdmin user found: {user.FullName} ({user.Email})");
            Console.WriteLine($"   User ID: {user.Id}");
            Console.WriteLine($"   Is Active: {user.IsActive}");
            Console.WriteLine($"   Password Hash Length: {user.PasswordHash?.Length ?? 0}");
            
            // Check roles
            var userRoles = await context.UserRoles
                .Include(ur => ur.Role)
                .Where(ur => ur.UserId == user.Id)
                .ToListAsync();
            
            Console.WriteLine($"   Roles: {string.Join(", ", userRoles.Select(ur => ur.Role.Name))}");
        }
        
        // Check roles
        var roles = await context.Roles.ToListAsync();
        Console.WriteLine($"\nRoles in database: {roles.Count}");
        foreach (var role in roles)
        {
            Console.WriteLine($"  - {role.Name} (ID: {role.Id})");
        }
        
        // Check permissions
        var permissions = await context.Permissions.CountAsync();
        Console.WriteLine($"\nPermissions in database: {permissions}");
        
        // Check categories
        var categories = await context.Categories.CountAsync();
        Console.WriteLine($"Categories in database: {categories}");
        
        Console.WriteLine("\n=== Seed Data Check Complete ===");
    }
}








using Microsoft.EntityFrameworkCore;
using Api.Data;
using Api.Models;
using BCrypt.Net;

namespace Api
{

public static class SeedData
{
    public static async Task SeedAsync(ApplicationDbContext context)
    {
        // Ensure database is created
        await context.Database.EnsureCreatedAsync();

        // Seed Roles
        await SeedRolesAsync(context);

        // Seed SuperAdmin User
        await SeedSuperAdminAsync(context);

        // Seed Sample Categories
        await SeedCategoriesAsync(context);

        // Seed Online Store Warehouse
        await SeedOnlineStoreWarehouseAsync(context);
    }

    private static async Task SeedRolesAsync(ApplicationDbContext context)
    {
        var rolesToCreate = new List<Role>();
        
        // Only seed SuperAdmin and Customer roles
        var roleDefinitions = new[]
        {
            new { Name = "SuperAdmin", Description = "Super Administrator with full system access" },
            new { Name = "Customer", Description = "Customer role for online orders" }
        };

        foreach (var roleDef in roleDefinitions)
        {
            // Use AsNoTracking to avoid entity tracking conflicts
            var existingRole = await context.Roles
                .AsNoTracking()
                .FirstOrDefaultAsync(r => r.Name == roleDef.Name);
            
            if (existingRole == null)
            {
                // Don't set ID - let database auto-generate or use existing from migrations
                rolesToCreate.Add(new Role
                {
                    Name = roleDef.Name,
                    Description = roleDef.Description
                });
            }
        }

        if (rolesToCreate.Any())
        {
            await context.Roles.AddRangeAsync(rolesToCreate);
            await context.SaveChangesAsync();
        }
    }

    // Permission system removed - using simple SuperAdmin role check instead

    private static async Task SeedSuperAdminAsync(ApplicationDbContext context)
    {
        if (await context.Users.AnyAsync(u => u.Email == "admin@jst.com"))
            return;

        // Hash password: Admin123!
        var passwordHash = BCrypt.Net.BCrypt.HashPassword("Admin123!");

        // Check if user with ID 1 exists
        var existingUserById = await context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == 1);

        var superAdmin = new User
        {
            FullName = "Super Administrator",
            Email = "admin@jst.com",
            PasswordHash = passwordHash,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        
        // Only set ID if it doesn't exist, otherwise let EF auto-generate
        if (existingUserById == null)
        {
            superAdmin.Id = 1;
        }

        await context.Users.AddAsync(superAdmin);
        await context.SaveChangesAsync();

        // Assign SuperAdmin role
        var superAdminRole = await context.Roles
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Name == "SuperAdmin");
        
        if (superAdminRole != null)
        {
            // Check if user role already exists
            var existingUserRole = await context.UserRoles
                .AsNoTracking()
                .FirstOrDefaultAsync(ur => ur.UserId == superAdmin.Id && ur.RoleId == superAdminRole.Id);
            
            if (existingUserRole == null)
            {
                var userRole = new UserRole
                {
                    UserId = superAdmin.Id,
                    RoleId = superAdminRole.Id,
                    AssignedAt = DateTime.UtcNow
                };

                await context.UserRoles.AddAsync(userRole);
                await context.SaveChangesAsync();
            }
        }
    }

    private static async Task SeedCategoriesAsync(ApplicationDbContext context)
    {
        var categoriesToCreate = new List<Category>();
        
        var categoryDefinitions = new[]
        {
            new { Name = "Suits", Description = "Premium tailored suits for formal occasions" },
            new { Name = "Blazers", Description = "Elegant blazers for business and casual wear" },
            new { Name = "Shirts", Description = "Classic and contemporary dress shirts" },
            new { Name = "Accessories", Description = "Ties, pocket squares, and other accessories" }
        };

        foreach (var catDef in categoryDefinitions)
        {
            // Use AsNoTracking to avoid entity tracking conflicts
            var existingCategory = await context.Categories
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Name == catDef.Name);
            
            if (existingCategory == null)
            {
                // Don't set ID - let database auto-generate
                categoriesToCreate.Add(new Category
                {
                    Name = catDef.Name,
                    Description = catDef.Description,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                });
            }
        }

        if (categoriesToCreate.Any())
        {
            await context.Categories.AddRangeAsync(categoriesToCreate);
            await context.SaveChangesAsync();
        }
    }

    private static async Task SeedOnlineStoreWarehouseAsync(ApplicationDbContext context)
    {
        // Check if Online Store warehouse already exists
        var existingWarehouse = await context.Warehouses
            .AsNoTracking()
            .FirstOrDefaultAsync(w => w.Name == "Online Store");
        
        if (existingWarehouse == null)
        {
            var onlineStore = new Warehouse
            {
                Name = "Online Store",
                Address = "Online",
                City = "Virtual",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            await context.Warehouses.AddAsync(onlineStore);
            await context.SaveChangesAsync();
        }
    }
}
}


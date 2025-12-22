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

            // Seed Products with Variants
            await SeedProductsAsync(context);
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
            // Clear all existing categories first
            var existingCategories = await context.Categories.ToListAsync();
            if (existingCategories.Any())
            {
                context.Categories.RemoveRange(existingCategories);
                await context.SaveChangesAsync();
            }

            // Create only the two required categories
            var categoriesToCreate = new List<Category>();

            var categoryDefinitions = new[]
            {
                new {
                    Name = "JST for Him",
                    Description = "Premium fashion collection for men",
                    ImageUrl = "https://images.unsplash.com/photo-1441986300917-64674dd600d8?w=800&h=600&fit=crop"
                },
                new {
                    Name = "JST for Her",
                    Description = "Elegant fashion collection for women",
                    ImageUrl = "https://images.unsplash.com/photo-1445205170230-053b83016050?w=800&h=600&fit=crop"
                },
                new {
                    Name = "New Arrival",
                    Description = "Latest additions to our collection",
                    ImageUrl = "https://images.unsplash.com/photo-1441986300917-64674dd600d8?w=800&h=600&fit=crop"
                }
            };

            foreach (var catDef in categoryDefinitions)
            {
                categoriesToCreate.Add(new Category
                {
                    Name = catDef.Name,
                    Description = catDef.Description,
                    ImageUrls = System.Text.Json.JsonSerializer.Serialize(new[] { catDef.ImageUrl }),
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                });
            }

            await context.Categories.AddRangeAsync(categoriesToCreate);
            await context.SaveChangesAsync();
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

        private static async Task SeedProductsAsync(ApplicationDbContext context)
        {
            // Clear all existing products, variants, inventory, and product categories first
            var existingProductCategories = await context.ProductCategories.ToListAsync();
            if (existingProductCategories.Any())
            {
                context.ProductCategories.RemoveRange(existingProductCategories);
                await context.SaveChangesAsync();
            }

            var existingVariantInventories = await context.VariantInventories.ToListAsync();
            if (existingVariantInventories.Any())
            {
                context.VariantInventories.RemoveRange(existingVariantInventories);
                await context.SaveChangesAsync();
            }

            var existingVariants = await context.ProductVariants.ToListAsync();
            if (existingVariants.Any())
            {
                context.ProductVariants.RemoveRange(existingVariants);
                await context.SaveChangesAsync();
            }

            var existingInventories = await context.ProductInventories.ToListAsync();
            if (existingInventories.Any())
            {
                context.ProductInventories.RemoveRange(existingInventories);
                await context.SaveChangesAsync();
            }

            var existingProducts = await context.Products.ToListAsync();
            if (existingProducts.Any())
            {
                context.Products.RemoveRange(existingProducts);
                await context.SaveChangesAsync();
            }

            // Get categories
            var jstForHimCategory = await context.Categories.FirstOrDefaultAsync(c => c.Name == "JST for Him");
            var jstForHerCategory = await context.Categories.FirstOrDefaultAsync(c => c.Name == "JST for Her");
            var newArrivalCategory = await context.Categories.FirstOrDefaultAsync(c => c.Name == "New Arrival");
            var onlineStoreWarehouse = await context.Warehouses.FirstOrDefaultAsync(w => w.Name == "Online Store");

            if (jstForHimCategory == null || jstForHerCategory == null || newArrivalCategory == null || onlineStoreWarehouse == null)
            {
                throw new InvalidOperationException("Required categories or warehouse not found. Please seed categories and warehouse first.");
            }

            // Product definitions: 3 for Him, 4 for Her
            var productDefinitions = new[]
            {
                // JST for Him - 3 products
                new {
                    Name = "Classic Men's Suit",
                    Description = "Elegant tailored suit perfect for formal occasions and business meetings. Made from premium fabric with impeccable craftsmanship.",
                    Price = 2999.99m,
                    CompareAtPrice = 3499.99m,
                    Category = jstForHimCategory,
                    ImageUrl = "https://images.unsplash.com/photo-1594938291221-94f18b6d4528?w=800&h=1000&fit=crop",
                    SKU = "JST-HIM-001"
                },
                new {
                    Name = "Premium Men's Blazer",
                    Description = "Sophisticated blazer that combines style and comfort. Perfect for both business and casual settings.",
                    Price = 1899.99m,
                    CompareAtPrice = 2299.99m,
                    Category = jstForHimCategory,
                    ImageUrl = "https://images.unsplash.com/photo-1507003211169-0a1dd7228f2d?w=800&h=1000&fit=crop",
                    SKU = "JST-HIM-002"
                },
                new {
                    Name = "Designer Men's Shirt",
                    Description = "Classic dress shirt with modern fit. Crisp, comfortable, and versatile for any occasion.",
                    Price = 599.99m,
                    CompareAtPrice = 799.99m,
                    Category = jstForHimCategory,
                    ImageUrl = "https://images.unsplash.com/photo-1603252109303-2751441dd157?w=800&h=1000&fit=crop",
                    SKU = "JST-HIM-003"
                },
                // JST for Her - 4 products
                new {
                    Name = "Elegant Women's Dress",
                    Description = "Stunning evening dress that exudes elegance and sophistication. Perfect for special occasions.",
                    Price = 2499.99m,
                    CompareAtPrice = 2999.99m,
                    Category = jstForHerCategory,
                    ImageUrl = "https://images.unsplash.com/photo-1595777457583-95e059d581b8?w=800&h=1000&fit=crop",
                    SKU = "JST-HER-001"
                },
                new {
                    Name = "Chic Women's Blazer",
                    Description = "Modern blazer that adds sophistication to any outfit. Versatile and stylish for the contemporary woman.",
                    Price = 1699.99m,
                    CompareAtPrice = 1999.99m,
                    Category = jstForHerCategory,
                    ImageUrl = "https://images.unsplash.com/photo-1591047139829-d91aecb6caea?w=800&h=1000&fit=crop",
                    SKU = "JST-HER-002"
                },
                new {
                    Name = "Designer Women's Top",
                    Description = "Elegant top that combines comfort with style. Perfect for both casual and formal occasions.",
                    Price = 799.99m,
                    CompareAtPrice = 999.99m,
                    Category = jstForHerCategory,
                    ImageUrl = "https://images.unsplash.com/photo-1434389677669-e08b4cac3105?w=800&h=1000&fit=crop",
                    SKU = "JST-HER-003"
                },
                new {
                    Name = "Luxury Women's Skirt",
                    Description = "Sophisticated skirt that offers timeless elegance. Made from premium materials for lasting quality.",
                    Price = 1299.99m,
                    CompareAtPrice = 1599.99m,
                    Category = jstForHerCategory,
                    ImageUrl = "https://images.unsplash.com/photo-1594633312681-425c7b97ccd1?w=800&h=1000&fit=crop",
                    SKU = "JST-HER-004"
                }
            };

            var sizes = new[] { "S", "M", "L", "XL" };
            var random = new Random();
            var productsForNewArrival = new List<int>(); // Track product IDs that should also be in "New Arrival" category

            foreach (var prodDef in productDefinitions)
            {
                // Create product
                // Set CategoryId for backward compatibility, but we'll use ProductCategory for the actual relationship
                var product = new Product
                {
                    Name = prodDef.Name,
                    Description = prodDef.Description,
                    Price = prodDef.Price,
                    CompareAtPrice = prodDef.CompareAtPrice,
                    CategoryId = prodDef.Category.Id, // Legacy field for backward compatibility
                    SKU = prodDef.SKU,
                    ImageUrl = prodDef.ImageUrl,
                    MediaUrls = System.Text.Json.JsonSerializer.Serialize(new[] { prodDef.ImageUrl }),
                    VariantAttributes = System.Text.Json.JsonSerializer.Serialize(new[] { "Size" }),
                    Status = ProductStatus.Active,
                    IsActive = true,
                    InventoryTracked = true,
                    IsPhysicalProduct = true,
                    PublishingChannels = "Online Store",
                    CreatedAt = DateTime.UtcNow
                };

                await context.Products.AddAsync(product);
                await context.SaveChangesAsync();

                // Create ProductCategory relationship (many-to-many)
                var productCategory = new ProductCategory
                {
                    ProductId = product.Id,
                    CategoryId = prodDef.Category.Id
                };
                await context.ProductCategories.AddAsync(productCategory);
                await context.SaveChangesAsync();

                // Track first 3 products to also add to "New Arrival" category
                if (productsForNewArrival.Count < 3)
                {
                    productsForNewArrival.Add(product.Id);
                }

                // Create one variant with size attribute (using first size as default)
                // Each product has one single variant which represents the size attribute
                var variantAttributes = System.Text.Json.JsonSerializer.Serialize(new Dictionary<string, string> { { "Size", sizes[0] } });

                var variant = new ProductVariant
                {
                    ProductId = product.Id,
                    Color = "Default", // Required legacy field
                    Attributes = variantAttributes,
                    ImageUrl = prodDef.ImageUrl,
                    MediaUrls = System.Text.Json.JsonSerializer.Serialize(new[] { prodDef.ImageUrl }),
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };

                await context.ProductVariants.AddAsync(variant);
                await context.SaveChangesAsync();

                // Create variant inventory for Online Store warehouse
                // The backend calculates stock from VariantInventories, so this is essential
                var variantStock = random.Next(10, 100); // Random stock between 10-100
                var variantInventory = new VariantInventory
                {
                    ProductVariantId = variant.Id,
                    WarehouseId = onlineStoreWarehouse.Id,
                    Quantity = variantStock,
                    POSQuantity = 0,
                    Unit = "piece",
                    MinimumStockLevel = 5,
                    MaximumStockLevel = 200,
                    CreatedAt = DateTime.UtcNow
                };

                await context.VariantInventories.AddAsync(variantInventory);
                await context.SaveChangesAsync();

                // Create product inventory for Online Store warehouse (for backward compatibility)
                var inventory = new ProductInventory
                {
                    ProductId = product.Id,
                    WarehouseId = onlineStoreWarehouse.Id,
                    Quantity = variantStock, // Match variant stock for consistency
                    POSQuantity = 0,
                    Unit = "piece",
                    CreatedAt = DateTime.UtcNow
                };

                await context.ProductInventories.AddAsync(inventory);
                await context.SaveChangesAsync();
            }

            // Add the first 3 products to "New Arrival" category as well
            foreach (var productId in productsForNewArrival)
            {
                // Check if the relationship already exists
                var existingProductCategory = await context.ProductCategories
                    .FirstOrDefaultAsync(pc => pc.ProductId == productId && pc.CategoryId == newArrivalCategory.Id);

                if (existingProductCategory == null)
                {
                    var newArrivalProductCategory = new ProductCategory
                    {
                        ProductId = productId,
                        CategoryId = newArrivalCategory.Id
                    };
                    await context.ProductCategories.AddAsync(newArrivalProductCategory);
                    await context.SaveChangesAsync();
                }
            }
        }
    }
}


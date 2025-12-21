using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Api.Data;
using Api.DTOs;
using Api.Models;
using Api.Services;

namespace Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IAuditService _auditService;
    private readonly ILogger<ProductController> _logger;

    public ProductController(ApplicationDbContext context, IAuditService auditService, ILogger<ProductController> logger)
    {
        _context = context;
        _auditService = auditService;
        _logger = logger;
    }

    private int GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier");
        return userIdClaim != null ? int.Parse(userIdClaim.Value) : 0;
    }

    /// <summary>
    /// Get all products - Public endpoint, no authentication required
    /// </summary>
    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<IEnumerable<ProductListResponse>>> GetProducts(
        [FromQuery] bool? includeDrafts = null,
        [FromQuery] string? categoryName = null)
    {
        // For public access, always filter to only active products
        // Only show drafts if explicitly requested (for admin use with authentication)
        var query = _context.Products.AsQueryable();
        
        // Public endpoint: always show only active products unless explicitly requesting drafts
        // (drafts would require authentication anyway)
        if (includeDrafts != true)
        {
            query = query.Where(p => p.IsActive);
        }
        
        // Filter by category name if provided (check both primary category and ProductCategories)
        if (!string.IsNullOrWhiteSpace(categoryName))
        {
            query = query.Where(p => 
                p.Category.Name == categoryName || 
                p.ProductCategories.Any(pc => pc.Category.Name == categoryName)
            );
        }
        
        var products = await query
            .Include(p => p.Category)
            .Include(p => p.ProductCategories)
                .ThenInclude(pc => pc.Category)
            .Include(p => p.ProductInventories)
                .ThenInclude(pi => pi.Warehouse)
            .Include(p => p.Variants)
            .ToListAsync();

        // Get the single warehouse (assuming only one store)
        var warehouse = await _context.Warehouses
            .Where(w => w.IsActive)
            .FirstOrDefaultAsync();

        // Load all variant inventories for all products in one query
        var allProductVariantIds = products
            .SelectMany(p => p.Variants.Where(v => v.IsActive).Select(v => v.Id))
            .ToList();
        
        var variantInventories = warehouse != null
            ? await _context.VariantInventories
                .Where(vi => allProductVariantIds.Contains(vi.ProductVariantId) && vi.WarehouseId == warehouse.Id)
                .ToListAsync()
            : new List<Models.VariantInventory>();

        // Group variant inventories by variant ID for quick lookup
        var variantInventoriesByVariantId = variantInventories
            .GroupBy(vi => vi.ProductVariantId)
            .ToDictionary(g => g.Key, g => g.Sum(vi => vi.Quantity));

        var response = products.Select(p => {
            // Check if product has active variants
            var hasActiveVariants = p.Variants.Any(v => v.IsActive);
            
            // Calculate TotalQuantity:
            // - If product has variants, sum from VariantInventories
            // - Otherwise, sum from ProductInventories
            decimal totalQuantity = 0;
            if (hasActiveVariants)
            {
                var productVariantIds = p.Variants.Where(v => v.IsActive).Select(v => v.Id).ToList();
                totalQuantity = productVariantIds
                    .Where(vid => variantInventoriesByVariantId.ContainsKey(vid))
                    .Sum(vid => variantInventoriesByVariantId[vid]);
            }
            else
            {
                totalQuantity = p.ProductInventories
                    .Where(pi => warehouse == null || pi.WarehouseId == warehouse.Id)
                    .Sum(pi => pi.Quantity);
            }

            return new ProductListResponse
            {
                Id = p.Id,
                Name = p.Name,
                Description = p.Description,
                Price = p.Price,
                CompareAtPrice = p.CompareAtPrice,
                Unit = p.Unit,
                SKU = p.SKU,
                Brand = p.Brand,
                ImageUrl = p.ImageUrl,
                CategoryName = p.Category.Name,
                IsActive = p.IsActive,
                TotalQuantity = totalQuantity,
                Colors = p.Variants.Where(v => v.IsActive).Select(v => v.Color).ToList(), // Legacy
                Variants = p.Variants.Where(v => v.IsActive).Select(v => {
                    var variantQuantity = variantInventoriesByVariantId.ContainsKey(v.Id) 
                        ? variantInventoriesByVariantId[v.Id] 
                        : 0;
                    var isVariantOutOfStock = variantQuantity == 0;
                    var isVariantAvailable = !isVariantOutOfStock || p.SellWhenOutOfStock || p.AlwaysAvailable;

                    return new ProductVariantResponse
                    {
                        Id = v.Id,
                        ProductId = v.ProductId,
                        Color = v.Color ?? string.Empty,
                        ColorHex = v.ColorHex,
                        Attributes = v.Attributes,
                        ImageUrl = v.ImageUrl,
                        MediaUrls = v.MediaUrls,
                        PriceOverride = v.PriceOverride,
                        SKU = v.SKU,
                        IsActive = v.IsActive,
                        Quantity = variantQuantity,
                        IsOutOfStock = isVariantOutOfStock,
                        IsAvailable = isVariantAvailable,
                        CreatedAt = v.CreatedAt,
                        UpdatedAt = v.UpdatedAt
                    };
                }).ToList(),
                VariantAttributes = p.VariantAttributes
            };
        })
        .OrderByDescending(p => p.IsActive) // Show active products first
        .ThenBy(p => p.Name)
        .ToList();

        return Ok(response);
    }

    /// <summary>
    /// Get all products including inactive ones (Requires Products.Read permission)
    /// </summary>
    [HttpGet("all")]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<ActionResult<IEnumerable<ProductResponse>>> GetAllProducts()
    {
        var products = await _context.Products
            .Include(p => p.Category)
            .Include(p => p.ProductCategories)
                .ThenInclude(pc => pc.Category)
            .Include(p => p.ProductInventories)
                .ThenInclude(pi => pi.Warehouse)
            .Include(p => p.Variants)
            .OrderBy(p => p.Name)
            .ToListAsync();

        // Load variant inventories for all products in one query
        var allProductVariantIds = products
            .SelectMany(p => p.Variants.Where(v => v.IsActive).Select(v => v.Id))
            .ToList();
        
        var variantInventories = await _context.VariantInventories
            .Include(vi => vi.Warehouse)
            .Where(vi => allProductVariantIds.Contains(vi.ProductVariantId))
            .ToListAsync();

        // Group variant inventories by product variant ID for quick lookup
        var variantInventoriesByVariantId = variantInventories
            .GroupBy(vi => vi.ProductVariantId)
            .ToDictionary(g => g.Key, g => g.ToList());

        var response = products.Select(p => {
            var allCategories = p.ProductCategories.Select(pc => pc.Category).ToList();
            var allCategoryIds = allCategories.Select(c => c.Id).ToList();
            var allCategoryNames = allCategories.Select(c => c.Name).ToList();
            
            // Check if product has active variants
            var hasActiveVariants = p.Variants.Any(v => v.IsActive);
            
            // Calculate TotalQuantity:
            // - If product has variants, sum from VariantInventories
            // - Otherwise, sum from ProductInventories
            decimal totalQuantity = 0;
            if (hasActiveVariants)
            {
                // Sum all variant inventories for this product's variants
                var productVariantIds = p.Variants.Where(v => v.IsActive).Select(v => v.Id).ToList();
                totalQuantity = variantInventories
                    .Where(vi => productVariantIds.Contains(vi.ProductVariantId))
                    .Sum(vi => vi.Quantity);
            }
            else
            {
                // Use product-level inventory
                totalQuantity = p.ProductInventories.Sum(pi => pi.Quantity);
            }
            
            return new ProductResponse
            {
                Id = p.Id,
                Name = p.Name,
                Description = p.Description,
                Price = p.Price,
                CompareAtPrice = p.CompareAtPrice,
                Cost = p.Cost,
                Taxable = p.Taxable,
                Unit = p.Unit,
                SKU = p.SKU,
                Barcode = p.Barcode,
                InventoryTracked = p.InventoryTracked,
                SellWhenOutOfStock = p.SellWhenOutOfStock,
                AlwaysAvailable = p.AlwaysAvailable,
                IsPhysicalProduct = p.IsPhysicalProduct,
                Weight = p.Weight,
                Dimensions = p.Dimensions,
                PackageLength = p.PackageLength,
                PackageWidth = p.PackageWidth,
                PackageHeight = p.PackageHeight,
                CountryOfOrigin = p.CountryOfOrigin,
                Brand = p.Brand,
                Vendor = p.Vendor,
                ProductType = p.ProductType,
                Tags = p.Tags,
                Status = p.Status,
                PublishingChannels = p.PublishingChannels,
                ImageUrl = p.ImageUrl,
                MediaUrls = p.MediaUrls,
                VariantAttributes = p.VariantAttributes,
                WashingInstructions = p.WashingInstructions,
                CategoryId = p.CategoryId,
                CategoryName = p.Category.Name,
                CategoryIds = allCategoryIds,
                CategoryNames = allCategoryNames,
                IsActive = p.IsActive,
                CreatedAt = p.CreatedAt,
                UpdatedAt = p.UpdatedAt,
                TotalQuantity = totalQuantity,
                Inventories = p.ProductInventories.Select(pi => new ProductInventoryResponse
                {
                    Id = pi.Id,
                    WarehouseId = pi.WarehouseId,
                    WarehouseName = pi.Warehouse.Name,
                    Quantity = pi.Quantity,
                    Unit = pi.Unit,
                    MinimumStockLevel = pi.MinimumStockLevel,
                    MaximumStockLevel = pi.MaximumStockLevel
                }).ToList(),
                Variants = p.Variants.Select(v => new ProductVariantResponse
                {
                    Id = v.Id,
                    ProductId = v.ProductId,
                    Color = v.Color,
                    ColorHex = v.ColorHex,
                    Attributes = v.Attributes,
                    ImageUrl = v.ImageUrl,
                    MediaUrls = v.MediaUrls,
                    PriceOverride = v.PriceOverride,
                    SKU = v.SKU,
                    IsActive = v.IsActive,
                    CreatedAt = v.CreatedAt,
                    UpdatedAt = v.UpdatedAt
                }).ToList()
            };
        }).ToList();

        return Ok(response);
    }

    /// <summary>
    /// Get a specific product by ID - Public endpoint, no authentication required
    /// </summary>
    [HttpGet("{id}")]
    [AllowAnonymous]
    public async Task<ActionResult<ProductResponse>> GetProduct(int id)
    {
        var product = await _context.Products
            .Include(p => p.Category)
            .Include(p => p.ProductCategories)
                .ThenInclude(pc => pc.Category)
            .Include(p => p.ProductInventories)
                .ThenInclude(pi => pi.Warehouse)
            .Include(p => p.Variants)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (product == null)
        {
            return NotFound($"Product with ID {id} not found.");
        }

        var allCategories = product.ProductCategories.Select(pc => pc.Category).ToList();
        var allCategoryIds = allCategories.Select(c => c.Id).ToList();
        var allCategoryNames = allCategories.Select(c => c.Name).ToList();

        // Check if product has active variants
        var hasActiveVariants = product.Variants.Any(v => v.IsActive);
        
        // Get the single warehouse
        var warehouse = await _context.Warehouses
            .Where(w => w.IsActive)
            .FirstOrDefaultAsync();
        
        // Get variant inventories for stock checking
        var productVariantIds = product.Variants.Where(v => v.IsActive).Select(v => v.Id).ToList();
        var variantInventories = await _context.VariantInventories
            .Where(vi => productVariantIds.Contains(vi.ProductVariantId))
            .ToListAsync();
        
        // Create a map of variant ID to quantity (for the single warehouse)
        var variantStockMap = new Dictionary<int, decimal>();
        if (warehouse != null)
        {
            foreach (var vi in variantInventories.Where(vi => vi.WarehouseId == warehouse.Id))
            {
                variantStockMap[vi.ProductVariantId] = vi.Quantity;
            }
        }
        
        // Calculate TotalQuantity:
        // - If product has variants, sum from VariantInventories
        // - Otherwise, sum from ProductInventories
        decimal totalQuantity = 0;
        if (hasActiveVariants)
        {
            totalQuantity = variantStockMap.Values.Sum();
        }
        else
        {
            // Use product-level inventory
            totalQuantity = product.ProductInventories.Sum(pi => pi.Quantity);
        }

        var response = new ProductResponse
        {
            Id = product.Id,
            Name = product.Name,
            Description = product.Description,
            Price = product.Price,
            CompareAtPrice = product.CompareAtPrice,
            Cost = product.Cost,
            Taxable = product.Taxable,
            Unit = product.Unit,
            SKU = product.SKU,
            Barcode = product.Barcode,
            InventoryTracked = product.InventoryTracked,
            SellWhenOutOfStock = product.SellWhenOutOfStock,
            AlwaysAvailable = product.AlwaysAvailable,
            IsPhysicalProduct = product.IsPhysicalProduct,
            Weight = product.Weight,
            Dimensions = product.Dimensions,
            PackageLength = product.PackageLength,
            PackageWidth = product.PackageWidth,
            PackageHeight = product.PackageHeight,
            CountryOfOrigin = product.CountryOfOrigin,
            Brand = product.Brand,
            Vendor = product.Vendor,
            ProductType = product.ProductType,
            Tags = product.Tags,
            Status = product.Status,
            PublishingChannels = product.PublishingChannels,
            ImageUrl = product.ImageUrl,
            MediaUrls = product.MediaUrls,
            VariantAttributes = product.VariantAttributes,
            WashingInstructions = product.WashingInstructions,
            CategoryId = product.CategoryId,
            CategoryName = product.Category.Name,
            CategoryIds = allCategoryIds,
            CategoryNames = allCategoryNames,
            IsActive = product.IsActive,
            CreatedAt = product.CreatedAt,
            UpdatedAt = product.UpdatedAt,
            TotalQuantity = totalQuantity,
            Inventories = product.ProductInventories.Select(pi => new ProductInventoryResponse
            {
                Id = pi.Id,
                WarehouseId = pi.WarehouseId,
                WarehouseName = pi.Warehouse.Name,
                Quantity = pi.Quantity,
                Unit = pi.Unit,
                MinimumStockLevel = pi.MinimumStockLevel,
                MaximumStockLevel = pi.MaximumStockLevel
            }).ToList(),
            Variants = product.Variants.Select(v => {
                // Get stock quantity for this variant
                decimal variantQuantity = 0;
                if (warehouse != null && variantStockMap.ContainsKey(v.Id))
                {
                    variantQuantity = variantStockMap[v.Id];
                }
                
                bool isOutOfStock = variantQuantity == 0;
                bool isAvailable = !isOutOfStock || product.SellWhenOutOfStock || product.AlwaysAvailable;
                
                return new ProductVariantResponse
                {
                    Id = v.Id,
                    ProductId = v.ProductId,
                    Color = v.Color ?? string.Empty,
                    ColorHex = v.ColorHex,
                    Attributes = v.Attributes,
                    ImageUrl = v.ImageUrl,
                    MediaUrls = v.MediaUrls,
                    PriceOverride = v.PriceOverride,
                    SKU = v.SKU,
                    IsActive = v.IsActive,
                    Quantity = variantQuantity,
                    IsOutOfStock = isOutOfStock,
                    IsAvailable = isAvailable,
                    CreatedAt = v.CreatedAt,
                    UpdatedAt = v.UpdatedAt
                };
            }).ToList()
        };

        return Ok(response);
    }

    /// <summary>
    /// Create a new product (Requires Products.Create permission)
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<ActionResult<ProductResponse>> CreateProduct([FromBody] CreateProductRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return BadRequest("Product name is required.");
        }

        // Check if SKU already exists
        if (!string.IsNullOrWhiteSpace(request.SKU))
        {
            var existingProduct = await _context.Products
                .FirstOrDefaultAsync(p => p.SKU == request.SKU);

            if (existingProduct != null)
            {
                return BadRequest($"A product with SKU '{request.SKU}' already exists.");
            }
        }

        // Check if Barcode already exists
        if (!string.IsNullOrWhiteSpace(request.Barcode))
        {
            var existingProduct = await _context.Products
                .FirstOrDefaultAsync(p => p.Barcode == request.Barcode);

            if (existingProduct != null)
            {
                return BadRequest($"A product with Barcode '{request.Barcode}' already exists.");
            }
        }

        // Determine which categories to use
        var categoryIds = request.CategoryIds ?? new List<int>();
        if (categoryIds.Count == 0 && request.CategoryId > 0)
        {
            // Fallback to single CategoryId if CategoryIds not provided
            categoryIds.Add(request.CategoryId);
        }
        
        if (categoryIds.Count == 0)
        {
            return BadRequest("At least one category is required.");
        }

        // Verify all categories exist
        var categories = await _context.Categories
            .Where(c => categoryIds.Contains(c.Id))
            .ToListAsync();
            
        if (categories.Count != categoryIds.Count)
        {
            var foundIds = categories.Select(c => c.Id).ToList();
            var missingIds = categoryIds.Except(foundIds).ToList();
            return BadRequest($"Categories with IDs {string.Join(", ", missingIds)} not found.");
        }

        // Use first category as primary (for backward compatibility)
        var primaryCategory = categories.First();

        var product = new Product
        {
            Name = request.Name.Trim(),
            Description = request.Description?.Trim(),
            Price = request.Price,
            CompareAtPrice = request.CompareAtPrice,
            Cost = request.Cost,
            Taxable = request.Taxable,
            Unit = request.Unit?.Trim(),
            SKU = request.SKU?.Trim(),
            Barcode = request.Barcode?.Trim(),
            InventoryTracked = request.InventoryTracked,
            SellWhenOutOfStock = request.SellWhenOutOfStock,
            AlwaysAvailable = request.AlwaysAvailable,
            IsPhysicalProduct = request.IsPhysicalProduct,
            Weight = request.Weight,
            Dimensions = request.Dimensions?.Trim(),
            PackageLength = request.PackageLength,
            PackageWidth = request.PackageWidth,
            PackageHeight = request.PackageHeight,
            CountryOfOrigin = request.CountryOfOrigin?.Trim(),
            Brand = request.Brand?.Trim(),
            Vendor = request.Vendor?.Trim(),
            ProductType = request.ProductType?.Trim(),
            Tags = request.Tags?.Trim(),
            Status = request.Status,
            PublishingChannels = request.PublishingChannels?.Trim(),
            ImageUrl = request.ImageUrl?.Trim(),
            MediaUrls = request.MediaUrls?.Trim(),
            VariantAttributes = request.VariantAttributes?.Trim(), // Variant attribute definitions
            WashingInstructions = request.WashingInstructions?.Trim(), // Optional washing/care instructions
            CategoryId = primaryCategory.Id, // Primary category for backward compatibility
            IsActive = request.IsActive,
            CreatedAt = DateTime.UtcNow
        };

        _context.Products.Add(product);
        await _context.SaveChangesAsync();

        // Add product to all specified categories
        foreach (var category in categories)
        {
            _context.ProductCategories.Add(new ProductCategory
            {
                ProductId = product.Id,
                CategoryId = category.Id
            });
        }
        await _context.SaveChangesAsync();

        // Automatically create inventory in Online Store warehouse if inventory tracking is enabled
        if (product.InventoryTracked)
        {
            var onlineWarehouse = await _context.Warehouses
                .FirstOrDefaultAsync(w => w.Name == "Online Store" || w.Name.ToLower().Contains("online"));
            
            if (onlineWarehouse != null)
            {
                // Check if inventory already exists (shouldn't, but just in case)
                var existingInventory = await _context.ProductInventories
                    .FirstOrDefaultAsync(pi => pi.ProductId == product.Id && pi.WarehouseId == onlineWarehouse.Id);
                
                if (existingInventory == null)
                {
                    var defaultInventory = new ProductInventory
                    {
                        ProductId = product.Id,
                        WarehouseId = onlineWarehouse.Id,
                        Quantity = 0, // Start with 0, user can update later
                        MinimumStockLevel = 2, // Default minimum stock level
                        MaximumStockLevel = 1000, // Default maximum stock level
                        Unit = product.Unit ?? "piece",
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };
                    _context.ProductInventories.Add(defaultInventory);
                    await _context.SaveChangesAsync();
                }
            }
        }

        // Create variants if provided
        if (request.Variants != null && request.Variants.Any())
        {
            foreach (var variantRequest in request.Variants)
            {
                var variant = new ProductVariant
                {
                    ProductId = product.Id,
                    Color = variantRequest.Color?.Trim() ?? string.Empty, // Legacy - keep for backward compatibility
                    ColorHex = variantRequest.ColorHex?.Trim(),
                    Attributes = variantRequest.Attributes?.Trim(), // Flexible attributes as JSON
                    ImageUrl = variantRequest.ImageUrl?.Trim(),
                    MediaUrls = variantRequest.MediaUrls?.Trim(),
                    PriceOverride = variantRequest.PriceOverride,
                    SKU = variantRequest.SKU?.Trim(),
                    IsActive = variantRequest.IsActive,
                    CreatedAt = DateTime.UtcNow
                };
                _context.ProductVariants.Add(variant);
            }
            await _context.SaveChangesAsync();
        }

        // Reload product with variants and categories
        product = await _context.Products
            .Include(p => p.Category)
            .Include(p => p.ProductCategories)
                .ThenInclude(pc => pc.Category)
            .Include(p => p.Variants)
            .Include(p => p.ProductInventories)
                .ThenInclude(pi => pi.Warehouse)
            .FirstOrDefaultAsync(p => p.Id == product.Id);

        var allCategories = product.ProductCategories.Select(pc => pc.Category).ToList();
        var allCategoryIds = allCategories.Select(c => c.Id).ToList();
        var allCategoryNames = allCategories.Select(c => c.Name).ToList();

        // Audit log
        await _auditService.LogAsync(
            entity: "Product",
            entityId: product.Id.ToString(),
            action: "CREATE",
            actorUserId: GetCurrentUserId(),
            before: null,
            after: $"Name: {product.Name}, Price: {product.Price}, Categories: {string.Join(", ", allCategoryNames)}, SKU: {product.SKU}, Variants: {request.Variants?.Count ?? 0}"
        );

        // Audit log
        await _auditService.LogAsync(
            entity: "Product",
            entityId: product.Id.ToString(),
            action: "CREATE",
            actorUserId: GetCurrentUserId(),
            before: null,
            after: $"Name: {product.Name}, Price: {product.Price}, Categories: {string.Join(", ", allCategoryNames)}, SKU: {product.SKU}, Variants: {request.Variants?.Count ?? 0}"
        );

        var response = new ProductResponse
        {
            Id = product.Id,
            Name = product.Name,
            Description = product.Description,
            Price = product.Price,
            CompareAtPrice = product.CompareAtPrice,
            Cost = product.Cost,
            Taxable = product.Taxable,
            Unit = product.Unit,
            SKU = product.SKU,
            Barcode = product.Barcode,
            InventoryTracked = product.InventoryTracked,
            SellWhenOutOfStock = product.SellWhenOutOfStock,
            AlwaysAvailable = product.AlwaysAvailable,
            IsPhysicalProduct = product.IsPhysicalProduct,
            Weight = product.Weight,
            Dimensions = product.Dimensions,
            PackageLength = product.PackageLength,
            PackageWidth = product.PackageWidth,
            PackageHeight = product.PackageHeight,
            CountryOfOrigin = product.CountryOfOrigin,
            Brand = product.Brand,
            Vendor = product.Vendor,
            ProductType = product.ProductType,
            Tags = product.Tags,
            Status = product.Status,
            PublishingChannels = product.PublishingChannels,
            ImageUrl = product.ImageUrl,
            MediaUrls = product.MediaUrls,
            VariantAttributes = product.VariantAttributes,
            CategoryId = product.CategoryId,
            CategoryName = primaryCategory.Name,
            CategoryIds = allCategoryIds,
            CategoryNames = allCategoryNames,
            IsActive = product.IsActive,
            CreatedAt = product.CreatedAt,
            UpdatedAt = product.UpdatedAt,
            TotalQuantity = product.ProductInventories.Sum(pi => pi.Quantity),
            Inventories = product.ProductInventories.Select(pi => new ProductInventoryResponse
            {
                Id = pi.Id,
                WarehouseId = pi.WarehouseId,
                WarehouseName = pi.Warehouse.Name,
                Quantity = pi.Quantity,
                Unit = pi.Unit,
                MinimumStockLevel = pi.MinimumStockLevel,
                MaximumStockLevel = pi.MaximumStockLevel
            }).ToList(),
            Variants = product.Variants.Select(v => new ProductVariantResponse
            {
                Id = v.Id,
                ProductId = v.ProductId,
                Color = v.Color,
                ColorHex = v.ColorHex,
                ImageUrl = v.ImageUrl,
                MediaUrls = v.MediaUrls,
                PriceOverride = v.PriceOverride,
                SKU = v.SKU,
                IsActive = v.IsActive,
                CreatedAt = v.CreatedAt,
                UpdatedAt = v.UpdatedAt
            }).ToList()
        };

        return CreatedAtAction(nameof(GetProduct), new { id = product.Id }, response);
    }

    /// <summary>
    /// Update a product (Requires Products.Update permission)
    /// </summary>
    [HttpPut("{id}")]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<ActionResult<ProductResponse>> UpdateProduct(int id, [FromBody] UpdateProductRequest request)
    {
        var product = await _context.Products
            .Include(p => p.Category)
            .Include(p => p.ProductCategories)
                .ThenInclude(pc => pc.Category)
            .Include(p => p.Variants)
            .Include(p => p.ProductInventories)
                .ThenInclude(pi => pi.Warehouse)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (product == null)
        {
            return NotFound($"Product with ID {id} not found.");
        }

        var before = $"Name: {product.Name}, Price: {product.Price}, Category: {product.Category.Name}, SKU: {product.SKU}";

        // Update fields if provided
        if (!string.IsNullOrWhiteSpace(request.Name))
        {
            product.Name = request.Name.Trim();
        }

        if (request.Description != null)
        {
            product.Description = request.Description.Trim();
        }

        // Pricing
        if (request.Price.HasValue)
        {
            product.Price = request.Price.Value;
        }

        if (request.CompareAtPrice.HasValue)
        {
            product.CompareAtPrice = request.CompareAtPrice.Value;
        }

        if (request.Cost.HasValue)
        {
            product.Cost = request.Cost.Value;
        }

        if (request.Taxable.HasValue)
        {
            product.Taxable = request.Taxable.Value;
        }

        if (request.Unit != null)
        {
            product.Unit = request.Unit.Trim();
        }

        // Inventory
        if (request.SKU != null)
        {
            // Check if new SKU already exists (excluding current product)
            if (!string.IsNullOrWhiteSpace(request.SKU))
            {
                var existingProduct = await _context.Products
                    .FirstOrDefaultAsync(p => p.SKU == request.SKU && p.Id != id);

                if (existingProduct != null)
                {
                    return BadRequest($"A product with SKU '{request.SKU}' already exists.");
                }
            }
            product.SKU = request.SKU.Trim();
        }

        if (request.Barcode != null)
        {
            // Check if new Barcode already exists (excluding current product)
            if (!string.IsNullOrWhiteSpace(request.Barcode))
            {
                var existingProduct = await _context.Products
                    .FirstOrDefaultAsync(p => p.Barcode == request.Barcode && p.Id != id);

                if (existingProduct != null)
                {
                    return BadRequest($"A product with Barcode '{request.Barcode}' already exists.");
                }
            }
            product.Barcode = request.Barcode.Trim();
        }

        if (request.InventoryTracked.HasValue)
        {
            product.InventoryTracked = request.InventoryTracked.Value;
        }

        if (request.SellWhenOutOfStock.HasValue)
        {
            product.SellWhenOutOfStock = request.SellWhenOutOfStock.Value;
        }

        if (request.AlwaysAvailable.HasValue)
        {
            product.AlwaysAvailable = request.AlwaysAvailable.Value;
        }

        // Shipping
        if (request.IsPhysicalProduct.HasValue)
        {
            product.IsPhysicalProduct = request.IsPhysicalProduct.Value;
        }

        if (request.Weight.HasValue)
        {
            product.Weight = request.Weight.Value;
        }

        if (request.Dimensions != null)
        {
            product.Dimensions = request.Dimensions.Trim();
        }

        if (request.PackageLength.HasValue)
        {
            product.PackageLength = request.PackageLength.Value;
        }

        if (request.PackageWidth.HasValue)
        {
            product.PackageWidth = request.PackageWidth.Value;
        }

        if (request.PackageHeight.HasValue)
        {
            product.PackageHeight = request.PackageHeight.Value;
        }

        if (request.CountryOfOrigin != null)
        {
            product.CountryOfOrigin = request.CountryOfOrigin.Trim();
        }

        // Product Organization
        if (request.Brand != null)
        {
            product.Brand = request.Brand.Trim();
        }

        if (request.Vendor != null)
        {
            product.Vendor = request.Vendor.Trim();
        }

        if (request.ProductType != null)
        {
            product.ProductType = request.ProductType.Trim();
        }

        if (request.Tags != null)
        {
            product.Tags = request.Tags.Trim();
        }

        // Handle CategoryIds (multiple categories) - this takes precedence over CategoryId
        // Only update categories if CategoryIds is explicitly provided and not empty
        // IMPORTANT: If CategoryIds is an empty array [], we should NOT remove existing categories
        // Only update if CategoryIds is provided AND has at least one valid category
        if (request.CategoryIds != null && request.CategoryIds.Count > 0)
        {
            // Validate FIRST before removing existing categories
            // Verify all categories exist
            var categories = await _context.Categories
                .Where(c => request.CategoryIds.Contains(c.Id))
                .ToListAsync();
                
            if (categories.Count != request.CategoryIds.Count)
            {
                var foundIds = categories.Select(c => c.Id).ToList();
                var missingIds = request.CategoryIds.Except(foundIds).ToList();
                return BadRequest($"Categories with IDs {string.Join(", ", missingIds)} not found.");
            }

            if (categories.Count == 0)
            {
                return BadRequest("At least one valid category is required.");
            }

            // Only remove and update AFTER validation passes
            // Remove existing ProductCategory relationships
            var existingProductCategories = await _context.ProductCategories
                .Where(pc => pc.ProductId == product.Id)
                .ToListAsync();
            _context.ProductCategories.RemoveRange(existingProductCategories);

            // Add new ProductCategory relationships
            foreach (var category in categories)
            {
                _context.ProductCategories.Add(new ProductCategory
                {
                    ProductId = product.Id,
                    CategoryId = category.Id
                });
            }

            // Update primary CategoryId (use first category for backward compatibility)
            product.CategoryId = categories.First().Id;
        }
        // If CategoryIds is explicitly null (not provided), don't touch categories
        // If CategoryIds is an empty array [], also don't touch categories (preserve existing)
        // Only update categories if CategoryIds has values OR CategoryId is provided
        else if (request.CategoryId.HasValue && request.CategoryId.Value > 0)
        {
            // Fallback to single CategoryId if CategoryIds not provided
            var category = await _context.Categories.FindAsync(request.CategoryId.Value);
            if (category == null)
            {
                return BadRequest($"Category with ID {request.CategoryId.Value} not found.");
            }
            product.CategoryId = request.CategoryId.Value;
            
            // Ensure ProductCategory relationship exists
            var existingProductCategory = await _context.ProductCategories
                .FirstOrDefaultAsync(pc => pc.ProductId == product.Id && pc.CategoryId == category.Id);
            if (existingProductCategory == null)
            {
                _context.ProductCategories.Add(new ProductCategory
                {
                    ProductId = product.Id,
                    CategoryId = category.Id
                });
            }
        }

        if (request.PublishingChannels != null)
        {
            product.PublishingChannels = request.PublishingChannels.Trim();
        }

        // Media
        if (request.ImageUrl != null)
        {
            product.ImageUrl = request.ImageUrl.Trim();
        }

        if (request.MediaUrls != null)
        {
            product.MediaUrls = string.IsNullOrWhiteSpace(request.MediaUrls) ? null : request.MediaUrls.Trim();
        }

        if (request.VariantAttributes != null)
        {
            product.VariantAttributes = string.IsNullOrWhiteSpace(request.VariantAttributes) ? null : request.VariantAttributes.Trim();
        }

        if (request.WashingInstructions != null)
        {
            product.WashingInstructions = string.IsNullOrWhiteSpace(request.WashingInstructions) ? null : request.WashingInstructions.Trim();
        }

        // IMPORTANT: Handle Status and IsActive together to prevent accidental deactivation
        // Rule: If product is currently active, only deactivate if BOTH IsActive=false AND Status=Draft are explicitly set
        // This prevents products from disappearing when updating other fields
        
        // Store the original active state before any changes
        var wasActive = product.IsActive;
        
        // Handle Status first
        if (request.Status.HasValue)
        {
            product.Status = request.Status.Value;
            // Only sync IsActive with Status if IsActive wasn't explicitly provided
            // AND the product is currently active (to prevent accidental deactivation)
            if (!request.IsActive.HasValue)
            {
                // If product is active and Status is being set to Active, keep it active
                // If product is active and Status is being set to Draft, only deactivate if explicitly intended
                if (wasActive && request.Status.Value == ProductStatus.Draft)
                {
                    // Don't auto-deactivate - preserve active state unless IsActive is explicitly false
                    // This prevents accidental deactivation
                }
                else
                {
                    // Safe to sync: either product is inactive, or Status is Active
                    product.IsActive = request.Status.Value == ProductStatus.Active;
                }
            }
        }

        // Handle IsActive with strong protection against accidental deactivation
        if (request.IsActive.HasValue)
        {
            // CRITICAL: If product is currently active and request wants to deactivate,
            // require BOTH IsActive=false AND Status=Draft to be explicitly set
            if (request.IsActive.Value == false && wasActive)
            {
                // Only allow deactivation if Status is also explicitly set to Draft
                // This ensures the user really wants to deactivate
                if (request.Status.HasValue && request.Status.Value == ProductStatus.Draft)
                {
                    // Both conditions met - safe to deactivate
                    product.IsActive = false;
                    product.Status = ProductStatus.Draft;
                }
                else
                {
                    // Status is not Draft or not provided - preserve active state
                    // Log a warning but don't deactivate
                    _logger.LogWarning(
                        "Prevented accidental deactivation of product {ProductId}. IsActive=false was sent but Status is not Draft. Preserving active state.",
                        product.Id
                    );
                    // Keep product active - don't change IsActive
                }
            }
            else
            {
                // Safe to update: either IsActive is true, or product is already inactive
                product.IsActive = request.IsActive.Value;
                // Sync Status with IsActive for backward compatibility (only if Status wasn't explicitly set)
                if (!request.Status.HasValue)
                {
                    product.Status = request.IsActive.Value ? ProductStatus.Active : ProductStatus.Draft;
                }
            }
        }

        product.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        // Reload product with all relationships including updated categories
        product = await _context.Products
            .Include(p => p.Category)
            .Include(p => p.ProductCategories)
                .ThenInclude(pc => pc.Category)
            .Include(p => p.Variants)
            .Include(p => p.ProductInventories)
                .ThenInclude(pi => pi.Warehouse)
            .FirstOrDefaultAsync(p => p.Id == id);

        // Check if product still exists after reload (should never be null, but safety check)
        if (product == null)
        {
            _logger.LogError("Product with ID {ProductId} was not found after update. This should not happen.", id);
            return NotFound($"Product with ID {id} was not found after update.");
        }

        var allCategoryNames = product.ProductCategories?.Select(pc => pc.Category?.Name).Where(name => name != null).ToList() ?? new List<string>();

        // Audit log
        var after = $"Name: {product.Name}, Price: {product.Price}, Categories: {string.Join(", ", allCategoryNames)}, SKU: {product.SKU}";
        await _auditService.LogAsync(
            entity: "Product",
            entityId: product.Id.ToString(),
            action: "UPDATE",
            actorUserId: GetCurrentUserId(),
            before: before,
            after: after
        );

        var allCategoryIds = product.ProductCategories.Select(pc => pc.CategoryId).ToList();

        // Check if product has active variants
        var hasActiveVariants = product.Variants.Any(v => v.IsActive);
        
        // Calculate TotalQuantity:
        // - If product has variants, sum from VariantInventories
        // - Otherwise, sum from ProductInventories
        decimal totalQuantity = 0;
        if (hasActiveVariants)
        {
            // Get variant inventories for this product's variants
            var productVariantIds = product.Variants.Where(v => v.IsActive).Select(v => v.Id).ToList();
            var variantInventories = await _context.VariantInventories
                .Where(vi => productVariantIds.Contains(vi.ProductVariantId))
                .ToListAsync();
            totalQuantity = variantInventories.Sum(vi => vi.Quantity);
        }
        else
        {
            // Use product-level inventory
            totalQuantity = product.ProductInventories.Sum(pi => pi.Quantity);
        }

        var response = new ProductResponse
        {
            Id = product.Id,
            Name = product.Name,
            Description = product.Description,
            Price = product.Price,
            CompareAtPrice = product.CompareAtPrice,
            Cost = product.Cost,
            Taxable = product.Taxable,
            Unit = product.Unit,
            SKU = product.SKU,
            Barcode = product.Barcode,
            InventoryTracked = product.InventoryTracked,
            SellWhenOutOfStock = product.SellWhenOutOfStock,
            AlwaysAvailable = product.AlwaysAvailable,
            IsPhysicalProduct = product.IsPhysicalProduct,
            Weight = product.Weight,
            Dimensions = product.Dimensions,
            PackageLength = product.PackageLength,
            PackageWidth = product.PackageWidth,
            PackageHeight = product.PackageHeight,
            CountryOfOrigin = product.CountryOfOrigin,
            Brand = product.Brand,
            Vendor = product.Vendor,
            ProductType = product.ProductType,
            Tags = product.Tags,
            Status = product.Status,
            PublishingChannels = product.PublishingChannels,
            ImageUrl = product.ImageUrl,
            MediaUrls = product.MediaUrls,
            CategoryId = product.CategoryId,
            CategoryName = product.Category.Name,
            CategoryIds = allCategoryIds,
            CategoryNames = allCategoryNames,
            IsActive = product.IsActive,
            CreatedAt = product.CreatedAt,
            UpdatedAt = product.UpdatedAt,
            TotalQuantity = totalQuantity,
            Inventories = product.ProductInventories.Select(pi => new ProductInventoryResponse
            {
                Id = pi.Id,
                WarehouseId = pi.WarehouseId,
                WarehouseName = pi.Warehouse.Name,
                Quantity = pi.Quantity,
                Unit = pi.Unit,
                MinimumStockLevel = pi.MinimumStockLevel,
                MaximumStockLevel = pi.MaximumStockLevel
            }).ToList(),
            Variants = product.Variants.Select(v => new ProductVariantResponse
            {
                Id = v.Id,
                ProductId = v.ProductId,
                Color = v.Color,
                ColorHex = v.ColorHex,
                Attributes = v.Attributes,
                ImageUrl = v.ImageUrl,
                MediaUrls = v.MediaUrls,
                PriceOverride = v.PriceOverride,
                SKU = v.SKU,
                IsActive = v.IsActive,
                CreatedAt = v.CreatedAt,
                UpdatedAt = v.UpdatedAt
            }).ToList()
        };

        return Ok(response);
    }

    /// <summary>
    /// Soft delete a product (Requires Products.Delete permission)
    /// </summary>
    [HttpDelete("{id}")]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<IActionResult> DeleteProduct(int id)
    {
        var product = await _context.Products.FindAsync(id);
        if (product == null)
        {
            return NotFound($"Product with ID {id} not found.");
        }

        var before = $"Name: {product.Name}, Price: {product.Price}, SKU: {product.SKU}";

        // Soft delete by setting IsActive to false
        product.IsActive = false;
        product.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        // Audit log
        var after = $"Name: {product.Name}, Price: {product.Price}, SKU: {product.SKU}, IsActive: {product.IsActive}";
        await _auditService.LogAsync(
            entity: "Product",
            entityId: product.Id.ToString(),
            action: "DELETE",
            actorUserId: GetCurrentUserId(),
            before: before,
            after: after
        );

        return NoContent();
    }

    /// <summary>
    /// Update product inventory in a specific warehouse (Requires Inventory.Update permission)
    /// </summary>
    [HttpPut("{id}/inventory/{warehouseId}")]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<ActionResult<ProductInventoryResponse>> UpdateInventory(int id, int warehouseId, [FromBody] UpdateInventoryRequest request)
    {
        var product = await _context.Products.FindAsync(id);
        if (product == null)
        {
            return NotFound($"Product with ID {id} not found.");
        }

        var warehouse = await _context.Warehouses.FindAsync(warehouseId);
        if (warehouse == null)
        {
            return NotFound($"Warehouse with ID {warehouseId} not found.");
        }

        var inventory = await _context.ProductInventories
            .FirstOrDefaultAsync(pi => pi.ProductId == id && pi.WarehouseId == warehouseId);

        var before = inventory != null ? $"Quantity: {inventory.Quantity}, Unit: {inventory.Unit}" : "No inventory record";

        if (inventory == null)
        {
            // Create new inventory record
            inventory = new ProductInventory
            {
                ProductId = id,
                WarehouseId = warehouseId,
                Quantity = request.Quantity,
                Unit = request.Unit?.Trim(),
                MinimumStockLevel = request.MinimumStockLevel,
                MaximumStockLevel = request.MaximumStockLevel,
                CreatedAt = DateTime.UtcNow
            };
            _context.ProductInventories.Add(inventory);
        }
        else
        {
            // Update existing inventory record
            inventory.Quantity = request.Quantity;
            inventory.Unit = request.Unit?.Trim();
            inventory.MinimumStockLevel = request.MinimumStockLevel;
            inventory.MaximumStockLevel = request.MaximumStockLevel;
            inventory.UpdatedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();

        // Audit log
        var after = $"Quantity: {inventory.Quantity}, Unit: {inventory.Unit}, MinLevel: {inventory.MinimumStockLevel}, MaxLevel: {inventory.MaximumStockLevel}";
        await _auditService.LogAsync(
            entity: "ProductInventory",
            entityId: inventory.Id.ToString(),
            action: inventory.CreatedAt == inventory.UpdatedAt ? "CREATE" : "UPDATE",
            actorUserId: GetCurrentUserId(),
            before: before,
            after: after
        );

        var response = new ProductInventoryResponse
        {
            Id = inventory.Id,
            WarehouseId = inventory.WarehouseId,
            WarehouseName = warehouse.Name,
            Quantity = inventory.Quantity,
            Unit = inventory.Unit,
            MinimumStockLevel = inventory.MinimumStockLevel,
            MaximumStockLevel = inventory.MaximumStockLevel
        };

        return Ok(response);
    }
}

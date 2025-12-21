using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Api.Data;
using Api.DTOs;
using Api.Services;
using Api.Attributes;
using Api.Models;
using System.Security.Claims;
using System.Linq;

namespace Api.Controllers;

[ApiController]
[Route("api/[controller]")]
// [Authorize] // Removed class-level authorize - add to individual methods as needed
public class DashboardController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IAuditService _auditService;
    private readonly IRevenueTrackingService _revenueTrackingService;
    private readonly ILogger<DashboardController> _logger;

    public DashboardController(ApplicationDbContext context, IAuditService auditService, IRevenueTrackingService revenueTrackingService, ILogger<DashboardController> logger)
    {
        _context = context;
        _auditService = auditService;
        _revenueTrackingService = revenueTrackingService;
        _logger = logger;
    }

    private int GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier");
        return userIdClaim != null ? int.Parse(userIdClaim.Value) : 0;
    }

    /// <summary>
    /// Get dashboard statistics with real-time revenue tracking
    /// </summary>
    [HttpGet("stats")]
    public async Task<ActionResult<DashboardStatsResponse>> GetStats()
    {
        try
        {
            // Use revenue tracking service for real-time totals
            var totalRevenue = await _revenueTrackingService.GetTotalRevenueAsync();
            var totalCosts = await _revenueTrackingService.GetTotalCostsAsync();

            // Count low stock items - both product and variant inventories
            // First, count product-level low stock (excluding products with active variants and always available)
            var productLowStockCount = await _context.ProductInventories
                .Include(pi => pi.Product)
                    .ThenInclude(p => p.Variants)
                .Where(pi => pi.Product != null && 
                             pi.Product.IsActive &&
                             !pi.Product.AlwaysAvailable &&
                             !(pi.Product.Variants != null && pi.Product.Variants.Any(v => v.IsActive)) &&
                             (pi.Quantity == 0 || 
                              (pi.MinimumStockLevel.HasValue && pi.Quantity <= pi.MinimumStockLevel.Value) ||
                              (pi.MinimumStockLevel.HasValue && pi.Quantity <= (pi.MinimumStockLevel.Value + 2))))
                .CountAsync();

            // Count variant-level low stock (including variants without inventory records = 0 stock)
            // First, count variants with inventory records that are low stock
            var variantInventoriesLowStock = await _context.VariantInventories
                .Include(vi => vi.ProductVariant)
                    .ThenInclude(pv => pv.Product)
                .Where(vi => vi.ProductVariant != null && 
                             vi.ProductVariant.IsActive &&
                             vi.ProductVariant.Product != null && 
                             vi.ProductVariant.Product.IsActive &&
                             !vi.ProductVariant.Product.AlwaysAvailable &&
                             (vi.Quantity == 0 || 
                              (vi.MinimumStockLevel.HasValue && vi.Quantity <= vi.MinimumStockLevel.Value) ||
                              (vi.MinimumStockLevel.HasValue && vi.Quantity <= (vi.MinimumStockLevel.Value + 2))))
                .Select(vi => vi.ProductVariantId)
                .Distinct()
                .CountAsync();

            // Count active variants without inventory records (these are 0 stock = low stock)
            var allActiveVariants = await _context.ProductVariants
                .Include(pv => pv.Product)
                .Where(pv => pv.IsActive &&
                             pv.Product != null &&
                             pv.Product.IsActive &&
                             !pv.Product.AlwaysAvailable)
                .Select(pv => pv.Id)
                .ToListAsync();

            var variantsWithInventory = await _context.VariantInventories
                .Where(vi => allActiveVariants.Contains(vi.ProductVariantId))
                .Select(vi => vi.ProductVariantId)
                .Distinct()
                .ToListAsync();

            var variantsWithoutInventory = allActiveVariants.Except(variantsWithInventory).Count();

            // Total variant low stock = variants with low stock inventory + variants without inventory (0 stock)
            var variantLowStockCount = variantInventoriesLowStock + variantsWithoutInventory;

            var stats = new DashboardStatsResponse
            {
                TotalProducts = await _context.Products.CountAsync(p => p.IsActive),
                TotalWarehouses = await _context.Warehouses.CountAsync(),
                TotalUsers = await _context.Users.CountAsync(u => u.IsActive),
                TotalInventory = await _context.ProductInventories.SumAsync(pi => pi.Quantity),
                PendingPurchases = await _context.PurchaseOrders.CountAsync(po => po.Status == "Pending"),
                PendingSales = await _context.SalesOrders.CountAsync(so => so.Status == "Pending"),
                LowStockItems = productLowStockCount + variantLowStockCount,
                TotalRevenue = totalRevenue, // Real-time revenue from tracking service
                TotalCosts = totalCosts, // Real-time costs from tracking service
                PendingRequests = await _context.ProductRequests.CountAsync(pr => pr.Status == "Pending"),
                CompletedAssemblies = await _context.ProductAssemblies.CountAsync(pa => pa.Status == "Completed"),
            };

            return Ok(stats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving dashboard statistics");
            return StatusCode(500, new { message = "An error occurred while retrieving dashboard statistics" });
        }
    }

    /// <summary>
    /// Get recent activity
    /// </summary>
    [HttpGet("recent-activity")]
    public async Task<ActionResult<IEnumerable<RecentActivityResponse>>> GetRecentActivity()
    {
        try
        {
            var activities = new List<RecentActivityResponse>();

            // Recent purchases
            var recentPurchases = await _context.PurchaseOrders
                .Include(po => po.CreatedByUser)
                .OrderByDescending(po => po.CreatedAt)
                .Take(5)
                .Select(po => new RecentActivityResponse
                {
                    Id = po.Id,
                    Type = "Purchase",
                    Title = $"Purchase Order #{po.OrderNumber}",
                    Description = $"Order from {po.SupplierName} - ${po.TotalAmount:F2}",
                    Status = po.Status,
                    CreatedAt = po.CreatedAt,
                    UserName = po.CreatedByUser.FullName
                })
                .ToListAsync();

            // Recent sales
            var recentSales = await _context.SalesOrders
                .Include(so => so.CreatedByUser)
                .OrderByDescending(so => so.CreatedAt)
                .Take(5)
                .Select(so => new RecentActivityResponse
                {
                    Id = so.Id,
                    Type = "Sale",
                    Title = $"Sales Order #{so.OrderNumber}",
                    Description = $"Order for {so.CustomerName} - ${so.TotalAmount:F2}",
                    Status = so.Status,
                    CreatedAt = so.CreatedAt,
                    UserName = so.CreatedByUser.FullName
                })
                .ToListAsync();

            // Recent product requests
            var recentRequests = await _context.ProductRequests
                .Include(pr => pr.RequestedByUser)
                .Include(pr => pr.Warehouse)
                .OrderByDescending(pr => pr.RequestDate)
                .Take(5)
                .Select(pr => new RecentActivityResponse
                {
                    Id = pr.Id,
                    Type = "Product Request",
                    Title = $"Product Request #{pr.Id}",
                    Description = $"Request for {pr.Warehouse.Name}",
                    Status = pr.Status,
                    CreatedAt = pr.RequestDate,
                    UserName = pr.RequestedByUser.FullName
                })
                .ToListAsync();

            activities.AddRange(recentPurchases);
            activities.AddRange(recentSales);
            activities.AddRange(recentRequests);

            return Ok(activities.OrderByDescending(a => a.CreatedAt).Take(10));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving recent activity");
            return StatusCode(500, new { message = "An error occurred while retrieving recent activity" });
        }
    }

    /// <summary>
    /// Test endpoint to check variants (temporary - for debugging)
    /// </summary>
    [HttpGet("test-variants")]
    public async Task<ActionResult> TestVariants()
    {
        try
        {
            var allVariants = await _context.ProductVariants
                .Include(pv => pv.Product)
                .Where(pv => pv.Product != null && pv.Product.Name.Contains("JST Classic Knit Pullover") && pv.Product.Name.Contains("Black"))
                .ToListAsync();
            
            var result = allVariants.Select(v => new
            {
                VariantId = v.Id,
                IsActive = v.IsActive,
                Attributes = v.Attributes,
                ProductId = v.ProductId,
                ProductName = v.Product?.Name,
                ProductIsActive = v.Product?.IsActive,
                ProductAlwaysAvailable = v.Product?.AlwaysAvailable,
                ProductStatus = v.Product?.Status.ToString()
            }).ToList();
            
            return Ok(new { count = result.Count, variants = result });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Get low stock items for both products and variants
    /// </summary>
    [HttpGet("low-stock-items")]
    [Authorize]
    public async Task<ActionResult<IEnumerable<LowStockItemResponse>>> GetLowStockItems()
    {
        try
        {
            _logger.LogInformation("=== GetLowStockItems endpoint called - UPDATED CODE ===");
            Console.WriteLine("=== GetLowStockItems endpoint called - UPDATED CODE ===");
            var lowStockItems = new List<LowStockItemResponse>();

            // Load all product inventories with related data - only active products
            var productInventories = await _context.ProductInventories
                .Include(pi => pi.Product)
                    .ThenInclude(p => p.Variants)
                .Include(pi => pi.Warehouse)
                .Where(pi => pi.Product != null && 
                             pi.Warehouse != null && 
                             pi.Product.IsActive)
                .ToListAsync();

            // Filter for low stock product inventories
            foreach (var pi in productInventories)
            {
                var product = pi.Product!;
                var warehouse = pi.Warehouse!;
                
                // Skip products that are always available - they don't need inventory tracking
                if (product.AlwaysAvailable)
                {
                    continue;
                }
                
                // Skip products that have active variants - we'll check variant inventories instead
                var hasActiveVariants = product.Variants != null && product.Variants.Any(v => v.IsActive);
                if (hasActiveVariants)
                {
                    continue;
                }
                
                // Check if this is a low stock item
                bool isLowStock = false;
                
                // Out of stock
                if (pi.Quantity == 0)
                {
                    isLowStock = true;
                }
                // Or quantity is at or below minimum stock level
                else if (pi.MinimumStockLevel.HasValue && pi.Quantity <= pi.MinimumStockLevel.Value)
                {
                    isLowStock = true;
                }
                // Or quantity is within 2 units above minimum (warning zone)
                else if (pi.MinimumStockLevel.HasValue && pi.Quantity <= (pi.MinimumStockLevel.Value + 2))
                {
                    isLowStock = true;
                }

                if (isLowStock)
                {
                    var isOutOfStock = pi.Quantity == 0;
                    var isNotAvailable = isOutOfStock && !product.AlwaysAvailable && !product.SellWhenOutOfStock;
                    
                    // Determine severity
                    string severity;
                    if (isOutOfStock)
                    {
                        severity = "Critical";
                    }
                    else if (pi.MinimumStockLevel.HasValue && pi.Quantity < pi.MinimumStockLevel.Value * 0.5m)
                    {
                        severity = "High";
                    }
                    else if (pi.MinimumStockLevel.HasValue && pi.Quantity <= pi.MinimumStockLevel.Value)
                    {
                        severity = "Medium";
                    }
                    else
                    {
                        severity = "Low";
                    }

                    // Use product unit if available, otherwise use inventory unit, default to "piece"
                    var displayUnit = product.Unit ?? pi.Unit ?? "piece";
                    // Normalize "bottle" to "piece" for furniture items
                    if (displayUnit == "bottle")
                    {
                        displayUnit = "piece";
                    }

                    lowStockItems.Add(new LowStockItemResponse
                    {
                        Type = "Product",
                        Id = pi.Id,
                        ProductId = product.Id,
                        ProductName = product.Name ?? "Unknown Product",
                        ProductSKU = product.SKU,
                        VariantId = null,
                        VariantAttributes = null,
                        WarehouseId = warehouse.Id,
                        WarehouseName = warehouse.Name ?? "Unknown Warehouse",
                        Quantity = pi.Quantity,
                        Unit = displayUnit,
                        MinimumStockLevel = pi.MinimumStockLevel,
                        MaximumStockLevel = pi.MaximumStockLevel,
                        IsOutOfStock = isOutOfStock,
                        IsNotAvailable = isNotAvailable,
                        Severity = severity,
                        UpdatedAt = pi.UpdatedAt
                    });
                }
            }

            // Get all variants for products that are not AlwaysAvailable
            // Be very lenient - include all variants where product exists and isn't AlwaysAvailable
            var allActiveVariants = await _context.ProductVariants
                .Include(pv => pv.Product)
                .Where(pv => pv.Product != null && !pv.Product.AlwaysAvailable)
                .ToListAsync();
            
            _logger.LogInformation($"Found {allActiveVariants.Count} variants to check for low stock (products not AlwaysAvailable)");
            Console.WriteLine($"Found {allActiveVariants.Count} variants to check for low stock (products not AlwaysAvailable)");
            
            // Log details about variants for "JST Classic Knit Pullover – Black"
            var blackPulloverVariants = allActiveVariants.Where(v => v.Product != null && v.Product.Name.Contains("JST Classic Knit Pullover") && v.Product.Name.Contains("Black")).ToList();
            if (blackPulloverVariants.Count > 0)
            {
                _logger.LogInformation($"Black pullover variants found: {blackPulloverVariants.Count}");
                Console.WriteLine($"Black pullover variants found: {blackPulloverVariants.Count}");
                foreach (var v in blackPulloverVariants)
                {
                    _logger.LogInformation($"  Variant {v.Id}: IsActive={v.IsActive}, Product.IsActive={v.Product?.IsActive}, Product.AlwaysAvailable={v.Product?.AlwaysAvailable}, Attributes={v.Attributes}");
                    Console.WriteLine($"  Variant {v.Id}: IsActive={v.IsActive}, Product.IsActive={v.Product?.IsActive}, Product.AlwaysAvailable={v.Product?.AlwaysAvailable}, Attributes={v.Attributes}");
                }
            }
            else
            {
                _logger.LogWarning("No black pullover variants found!");
                Console.WriteLine("No black pullover variants found!");
            }

            // Get the "Online Store" warehouse (primary warehouse for online orders)
            var onlineWarehouse = await _context.Warehouses
                .FirstOrDefaultAsync(w => w.IsActive && (w.Name == "Online Store" || w.Name.ToLower().Contains("online")));
            
            // If no online warehouse found, try to create it
            Warehouse? primaryWarehouse = onlineWarehouse;
            if (primaryWarehouse == null)
            {
                _logger.LogWarning("Online Store warehouse not found. Attempting to create it...");
                Console.WriteLine("Online Store warehouse not found. Attempting to create it...");
                
                try
                {
                    primaryWarehouse = new Warehouse
                    {
                        Name = "Online Store",
                        Address = "Online",
                        City = "Virtual",
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow
                    };
                    _context.Warehouses.Add(primaryWarehouse);
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("✅ Created Online Store warehouse");
                    Console.WriteLine("✅ Created Online Store warehouse");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to create Online Store warehouse");
                    Console.WriteLine($"Failed to create Online Store warehouse: {ex.Message}");
                    
                    // Fallback: use the first active warehouse
                    primaryWarehouse = await _context.Warehouses
                        .Where(w => w.IsActive)
                        .FirstOrDefaultAsync();
                }
            }
            
            // If still no warehouse, use the first active warehouse
            if (primaryWarehouse == null)
            {
                primaryWarehouse = await _context.Warehouses
                    .Where(w => w.IsActive)
                    .FirstOrDefaultAsync();
            }

            _logger.LogInformation($"Primary warehouse: {(primaryWarehouse != null ? primaryWarehouse.Name : "NOT FOUND")}");
            Console.WriteLine($"Primary warehouse: {(primaryWarehouse != null ? primaryWarehouse.Name : "NOT FOUND")}");

            // Even if no warehouse is found, we can still check variants (they'll be treated as 0 stock)
            if (primaryWarehouse != null)
            {
                // Get all variant inventories for the primary warehouse
                // Don't filter by IsActive here - we want to check all variants
                var variantInventories = await _context.VariantInventories
                    .Include(vi => vi.ProductVariant)
                        .ThenInclude(pv => pv.Product)
                    .Where(vi => vi.ProductVariant != null && 
                                 vi.ProductVariant.Product != null && 
                                 vi.WarehouseId == primaryWarehouse.Id)
                    .ToListAsync();

                // Create a dictionary for quick lookup: VariantId -> VariantInventory
                var variantInventoryMap = variantInventories
                    .ToDictionary(vi => vi.ProductVariantId, vi => vi);

                _logger.LogInformation($"Found {variantInventories.Count} variant inventory records for warehouse {primaryWarehouse.Id}");
                Console.WriteLine($"Found {variantInventories.Count} variant inventory records for warehouse {primaryWarehouse.Id}");

                // Check each variant for low stock (including variants without inventory records = 0 stock)
                int variantsChecked = 0;
                int variantsAdded = 0;
                
                _logger.LogInformation($"Starting to check {allActiveVariants.Count} variants for low stock");
                Console.WriteLine($"Starting to check {allActiveVariants.Count} variants for low stock");
                
                foreach (var variant in allActiveVariants)
                {
                    var product = variant.Product!;
                    
                    // Skip products that are always available - they don't need inventory tracking
                    // (This is a double-check since we already filtered in the query, but keep it for safety)
                    if (product.AlwaysAvailable)
                    {
                        variantsChecked++;
                        continue;
                    }

                    // Try to find existing inventory record for this variant in the primary warehouse
                    var variantInventory = variantInventoryMap.ContainsKey(variant.Id) ? variantInventoryMap[variant.Id] : null;
                    
                    // If no inventory record exists, treat as 0 stock (low stock)
                    // If inventory record exists, check if it's low stock
                    decimal quantity = variantInventory?.Quantity ?? 0;
                    decimal? minimumStockLevel = variantInventory?.MinimumStockLevel;
                    decimal? maximumStockLevel = variantInventory?.MaximumStockLevel;
                    
                    // Debug log for black pullover variants
                    if (product.Name.Contains("JST Classic Knit Pullover") && product.Name.Contains("Black"))
                    {
                        _logger.LogInformation($"  🔍 Checking variant {variant.Id}: quantity={quantity}, hasInventory={variantInventory != null}, attributes={variant.Attributes}");
                        Console.WriteLine($"  🔍 Checking variant {variant.Id}: quantity={quantity}, hasInventory={variantInventory != null}, attributes={variant.Attributes}");
                    }
                    
                    // Check if this is a low stock item
                    bool isLowStock = false;
                    
                    // Out of stock (no inventory record or quantity is 0)
                    if (quantity == 0)
                    {
                        isLowStock = true;
                    }
                // Or quantity is at or below minimum stock level
                    else if (minimumStockLevel.HasValue && quantity <= minimumStockLevel.Value)
                {
                    isLowStock = true;
                }
                // Or quantity is within 2 units above minimum (warning zone)
                    else if (minimumStockLevel.HasValue && quantity <= (minimumStockLevel.Value + 2))
                {
                    isLowStock = true;
                }

                if (isLowStock)
                {
                        var isOutOfStock = quantity == 0;
                    var isNotAvailable = isOutOfStock && !product.AlwaysAvailable && !product.SellWhenOutOfStock;
                    
                    // Determine severity
                    string severity;
                    if (isOutOfStock)
                    {
                        severity = "Critical";
                    }
                        else if (minimumStockLevel.HasValue && quantity < minimumStockLevel.Value * 0.5m)
                    {
                        severity = "High";
                    }
                        else if (minimumStockLevel.HasValue && quantity <= minimumStockLevel.Value)
                    {
                        severity = "Medium";
                    }
                    else
                    {
                        severity = "Low";
                    }

                    // Use product unit if available, otherwise use inventory unit, default to "piece"
                        var variantDisplayUnit = product.Unit ?? variantInventory?.Unit ?? "piece";
                    // Normalize "bottle" to "piece" for furniture items
                    if (variantDisplayUnit == "bottle")
                    {
                        variantDisplayUnit = "piece";
                    }

                        var lowStockItem = new LowStockItemResponse
                    {
                        Type = "Variant",
                            Id = variantInventory?.Id ?? 0, // 0 if no inventory record exists
                        ProductId = product.Id,
                        ProductName = product.Name ?? "Unknown Product",
                        ProductSKU = product.SKU,
                        VariantId = variant.Id,
                        VariantAttributes = variant.Attributes,
                            WarehouseId = primaryWarehouse!.Id,
                            WarehouseName = primaryWarehouse!.Name ?? "Unknown Warehouse",
                            Quantity = quantity,
                        Unit = variantDisplayUnit,
                            MinimumStockLevel = minimumStockLevel,
                            MaximumStockLevel = maximumStockLevel,
                        IsOutOfStock = isOutOfStock,
                        IsNotAvailable = isNotAvailable,
                        Severity = severity,
                            UpdatedAt = variantInventory?.UpdatedAt ?? variant.UpdatedAt ?? DateTime.UtcNow
                        };
                        
                        lowStockItems.Add(lowStockItem);
                        variantsAdded++;
                        
                        // Debug logging for variants with 0 stock
                        if (quantity == 0)
                        {
                            _logger.LogInformation($"Added variant low stock item: {product.Name} - Variant {variant.Id} (Attributes: {variant.Attributes}) - Qty: 0");
                            Console.WriteLine($"Added variant low stock item: {product.Name} - Variant {variant.Id} (Attributes: {variant.Attributes}) - Qty: 0");
                        }
                    }
                    variantsChecked++;
                }
                
                _logger.LogInformation($"Checked {variantsChecked} variants, added {variantsAdded} to low stock items");
                Console.WriteLine($"Checked {variantsChecked} variants, added {variantsAdded} to low stock items");
            }
            else
            {
                // Even without a warehouse, we can still check variants (treat as 0 stock)
                _logger.LogWarning("⚠️ No warehouse found - checking variants anyway (will treat as 0 stock)");
                Console.WriteLine("⚠️ No warehouse found - checking variants anyway (will treat as 0 stock)");
                
                int variantsCheckedNoWarehouse = 0;
                int variantsAddedNoWarehouse = 0;
                foreach (var variant in allActiveVariants)
                {
                    var product = variant.Product!;
                    
                    if (product.AlwaysAvailable)
                    {
                        variantsCheckedNoWarehouse++;
                        continue;
                    }
                    
                    // No warehouse = no inventory records = 0 stock = low stock
                    decimal quantity = 0;
                    bool isLowStock = true; // Always low stock if no warehouse
                    var isOutOfStock = true;
                    var isNotAvailable = !product.AlwaysAvailable && !product.SellWhenOutOfStock;
                    
                    var variantDisplayUnit = product.Unit ?? "piece";
                    if (variantDisplayUnit == "bottle")
                    {
                        variantDisplayUnit = "piece";
                    }
                    
                    var lowStockItem = new LowStockItemResponse
                    {
                        Type = "Variant",
                        Id = 0,
                        ProductId = product.Id,
                        ProductName = product.Name ?? "Unknown Product",
                        ProductSKU = product.SKU,
                        VariantId = variant.Id,
                        VariantAttributes = variant.Attributes,
                        WarehouseId = 0,
                        WarehouseName = "No Warehouse",
                        Quantity = quantity,
                        Unit = variantDisplayUnit,
                        MinimumStockLevel = null,
                        MaximumStockLevel = null,
                        IsOutOfStock = isOutOfStock,
                        IsNotAvailable = isNotAvailable,
                        Severity = "Critical",
                        UpdatedAt = variant.UpdatedAt ?? DateTime.UtcNow
                    };
                    
                    lowStockItems.Add(lowStockItem);
                    variantsAddedNoWarehouse++;
                    variantsCheckedNoWarehouse++;
                    
                    _logger.LogInformation($"✅ Added variant low stock item (no warehouse): {product.Name} - Variant {variant.Id} (Attributes: {variant.Attributes}) - Qty: 0");
                    Console.WriteLine($"✅ Added variant low stock item (no warehouse): {product.Name} - Variant {variant.Id} (Attributes: {variant.Attributes}) - Qty: 0");
                }
                
                _logger.LogInformation($"Checked {variantsCheckedNoWarehouse} variants without warehouse, added {variantsAddedNoWarehouse} to low stock items");
                Console.WriteLine($"Checked {variantsCheckedNoWarehouse} variants without warehouse, added {variantsAddedNoWarehouse} to low stock items");
            }
            
            _logger.LogInformation($"Total low stock items found: {lowStockItems.Count} (Product: {lowStockItems.Count(i => i.Type == "Product")}, Variant: {lowStockItems.Count(i => i.Type == "Variant")})");
            Console.WriteLine($"Total low stock items found: {lowStockItems.Count} (Product: {lowStockItems.Count(i => i.Type == "Product")}, Variant: {lowStockItems.Count(i => i.Type == "Variant")})");

            _logger.LogInformation($"Returning {lowStockItems.Count} low stock items");
            return Ok(lowStockItems.OrderBy(item => item.Quantity).ThenBy(item => item.Severity));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving low stock items: {Message}", ex.Message);
            return StatusCode(500, new { message = "An error occurred while retrieving low stock items", error = ex.Message });
        }
    }

    /// <summary>
    /// Refresh revenue and cost totals (Admin only)
    /// </summary>
    [HttpPost("refresh-totals")]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<IActionResult> RefreshTotals()
    {
        try
        {
            await _revenueTrackingService.RefreshAllTotalsAsync();
            return Ok(new { message = "Revenue and cost totals refreshed successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error refreshing totals");
            return StatusCode(500, new { message = "An error occurred while refreshing totals" });
        }
    }
}
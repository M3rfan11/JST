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
[Authorize] // All endpoints require authentication
public class ProductInventoryController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IAuditService _auditService;
    private readonly ILogger<ProductInventoryController> _logger;

    public ProductInventoryController(ApplicationDbContext context, IAuditService auditService, ILogger<ProductInventoryController> logger)
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

    private async Task<List<string>> GetCurrentUserRoles(int userId)
    {
        return await _context.UserRoles
            .Where(ur => ur.UserId == userId)
            .Include(ur => ur.Role)
            .Select(ur => ur.Role.Name)
            .ToListAsync();
    }

    /// <summary>
    /// Create new product inventory
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<ActionResult<ProductInventoryResponse>> CreateInventory([FromBody] CreateProductInventoryRequest request)
    {
        try
        {
            // Check if inventory already exists for this product and warehouse
            var existingInventory = await _context.ProductInventories
                .FirstOrDefaultAsync(pi => pi.ProductId == request.ProductId && pi.WarehouseId == request.WarehouseId);

            if (existingInventory != null)
            {
                return BadRequest("Inventory already exists for this product in this warehouse. Use update instead.");
            }

            var inventory = new ProductInventory
            {
                ProductId = request.ProductId,
                WarehouseId = request.WarehouseId,
                Quantity = request.Quantity,
                MinimumStockLevel = request.MinimumStockLevel ?? 0,
                MaximumStockLevel = request.MaximumStockLevel ?? 1000,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.ProductInventories.Add(inventory);
            await _context.SaveChangesAsync();

            await _auditService.LogAsync("ProductInventory", inventory.Id.ToString(), "Created", 
                null, System.Text.Json.JsonSerializer.Serialize(inventory), GetCurrentUserId());

            var response = await _context.ProductInventories
                .Include(pi => pi.Product)
                .Include(pi => pi.Warehouse)
                .Where(pi => pi.Id == inventory.Id)
                .Select(pi => new ProductInventoryResponse
                {
                    Id = pi.Id,
                    ProductId = pi.ProductId,
                    ProductName = pi.Product.Name,
                    ProductSKU = pi.Product.SKU ?? "",
                    WarehouseId = pi.WarehouseId,
                    WarehouseName = pi.Warehouse.Name,
                    Quantity = pi.Quantity,
                    MinimumStockLevel = pi.MinimumStockLevel,
                    MaximumStockLevel = pi.MaximumStockLevel,
                    CreatedAt = pi.CreatedAt,
                    UpdatedAt = pi.UpdatedAt
                })
                .FirstOrDefaultAsync();

            return CreatedAtAction(nameof(GetInventory), new { id = inventory.Id }, response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating product inventory");
            return StatusCode(500, new { message = "An error occurred while creating the inventory" });
        }
    }

    /// <summary>
    /// Get all product inventories
    /// </summary>
    [HttpGet]
    [Authorize]
    public async Task<ActionResult<IEnumerable<ProductInventoryResponse>>> GetInventories()
    {
        try
        {
            var currentUserId = GetCurrentUserId();
            var currentUserRoles = await GetCurrentUserRoles(currentUserId);
            
            IQueryable<ProductInventory> inventoryQuery = _context.ProductInventories
                .Include(pi => pi.Product)
                .Include(pi => pi.Warehouse);

            // Apply role-based filtering
            if (currentUserRoles.Contains("SuperAdmin"))
            {
                // SuperAdmin can see all inventories
            }
            else
            {
                return StatusCode(403, new { Message = "You don't have permission to view inventory" });
            }

            var inventories = await inventoryQuery
                .Select(pi => new ProductInventoryResponse
                {
                    Id = pi.Id,
                    ProductId = pi.ProductId,
                    ProductName = pi.Product.Name,
                    WarehouseId = pi.WarehouseId,
                    WarehouseName = pi.Warehouse.Name,
                    Quantity = pi.Quantity,
                    Unit = pi.Unit,
                    MinimumStockLevel = pi.MinimumStockLevel,
                    MaximumStockLevel = pi.MaximumStockLevel,
                    UpdatedAt = pi.UpdatedAt
                })
                .OrderBy(pi => pi.ProductName)
                .ThenBy(pi => pi.WarehouseName)
                .ToListAsync();

            return Ok(inventories);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving product inventories");
            return StatusCode(500, new { message = "An error occurred while retrieving inventories" });
        }
    }

    /// <summary>
    /// Get product inventory by ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<ProductInventoryResponse>> GetInventory(int id)
    {
        try
        {
            var inventory = await _context.ProductInventories
                .Include(pi => pi.Product)
                .Include(pi => pi.Warehouse)
                .Where(pi => pi.Id == id)
                .Select(pi => new ProductInventoryResponse
                {
                    Id = pi.Id,
                    ProductId = pi.ProductId,
                    ProductName = pi.Product.Name,
                    WarehouseId = pi.WarehouseId,
                    WarehouseName = pi.Warehouse.Name,
                    Quantity = pi.Quantity,
                    Unit = pi.Unit,
                    MinimumStockLevel = pi.MinimumStockLevel,
                    MaximumStockLevel = pi.MaximumStockLevel,
                    UpdatedAt = pi.UpdatedAt
                })
                .FirstOrDefaultAsync();

            if (inventory == null)
            {
                return NotFound($"Inventory with ID {id} not found.");
            }

            return Ok(inventory);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving product inventory {InventoryId}", id);
            return StatusCode(500, new { message = "An error occurred while retrieving the inventory" });
        }
    }

    /// <summary>
    /// Update product inventory (Admin only)
    /// </summary>
    [HttpPut("{id}")]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<ActionResult<ProductInventoryResponse>> UpdateInventory(int id, [FromBody] UpdateInventoryRequest request)
    {
        try
        {
            var inventory = await _context.ProductInventories
                .Include(pi => pi.Product)
                .Include(pi => pi.Warehouse)
                .FirstOrDefaultAsync(pi => pi.Id == id);

            if (inventory == null)
            {
                return NotFound($"Inventory with ID {id} not found.");
            }

            var before = $"Quantity: {inventory.Quantity}, Unit: {inventory.Unit}, MinLevel: {inventory.MinimumStockLevel}, MaxLevel: {inventory.MaximumStockLevel}";

            // Update inventory
            inventory.Quantity = request.Quantity;
            inventory.Unit = request.Unit?.Trim();
            inventory.MinimumStockLevel = request.MinimumStockLevel;
            inventory.MaximumStockLevel = request.MaximumStockLevel;
            inventory.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            // Audit log
            var after = $"Quantity: {inventory.Quantity}, Unit: {inventory.Unit}, MinLevel: {inventory.MinimumStockLevel}, MaxLevel: {inventory.MaximumStockLevel}";
            await _auditService.LogAsync(
                entity: "ProductInventory",
                entityId: inventory.Id.ToString(),
                action: "UPDATE",
                actorUserId: GetCurrentUserId(),
                before: before,
                after: after
            );

            var response = new ProductInventoryResponse
            {
                Id = inventory.Id,
                ProductId = inventory.ProductId,
                ProductName = inventory.Product.Name,
                WarehouseId = inventory.WarehouseId,
                WarehouseName = inventory.Warehouse.Name,
                Quantity = inventory.Quantity,
                Unit = inventory.Unit,
                MinimumStockLevel = inventory.MinimumStockLevel,
                MaximumStockLevel = inventory.MaximumStockLevel,
                UpdatedAt = inventory.UpdatedAt
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating product inventory {InventoryId}", id);
            return StatusCode(500, new { message = "An error occurred while updating the inventory" });
        }
    }

    /// <summary>
    /// Delete product inventory (Admin only)
    /// </summary>
    [HttpDelete("{id}")]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<IActionResult> DeleteInventory(int id)
    {
        try
        {
            var inventory = await _context.ProductInventories
                .Include(pi => pi.Product)
                .Include(pi => pi.Warehouse)
                .FirstOrDefaultAsync(pi => pi.Id == id);

            if (inventory == null)
            {
                return NotFound($"Inventory with ID {id} not found.");
            }

            var before = $"Product: {inventory.Product.Name}, Warehouse: {inventory.Warehouse.Name}, Quantity: {inventory.Quantity}";

            _context.ProductInventories.Remove(inventory);
            await _context.SaveChangesAsync();

            // Audit log
            await _auditService.LogAsync(
                entity: "ProductInventory",
                entityId: inventory.Id.ToString(),
                action: "DELETE",
                actorUserId: GetCurrentUserId(),
                before: before,
                after: "Deleted"
            );

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting product inventory {InventoryId}", id);
            return StatusCode(500, new { message = "An error occurred while deleting the inventory" });
        }
    }

    /// <summary>
    /// Get low stock items (product inventory only)
    /// Requires Inventory.Read permission
    /// </summary>
    [HttpGet("low-stock")]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<ActionResult<IEnumerable<ProductInventoryResponse>>> GetLowStockItems()
    {
        try
        {
            var lowStockItems = await _context.ProductInventories
                .Include(pi => pi.Product)
                .Include(pi => pi.Warehouse)
                .Where(pi => pi.MinimumStockLevel.HasValue && pi.Quantity <= pi.MinimumStockLevel.Value)
                .Select(pi => new ProductInventoryResponse
                {
                    Id = pi.Id,
                    ProductId = pi.ProductId,
                    ProductName = pi.Product.Name,
                    WarehouseId = pi.WarehouseId,
                    WarehouseName = pi.Warehouse.Name,
                    Quantity = pi.Quantity,
                    Unit = pi.Unit,
                    MinimumStockLevel = pi.MinimumStockLevel,
                    MaximumStockLevel = pi.MaximumStockLevel,
                    UpdatedAt = pi.UpdatedAt
                })
                .OrderBy(pi => pi.Quantity)
                .ToListAsync();

            return Ok(lowStockItems);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving low stock items");
            return StatusCode(500, new { message = "An error occurred while retrieving low stock items" });
        }
    }

    /// <summary>
    /// Initialize inventory for products that don't have inventory records
    /// </summary>
    [HttpPost("initialize-missing")]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<ActionResult> InitializeMissingInventories()
    {
        try
        {
            var onlineWarehouse = await _context.Warehouses
                .FirstOrDefaultAsync(w => w.Name == "Online Store" || w.Name.ToLower().Contains("online"));
            
            if (onlineWarehouse == null)
            {
                return BadRequest("Online Store warehouse not found. Please create it first.");
            }

            // Get all products that have inventory tracking enabled but no inventory records
            var productsWithoutInventory = await _context.Products
                .Where(p => p.InventoryTracked && 
                    !_context.ProductInventories.Any(pi => pi.ProductId == p.Id && pi.WarehouseId == onlineWarehouse.Id))
                .ToListAsync();

            var createdCount = 0;
            foreach (var product in productsWithoutInventory)
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
                createdCount++;
            }

            await _context.SaveChangesAsync();

            return Ok(new { 
                message = $"Initialized inventory for {createdCount} product(s) that were missing inventory records.",
                createdCount = createdCount
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initializing missing inventories");
            return StatusCode(500, new { message = "An error occurred while initializing missing inventories", error = ex.Message });
        }
    }

    /// <summary>
    /// Set default minimum stock levels for products that don't have them
    /// </summary>
    [HttpPost("set-default-minimum-levels")]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<IActionResult> SetDefaultMinimumLevels([FromBody] SetDefaultMinimumLevelsRequest request)
    {
        try
        {
            var inventoriesWithoutMinLevel = await _context.ProductInventories
                .Where(pi => !pi.MinimumStockLevel.HasValue)
                .ToListAsync();

            var updatedCount = 0;
            foreach (var inventory in inventoriesWithoutMinLevel)
            {
                inventory.MinimumStockLevel = request.DefaultMinimumLevel;
                inventory.UpdatedAt = DateTime.UtcNow;
                updatedCount++;
            }

            await _context.SaveChangesAsync();

            return Ok(new { 
                Message = $"Updated {updatedCount} inventory records with default minimum level of {request.DefaultMinimumLevel}",
                UpdatedCount = updatedCount
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting default minimum levels");
            return StatusCode(500, new { message = "An error occurred while setting default minimum levels" });
        }
    }

    /// <summary>
    /// Fix missing inventory records for all products in all stores
    /// </summary>
    [HttpPost("fix-missing-inventories")]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<IActionResult> FixMissingInventories()
    {
        try
        {
            var products = await _context.Products.ToListAsync();
            var stores = await _context.Warehouses.Where(w => w.IsActive).ToListAsync();
            
            var inventoriesToAdd = new List<ProductInventory>();
            var random = new Random();
            var addedCount = 0;
            
            foreach (var store in stores)
            {
                foreach (var product in products)
                {
                    // Check if inventory already exists for this product in this store
                    var existingInventory = await _context.ProductInventories
                        .FirstOrDefaultAsync(pi => pi.ProductId == product.Id && pi.WarehouseId == store.Id);
                    
                    if (existingInventory == null)
                    {
                        // Create missing inventory
                        var inventory = new ProductInventory
                        {
                            ProductId = product.Id,
                            WarehouseId = store.Id,
                            Quantity = random.Next(10, 100), // Random stock between 10-100
                            Unit = product.Unit,
                            MinimumStockLevel = 10,
                            MaximumStockLevel = 200,
                            CreatedAt = DateTime.UtcNow,
                            UpdatedAt = DateTime.UtcNow
                        };
                        
                        inventoriesToAdd.Add(inventory);
                        addedCount++;
                    }
                }
            }
            
            if (inventoriesToAdd.Any())
            {
                _context.ProductInventories.AddRange(inventoriesToAdd);
                await _context.SaveChangesAsync();
                
                await _auditService.LogAsync("ProductInventory", "Bulk", "Created", 
                    $"Added {addedCount} missing inventory records", null, GetCurrentUserId());
                
                return Ok(new { 
                    Message = $"Successfully added {addedCount} missing inventory records",
                    AddedCount = addedCount,
                    TotalProducts = products.Count,
                    TotalStores = stores.Count
                });
            }
            else
            {
                return Ok(new { 
                    Message = "No missing inventories found. All products already have inventory in all stores.",
                    AddedCount = 0,
                    TotalProducts = products.Count,
                    TotalStores = stores.Count
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fixing missing inventories");
            return StatusCode(500, new { Message = "Error fixing missing inventories", Error = ex.Message });
        }
    }
}

public class UpdateInventoryRequest
{
    public decimal Quantity { get; set; }
    public string? Unit { get; set; }
    public decimal? MinimumStockLevel { get; set; }
    public decimal? MaximumStockLevel { get; set; }
}

public class SetDefaultMinimumLevelsRequest
{
    public decimal DefaultMinimumLevel { get; set; }
}

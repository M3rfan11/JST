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
public class VariantInventoryController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IAuditService _auditService;

    public VariantInventoryController(ApplicationDbContext context, IAuditService auditService)
    {
        _context = context;
        _auditService = auditService;
    }

    private int GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier");
        return userIdClaim != null ? int.Parse(userIdClaim.Value) : 0;
    }

    /// <summary>
    /// Get inventory for multiple variants in batch (optimized for performance)
    /// Must be placed before generic [HttpPost] route to avoid conflicts
    /// </summary>
    [HttpPost("get-batch")]
    public async Task<ActionResult<Dictionary<int, IEnumerable<VariantInventoryResponse>>>> GetVariantInventoriesBatch([FromBody] int[] variantIds)
    {
        if (variantIds == null || variantIds.Length == 0)
        {
            return BadRequest("Variant IDs are required");
        }

        var inventories = await _context.VariantInventories
            .Include(vi => vi.Warehouse)
            .Where(vi => variantIds.Contains(vi.ProductVariantId))
            .Select(vi => new VariantInventoryResponse
            {
                Id = vi.Id,
                ProductVariantId = vi.ProductVariantId,
                WarehouseId = vi.WarehouseId,
                WarehouseName = vi.Warehouse.Name,
                Quantity = vi.Quantity,
                POSQuantity = vi.POSQuantity,
                Unit = vi.Unit,
                MinimumStockLevel = vi.MinimumStockLevel,
                MaximumStockLevel = vi.MaximumStockLevel,
                CreatedAt = vi.CreatedAt,
                UpdatedAt = vi.UpdatedAt
            })
            .ToListAsync();

        // Group by variant ID
        var result = inventories
            .GroupBy(vi => vi.ProductVariantId)
            .ToDictionary(g => g.Key, g => g.AsEnumerable());

        // Ensure all requested variant IDs are in the result (even if empty)
        foreach (var variantId in variantIds)
        {
            if (!result.ContainsKey(variantId))
            {
                result[variantId] = Enumerable.Empty<VariantInventoryResponse>();
            }
        }

        return Ok(result);
    }

    /// <summary>
    /// Get all inventory for a specific variant
    /// </summary>
    [HttpGet("variant/{variantId}")]
    public async Task<ActionResult<IEnumerable<VariantInventoryResponse>>> GetVariantInventory(int variantId)
    {
        var inventories = await _context.VariantInventories
            .Include(vi => vi.Warehouse)
            .Where(vi => vi.ProductVariantId == variantId)
            .Select(vi => new VariantInventoryResponse
            {
                Id = vi.Id,
                ProductVariantId = vi.ProductVariantId,
                WarehouseId = vi.WarehouseId,
                WarehouseName = vi.Warehouse.Name,
                Quantity = vi.Quantity,
                POSQuantity = vi.POSQuantity,
                Unit = vi.Unit,
                MinimumStockLevel = vi.MinimumStockLevel,
                MaximumStockLevel = vi.MaximumStockLevel,
                CreatedAt = vi.CreatedAt,
                UpdatedAt = vi.UpdatedAt
            })
            .ToListAsync();

        return Ok(inventories);
    }

    /// <summary>
    /// Get inventory for a variant in a specific warehouse
    /// </summary>
    [HttpGet("variant/{variantId}/warehouse/{warehouseId}")]
    public async Task<ActionResult<VariantInventoryResponse>> GetVariantInventoryByWarehouse(int variantId, int warehouseId)
    {
        var inventory = await _context.VariantInventories
            .Include(vi => vi.Warehouse)
            .Where(vi => vi.ProductVariantId == variantId && vi.WarehouseId == warehouseId)
            .Select(vi => new VariantInventoryResponse
            {
                Id = vi.Id,
                ProductVariantId = vi.ProductVariantId,
                WarehouseId = vi.WarehouseId,
                WarehouseName = vi.Warehouse.Name,
                Quantity = vi.Quantity,
                POSQuantity = vi.POSQuantity,
                Unit = vi.Unit,
                MinimumStockLevel = vi.MinimumStockLevel,
                MaximumStockLevel = vi.MaximumStockLevel,
                CreatedAt = vi.CreatedAt,
                UpdatedAt = vi.UpdatedAt
            })
            .FirstOrDefaultAsync();

        if (inventory == null)
        {
            return NotFound($"Inventory not found for variant {variantId} in warehouse {warehouseId}");
        }

        return Ok(inventory);
    }

    /// <summary>
    /// Create or update variant inventory (Requires Inventory.Create permission)
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<ActionResult<VariantInventoryResponse>> CreateVariantInventory([FromBody] CreateVariantInventoryRequest request)
    {
        var variant = await _context.ProductVariants.FindAsync(request.ProductVariantId);
        if (variant == null)
        {
            return NotFound($"Variant with ID {request.ProductVariantId} not found.");
        }

        var warehouse = await _context.Warehouses.FindAsync(request.WarehouseId);
        if (warehouse == null)
        {
            return NotFound($"Warehouse with ID {request.WarehouseId} not found.");
        }

        // Check if inventory already exists
        var existingInventory = await _context.VariantInventories
            .FirstOrDefaultAsync(vi => vi.ProductVariantId == request.ProductVariantId && vi.WarehouseId == request.WarehouseId);

        if (existingInventory != null)
        {
            return BadRequest($"Inventory already exists for variant {request.ProductVariantId} in warehouse {request.WarehouseId}. Use PUT to update.");
        }

        var inventory = new VariantInventory
        {
            ProductVariantId = request.ProductVariantId,
            WarehouseId = request.WarehouseId,
            Quantity = request.Quantity,
            Unit = request.Unit?.Trim(),
            MinimumStockLevel = request.MinimumStockLevel,
            MaximumStockLevel = request.MaximumStockLevel,
            CreatedAt = DateTime.UtcNow
        };

        _context.VariantInventories.Add(inventory);
        await _context.SaveChangesAsync();

        // Reload with warehouse
        await _context.Entry(inventory).Reference(vi => vi.Warehouse).LoadAsync();

        // Audit log
        await _auditService.LogAsync(
            entity: "VariantInventory",
            entityId: inventory.Id.ToString(),
            action: "CREATE",
            actorUserId: GetCurrentUserId(),
            before: null,
            after: $"VariantId: {request.ProductVariantId}, WarehouseId: {request.WarehouseId}, Quantity: {request.Quantity}"
        );

        var response = new VariantInventoryResponse
        {
            Id = inventory.Id,
            ProductVariantId = inventory.ProductVariantId,
            WarehouseId = inventory.WarehouseId,
            WarehouseName = inventory.Warehouse.Name,
            Quantity = inventory.Quantity,
            POSQuantity = inventory.POSQuantity,
            Unit = inventory.Unit,
            MinimumStockLevel = inventory.MinimumStockLevel,
            MaximumStockLevel = inventory.MaximumStockLevel,
            CreatedAt = inventory.CreatedAt,
            UpdatedAt = inventory.UpdatedAt
        };

        return CreatedAtAction(nameof(GetVariantInventoryByWarehouse), new { variantId = inventory.ProductVariantId, warehouseId = inventory.WarehouseId }, response);
    }

    /// <summary>
    /// Update variant inventory (Requires Inventory.Update permission)
    /// </summary>
    [HttpPut("{id}")]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<ActionResult<VariantInventoryResponse>> UpdateVariantInventory(int id, [FromBody] UpdateVariantInventoryRequest request)
    {
        var inventory = await _context.VariantInventories
            .Include(vi => vi.Warehouse)
            .FirstOrDefaultAsync(vi => vi.Id == id);

        if (inventory == null)
        {
            return NotFound($"Variant inventory with ID {id} not found.");
        }

        var before = $"Quantity: {inventory.Quantity}, Unit: {inventory.Unit}, MinLevel: {inventory.MinimumStockLevel}, MaxLevel: {inventory.MaximumStockLevel}";

        inventory.Quantity = request.Quantity;
        inventory.Unit = request.Unit?.Trim();
        inventory.MinimumStockLevel = request.MinimumStockLevel;
        inventory.MaximumStockLevel = request.MaximumStockLevel;
        inventory.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        // Audit log
        var after = $"Quantity: {inventory.Quantity}, Unit: {inventory.Unit}, MinLevel: {inventory.MinimumStockLevel}, MaxLevel: {inventory.MaximumStockLevel}";
        await _auditService.LogAsync(
            entity: "VariantInventory",
            entityId: inventory.Id.ToString(),
            action: "UPDATE",
            actorUserId: GetCurrentUserId(),
            before: before,
            after: after
        );

        var response = new VariantInventoryResponse
        {
            Id = inventory.Id,
            ProductVariantId = inventory.ProductVariantId,
            WarehouseId = inventory.WarehouseId,
            WarehouseName = inventory.Warehouse.Name,
            Quantity = inventory.Quantity,
            POSQuantity = inventory.POSQuantity,
            Unit = inventory.Unit,
            MinimumStockLevel = inventory.MinimumStockLevel,
            MaximumStockLevel = inventory.MaximumStockLevel,
            CreatedAt = inventory.CreatedAt,
            UpdatedAt = inventory.UpdatedAt
        };

        return Ok(response);
    }

    /// <summary>
    /// Update variant inventory by variant and warehouse (Requires Inventory.Update permission)
    /// </summary>
    [HttpPut("variant/{variantId}/warehouse/{warehouseId}")]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<ActionResult<VariantInventoryResponse>> UpdateVariantInventoryByVariantAndWarehouse(int variantId, int warehouseId, [FromBody] UpdateVariantInventoryRequest request)
    {
        var inventory = await _context.VariantInventories
            .Include(vi => vi.Warehouse)
            .FirstOrDefaultAsync(vi => vi.ProductVariantId == variantId && vi.WarehouseId == warehouseId);

        var before = inventory != null ? $"Quantity: {inventory.Quantity}, Unit: {inventory.Unit}" : "No inventory record";

        if (inventory == null)
        {
            // Create new inventory record
            inventory = new VariantInventory
            {
                ProductVariantId = variantId,
                WarehouseId = warehouseId,
                Quantity = request.Quantity,
                Unit = request.Unit?.Trim(),
                MinimumStockLevel = request.MinimumStockLevel,
                MaximumStockLevel = request.MaximumStockLevel,
                CreatedAt = DateTime.UtcNow
            };
            _context.VariantInventories.Add(inventory);
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

        // Reload with warehouse
        await _context.Entry(inventory).Reference(vi => vi.Warehouse).LoadAsync();

        // Audit log
        var after = $"Quantity: {inventory.Quantity}, Unit: {inventory.Unit}, MinLevel: {inventory.MinimumStockLevel}, MaxLevel: {inventory.MaximumStockLevel}";
        await _auditService.LogAsync(
            entity: "VariantInventory",
            entityId: inventory.Id.ToString(),
            action: inventory.CreatedAt == inventory.UpdatedAt ? "CREATE" : "UPDATE",
            actorUserId: GetCurrentUserId(),
            before: before,
            after: after
        );

        var response = new VariantInventoryResponse
        {
            Id = inventory.Id,
            ProductVariantId = inventory.ProductVariantId,
            WarehouseId = inventory.WarehouseId,
            WarehouseName = inventory.Warehouse.Name,
            Quantity = inventory.Quantity,
            POSQuantity = inventory.POSQuantity,
            Unit = inventory.Unit,
            MinimumStockLevel = inventory.MinimumStockLevel,
            MaximumStockLevel = inventory.MaximumStockLevel,
            CreatedAt = inventory.CreatedAt,
            UpdatedAt = inventory.UpdatedAt
        };

        return Ok(response);
    }

    /// <summary>
    /// Delete variant inventory (Requires Inventory.Delete permission)
    /// </summary>
    [HttpDelete("{id}")]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<IActionResult> DeleteVariantInventory(int id)
    {
        var inventory = await _context.VariantInventories.FindAsync(id);
        if (inventory == null)
        {
            return NotFound($"Variant inventory with ID {id} not found.");
        }

        var before = $"VariantId: {inventory.ProductVariantId}, WarehouseId: {inventory.WarehouseId}, Quantity: {inventory.Quantity}";

        _context.VariantInventories.Remove(inventory);
        await _context.SaveChangesAsync();

        // Audit log
        await _auditService.LogAsync(
            entity: "VariantInventory",
            entityId: id.ToString(),
            action: "DELETE",
            actorUserId: GetCurrentUserId(),
            before: before,
            after: null
        );

        return NoContent();
    }
}



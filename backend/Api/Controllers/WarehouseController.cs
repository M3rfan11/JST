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
// [Authorize] // Temporarily removed for debugging
public class WarehouseController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IAuditService _auditService;

    public WarehouseController(ApplicationDbContext context, IAuditService auditService)
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
    /// Test endpoint to check if the controller is working
    /// </summary>
    [HttpGet("test")]
    public IActionResult Test()
    {
        Console.WriteLine("Test endpoint called");
        return Ok(new { message = "WarehouseController is working", timestamp = DateTime.UtcNow });
    }

    /// <summary>
    /// Get all active warehouses
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<WarehouseListResponse>>> GetWarehouses()
    {
        try
        {
            Console.WriteLine("GetWarehouses endpoint called");
            
            // First, try to get warehouses without projection
            var warehousesRaw = await _context.Warehouses
                .Where(w => w.IsActive)
                .ToListAsync();

            Console.WriteLine($"Found {warehousesRaw.Count} raw warehouses");

            // Then manually map to response
            var warehouses = warehousesRaw.Select(w => new WarehouseListResponse
            {
                Id = w.Id,
                Name = w.Name,
                Address = w.Address,
                City = w.City,
                ManagerName = w.ManagerName,
                IsActive = w.IsActive,
                ProductCount = 0, // Simplified for now
                TotalInventoryValue = 0 // Simplified for now
            }).OrderBy(w => w.Name).ToList();

            Console.WriteLine($"Mapped {warehouses.Count} warehouses");
            return Ok(warehouses);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in GetWarehouses: {ex.Message}");
            return StatusCode(500, new { message = "Internal server error", error = ex.Message });
        }
    }

    /// <summary>
    /// Get all warehouses including inactive ones (Admin only)
    /// </summary>
    [HttpGet("all")]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<ActionResult<IEnumerable<WarehouseResponse>>> GetAllWarehouses()
    {
        var warehouses = await _context.Warehouses
            .Include(w => w.ProductInventories)
                .ThenInclude(pi => pi.Product)
            .Select(w => new WarehouseResponse
            {
                Id = w.Id,
                Name = w.Name,
                Address = w.Address,
                City = w.City,
                PhoneNumber = w.PhoneNumber,
                ManagerName = w.ManagerName,
                IsActive = w.IsActive,
                CreatedAt = w.CreatedAt,
                UpdatedAt = w.UpdatedAt,
                ProductCount = w.ProductInventories.Count(pi => pi.Product.IsActive),
                TotalInventoryValue = w.ProductInventories
                    .Where(pi => pi.Product.IsActive)
                    .Sum(pi => pi.Quantity * pi.Product.Price)
            })
            .OrderBy(w => w.Name)
            .ToListAsync();

        return Ok(warehouses);
    }

    /// <summary>
    /// Get a specific warehouse by ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<WarehouseResponse>> GetWarehouse(int id)
    {
        var warehouse = await _context.Warehouses
            .Where(w => w.Id == id)
            .Include(w => w.ProductInventories)
                .ThenInclude(pi => pi.Product)
            .Select(w => new WarehouseResponse
            {
                Id = w.Id,
                Name = w.Name,
                Address = w.Address,
                City = w.City,
                PhoneNumber = w.PhoneNumber,
                ManagerName = w.ManagerName,
                IsActive = w.IsActive,
                CreatedAt = w.CreatedAt,
                UpdatedAt = w.UpdatedAt,
                ProductCount = w.ProductInventories.Count(pi => pi.Product.IsActive),
                TotalInventoryValue = w.ProductInventories
                    .Where(pi => pi.Product.IsActive)
                    .Sum(pi => pi.Quantity * pi.Product.Price)
            })
            .FirstOrDefaultAsync();

        if (warehouse == null)
        {
            return NotFound($"Warehouse with ID {id} not found.");
        }

        return Ok(warehouse);
    }

    /// <summary>
    /// Create a new warehouse (Admin only)
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<ActionResult<WarehouseResponse>> CreateWarehouse([FromBody] CreateWarehouseRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return BadRequest("Warehouse name is required.");
        }

        // Check if warehouse name already exists
        var existingWarehouse = await _context.Warehouses
            .FirstOrDefaultAsync(w => w.Name.ToLower() == request.Name.ToLower());

        if (existingWarehouse != null)
        {
            return BadRequest($"A warehouse with the name '{request.Name}' already exists.");
        }

        // Validate manager user if provided
        User? managerUser = null;
        if (request.ManagerUserId.HasValue)
        {
            managerUser = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == request.ManagerUserId.Value && u.IsActive);
            
            if (managerUser == null)
            {
                return BadRequest($"User with ID {request.ManagerUserId.Value} not found or inactive.");
            }
        }

        var warehouse = new Warehouse
        {
            Name = request.Name.Trim(),
            Address = request.Address?.Trim(),
            City = request.City?.Trim(),
            PhoneNumber = request.PhoneNumber?.Trim(),
            ManagerName = request.ManagerName?.Trim(),
            ManagerUserId = request.ManagerUserId,
            IsActive = request.IsActive,
            CreatedAt = DateTime.UtcNow
        };

        _context.Warehouses.Add(warehouse);
        await _context.SaveChangesAsync();

        // Audit log
        await _auditService.LogAsync(
            entity: "Warehouse",
            entityId: warehouse.Id.ToString(),
            action: "CREATE",
            actorUserId: GetCurrentUserId(),
            before: null,
            after: $"Name: {warehouse.Name}, Address: {warehouse.Address}, City: {warehouse.City}, Manager: {warehouse.ManagerName}"
        );

        var response = new WarehouseResponse
        {
            Id = warehouse.Id,
            Name = warehouse.Name,
            Address = warehouse.Address,
            City = warehouse.City,
            PhoneNumber = warehouse.PhoneNumber,
            ManagerName = warehouse.ManagerName,
            ManagerUserId = warehouse.ManagerUserId,
            ManagerEmail = managerUser?.Email,
            IsActive = warehouse.IsActive,
            CreatedAt = warehouse.CreatedAt,
            UpdatedAt = warehouse.UpdatedAt,
            ProductCount = 0,
            TotalInventoryValue = 0
        };

        return CreatedAtAction(nameof(GetWarehouse), new { id = warehouse.Id }, response);
    }

    /// <summary>
    /// Update a warehouse (Admin only)
    /// </summary>
    [HttpPut("{id}")]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<ActionResult<WarehouseResponse>> UpdateWarehouse(int id, [FromBody] UpdateWarehouseRequest request)
    {
        var warehouse = await _context.Warehouses.FindAsync(id);
        if (warehouse == null)
        {
            return NotFound($"Warehouse with ID {id} not found.");
        }

        var before = $"Name: {warehouse.Name}, Address: {warehouse.Address}, City: {warehouse.City}, Manager: {warehouse.ManagerName}";

        // Update fields if provided
        if (!string.IsNullOrWhiteSpace(request.Name))
        {
            // Check if new name already exists (excluding current warehouse)
            var existingWarehouse = await _context.Warehouses
                .FirstOrDefaultAsync(w => w.Name.ToLower() == request.Name.ToLower() && w.Id != id);

            if (existingWarehouse != null)
            {
                return BadRequest($"A warehouse with the name '{request.Name}' already exists.");
            }

            warehouse.Name = request.Name.Trim();
        }

        if (request.Address != null)
        {
            warehouse.Address = request.Address.Trim();
        }

        if (request.City != null)
        {
            warehouse.City = request.City.Trim();
        }

        if (request.PhoneNumber != null)
        {
            warehouse.PhoneNumber = request.PhoneNumber.Trim();
        }

        if (request.ManagerName != null)
        {
            warehouse.ManagerName = request.ManagerName.Trim();
        }

        if (request.IsActive.HasValue)
        {
            warehouse.IsActive = request.IsActive.Value;
        }

        warehouse.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        // Audit log
        var after = $"Name: {warehouse.Name}, Address: {warehouse.Address}, City: {warehouse.City}, Manager: {warehouse.ManagerName}";
        await _auditService.LogAsync(
            entity: "Warehouse",
            entityId: warehouse.Id.ToString(),
            action: "UPDATE",
            actorUserId: GetCurrentUserId(),
            before: before,
            after: after
        );

        var response = new WarehouseResponse
        {
            Id = warehouse.Id,
            Name = warehouse.Name,
            Address = warehouse.Address,
            City = warehouse.City,
            PhoneNumber = warehouse.PhoneNumber,
            ManagerName = warehouse.ManagerName,
            IsActive = warehouse.IsActive,
            CreatedAt = warehouse.CreatedAt,
            UpdatedAt = warehouse.UpdatedAt,
            ProductCount = await _context.ProductInventories.CountAsync(pi => pi.WarehouseId == warehouse.Id && pi.Product.IsActive),
            TotalInventoryValue = await _context.ProductInventories
                .Where(pi => pi.WarehouseId == warehouse.Id && pi.Product.IsActive)
                .SumAsync(pi => pi.Quantity * pi.Product.Price)
        };

        return Ok(response);
    }

    /// <summary>
    /// Soft delete a warehouse (Admin only)
    /// </summary>
    [HttpDelete("{id}")]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<IActionResult> DeleteWarehouse(int id)
    {
        var warehouse = await _context.Warehouses.FindAsync(id);
        if (warehouse == null)
        {
            return NotFound($"Warehouse with ID {id} not found.");
        }

        // Check if warehouse has active inventory
        var hasActiveInventory = await _context.ProductInventories
            .AnyAsync(pi => pi.WarehouseId == id && pi.Quantity > 0);

        if (hasActiveInventory)
        {
            return BadRequest("Cannot delete warehouse that has active inventory. Please transfer or remove all inventory first.");
        }

        var before = $"Name: {warehouse.Name}, Address: {warehouse.Address}, City: {warehouse.City}, Manager: {warehouse.ManagerName}";

        // Soft delete by setting IsActive to false
        warehouse.IsActive = false;
        warehouse.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        // Audit log
        var after = $"Name: {warehouse.Name}, Address: {warehouse.Address}, City: {warehouse.City}, Manager: {warehouse.ManagerName}, IsActive: {warehouse.IsActive}";
        await _auditService.LogAsync(
            entity: "Warehouse",
            entityId: warehouse.Id.ToString(),
            action: "DELETE",
            actorUserId: GetCurrentUserId(),
            before: before,
            after: after
        );

        return NoContent();
    }

    /// <summary>
    /// Get warehouse inventory details
    /// </summary>
    [HttpGet("{id}/inventory")]
    public async Task<ActionResult<IEnumerable<ProductInventoryResponse>>> GetWarehouseInventory(int id)
    {
        var warehouse = await _context.Warehouses.FindAsync(id);
        if (warehouse == null)
        {
            return NotFound($"Warehouse with ID {id} not found.");
        }

        var inventory = await _context.ProductInventories
            .Where(pi => pi.WarehouseId == id && pi.Product.IsActive)
            .Include(pi => pi.Product)
            .Select(pi => new ProductInventoryResponse
            {
                Id = pi.Id,
                WarehouseId = pi.WarehouseId,
                WarehouseName = warehouse.Name,
                Quantity = pi.Quantity,
                Unit = pi.Unit,
                MinimumStockLevel = pi.MinimumStockLevel,
                MaximumStockLevel = pi.MaximumStockLevel
            })
            .OrderBy(pi => pi.WarehouseName)
            .ToListAsync();

        return Ok(inventory);
    }

    /// <summary>
    /// Assign a manager to a warehouse
    /// </summary>
    [HttpPost("{id}/assign-manager")]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<ActionResult> AssignManager(int id, [FromBody] AssignManagerRequest request)
    {
        var warehouse = await _context.Warehouses
            .Include(w => w.ManagerUser)
            .FirstOrDefaultAsync(w => w.Id == id);

        if (warehouse == null)
        {
            return NotFound($"Warehouse with ID {id} not found.");
        }

        // Validate manager user
        var managerUser = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == request.ManagerUserId && u.IsActive);

        if (managerUser == null)
        {
            return BadRequest($"User with ID {request.ManagerUserId} not found or inactive.");
        }

        // Check if the manager is already managing another warehouse
        var existingManagerWarehouse = await _context.Warehouses
            .FirstOrDefaultAsync(w => w.ManagerUserId == request.ManagerUserId && w.IsActive && w.Id != id);
        
        if (existingManagerWarehouse != null)
        {
            return BadRequest($"User is already managing warehouse '{existingManagerWarehouse.Name}'. A manager can only manage one warehouse at a time.");
        }

        var oldManagerEmail = warehouse.ManagerUser?.Email;
        
        // Remove old manager from store assignment if they exist
        if (warehouse.ManagerUserId.HasValue)
        {
            var oldManager = await _context.Users.FindAsync(warehouse.ManagerUserId.Value);
            if (oldManager != null)
            {
                // Store assignment removed
            }
        }

        warehouse.ManagerUserId = request.ManagerUserId;
        warehouse.ManagerName = managerUser.FullName; // Update manager name from user
        warehouse.UpdatedAt = DateTime.UtcNow;

        // Update new manager's store assignment
        // Store assignment removed

        await _context.SaveChangesAsync();

        // Audit log
        await _auditService.LogAsync(
            entity: "Warehouse",
            entityId: warehouse.Id.ToString(),
            action: "ASSIGN_MANAGER",
            actorUserId: GetCurrentUserId(),
            before: $"Manager: {oldManagerEmail}",
            after: $"Manager: {managerUser.Email}"
        );

        return Ok(new { message = $"Manager {managerUser.FullName} assigned to warehouse {warehouse.Name}" });
    }

    /// <summary>
    /// Remove manager from a warehouse
    /// </summary>
    [HttpDelete("{id}/remove-manager")]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<ActionResult> RemoveManager(int id)
    {
        var warehouse = await _context.Warehouses
            .Include(w => w.ManagerUser)
            .FirstOrDefaultAsync(w => w.Id == id);

        if (warehouse == null)
        {
            return NotFound($"Warehouse with ID {id} not found.");
        }

        var oldManagerEmail = warehouse.ManagerUser?.Email;
        
        // Remove manager from store assignment
        if (warehouse.ManagerUserId.HasValue)
        {
            var oldManager = await _context.Users.FindAsync(warehouse.ManagerUserId.Value);
            if (oldManager != null)
            {
                // Store assignment removed
            }
        }
        
        warehouse.ManagerUserId = null;
        warehouse.ManagerName = null;
        warehouse.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        // Audit log
        await _auditService.LogAsync(
            entity: "Warehouse",
            entityId: warehouse.Id.ToString(),
            action: "REMOVE_MANAGER",
            actorUserId: GetCurrentUserId(),
            before: $"Manager: {oldManagerEmail}",
            after: "Manager: None"
        );

        return Ok(new { message = $"Manager removed from warehouse {warehouse.Name}" });
    }
}

public class AssignManagerRequest
{
    public int ManagerUserId { get; set; }
}

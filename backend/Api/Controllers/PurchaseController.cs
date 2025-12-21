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
public class PurchaseController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IAuditService _auditService;
    private readonly IRevenueTrackingService _revenueTrackingService;

    public PurchaseController(ApplicationDbContext context, IAuditService auditService, IRevenueTrackingService revenueTrackingService)
    {
        _context = context;
        _auditService = auditService;
        _revenueTrackingService = revenueTrackingService;
    }

    private int GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier");
        return userIdClaim != null ? int.Parse(userIdClaim.Value) : 0;
    }

    private string GenerateOrderNumber()
    {
        var today = DateTime.UtcNow;
        var prefix = $"PO{today:yyyyMMdd}";
        var count = _context.PurchaseOrders.Count(po => po.OrderNumber.StartsWith(prefix)) + 1;
        return $"{prefix}{count:D4}";
    }

    /// <summary>
    /// Get all purchase orders
    /// </summary>
    [HttpGet]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<ActionResult<IEnumerable<PurchaseOrderListResponse>>> GetPurchaseOrders()
    {
        var orders = await _context.PurchaseOrders
            .Include(po => po.CreatedByUser)
            .Include(po => po.ApprovedByUser)
            .OrderByDescending(po => po.CreatedAt)
            .Select(po => new PurchaseOrderListResponse
            {
                Id = po.Id,
                OrderNumber = po.OrderNumber,
                SupplierName = po.SupplierName,
                OrderDate = po.OrderDate,
                ExpectedDeliveryDate = po.ExpectedDeliveryDate,
                ActualDeliveryDate = po.ActualDeliveryDate,
                TotalAmount = po.TotalAmount,
                Status = po.Status,
                CreatedByUserName = po.CreatedByUser.FullName,
                ApprovedByUserName = po.ApprovedByUser != null ? po.ApprovedByUser.FullName : null
            })
            .ToListAsync();

        return Ok(orders);
    }

    /// <summary>
    /// Get a specific purchase order by ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<PurchaseOrderResponse>> GetPurchaseOrder(int id)
    {
        var order = await _context.PurchaseOrders
            .Include(po => po.CreatedByUser)
            .Include(po => po.ApprovedByUser)
            .Include(po => po.PurchaseItems)
                .ThenInclude(pi => pi.Product)
            .Include(po => po.PurchaseItems)
                .ThenInclude(pi => pi.Warehouse)
            .FirstOrDefaultAsync(po => po.Id == id);

        if (order == null)
        {
            return NotFound();
        }

        var response = new PurchaseOrderResponse
        {
            Id = order.Id,
            OrderNumber = order.OrderNumber,
            SupplierName = order.SupplierName,
            SupplierAddress = order.SupplierAddress,
            SupplierPhone = order.SupplierPhone,
            SupplierEmail = order.SupplierEmail,
            OrderDate = order.OrderDate,
            ExpectedDeliveryDate = order.ExpectedDeliveryDate,
            ActualDeliveryDate = order.ActualDeliveryDate,
            TotalAmount = order.TotalAmount,
            Status = order.Status,
            Notes = order.Notes,
            CreatedByUserId = order.CreatedByUserId,
            CreatedByUserName = order.CreatedByUser.FullName,
            ApprovedByUserId = order.ApprovedByUserId,
            ApprovedByUserName = order.ApprovedByUser?.FullName,
            CreatedAt = order.CreatedAt,
            UpdatedAt = order.UpdatedAt,
            Items = order.PurchaseItems.Select(pi => new PurchaseItemResponse
            {
                Id = pi.Id,
                ProductId = pi.ProductId,
                ProductName = pi.Product.Name,
                ProductSKU = pi.Product.SKU ?? "",
                WarehouseId = pi.WarehouseId,
                WarehouseName = pi.Warehouse.Name,
                Quantity = pi.Quantity,
                UnitPrice = pi.UnitPrice,
                TotalPrice = pi.TotalPrice,
                Unit = pi.Unit,
                Notes = pi.Notes
            }).ToList()
        };

        return Ok(response);
    }

    /// <summary>
    /// Create a new purchase order
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<ActionResult<PurchaseOrderResponse>> CreatePurchaseOrder(CreatePurchaseOrderRequest request)
    {
        if (request.Items == null || !request.Items.Any())
        {
            return BadRequest("Purchase order must contain at least one item.");
        }

        var orderNumber = GenerateOrderNumber();
        var currentUserId = GetCurrentUserId();
        var currentUser = await _context.Users.FindAsync(currentUserId);

        // Get user roles to determine purchasing permissions
        var userRoles = await _context.UserRoles
            .Where(ur => ur.UserId == currentUserId)
            .Include(ur => ur.Role)
            .Select(ur => ur.Role.Name)
            .ToListAsync();

        // Only SuperAdmin can create purchase orders - no store restrictions
        if (!userRoles.Contains("SuperAdmin"))
        {
            return StatusCode(403, new { Message = "You don't have permission to create purchase orders" });
        }
        // SuperAdmin has no restrictions (can purchase for any store)

        var order = new PurchaseOrder
        {
            OrderNumber = orderNumber,
            SupplierName = request.SupplierName,
            SupplierAddress = request.SupplierAddress,
            SupplierPhone = request.SupplierPhone,
            SupplierEmail = request.SupplierEmail,
            OrderDate = DateTime.UtcNow,
            ExpectedDeliveryDate = request.ExpectedDeliveryDate,
            Notes = request.Notes,
            CreatedByUserId = currentUserId,
            CreatedAt = DateTime.UtcNow,
            Status = "Pending"
        };

        decimal totalAmount = 0;

        foreach (var itemRequest in request.Items)
        {
            // Validate product exists
            var product = await _context.Products.FindAsync(itemRequest.ProductId);
            if (product == null)
            {
                return BadRequest($"Product with ID {itemRequest.ProductId} not found.");
            }

            // Validate warehouse exists
            var warehouse = await _context.Warehouses.FindAsync(itemRequest.WarehouseId);
            if (warehouse == null)
            {
                return BadRequest($"Warehouse with ID {itemRequest.WarehouseId} not found.");
            }

            var itemTotalPrice = itemRequest.Quantity * itemRequest.UnitPrice;
            totalAmount += itemTotalPrice;

            var item = new PurchaseItem
            {
                ProductId = itemRequest.ProductId,
                WarehouseId = itemRequest.WarehouseId,
                Quantity = itemRequest.Quantity,
                UnitPrice = itemRequest.UnitPrice,
                TotalPrice = itemTotalPrice,
                Unit = itemRequest.Unit,
                Notes = itemRequest.Notes
            };

            order.PurchaseItems.Add(item);
        }

        order.TotalAmount = totalAmount;

        _context.PurchaseOrders.Add(order);
        await _context.SaveChangesAsync();

        await _auditService.LogAsync("PurchaseOrder", order.Id.ToString(), "Created", $"Created purchase order {orderNumber}", null, currentUserId);

        // Return the created order directly instead of using CreatedAtAction
        var response = new PurchaseOrderResponse
        {
            Id = order.Id,
            OrderNumber = order.OrderNumber,
            SupplierName = order.SupplierName,
            SupplierAddress = order.SupplierAddress,
            SupplierPhone = order.SupplierPhone,
            SupplierEmail = order.SupplierEmail,
            OrderDate = order.OrderDate,
            ExpectedDeliveryDate = order.ExpectedDeliveryDate,
            ActualDeliveryDate = order.ActualDeliveryDate,
            TotalAmount = order.TotalAmount,
            Status = order.Status,
            Notes = order.Notes,
            CreatedByUserId = order.CreatedByUserId,
            CreatedByUserName = order.CreatedByUser?.FullName ?? "Unknown",
            ApprovedByUserId = order.ApprovedByUserId,
            ApprovedByUserName = order.ApprovedByUser?.FullName,
            CreatedAt = order.CreatedAt,
            UpdatedAt = order.UpdatedAt,
            Items = order.PurchaseItems.Select(pi => new PurchaseItemResponse
            {
                Id = pi.Id,
                ProductId = pi.ProductId,
                ProductName = pi.Product?.Name ?? "Unknown",
                ProductSKU = pi.Product?.SKU ?? "",
                WarehouseId = pi.WarehouseId,
                WarehouseName = pi.Warehouse?.Name ?? "Unknown",
                Quantity = pi.Quantity,
                UnitPrice = pi.UnitPrice,
                TotalPrice = pi.TotalPrice,
                Unit = pi.Unit,
                Notes = pi.Notes
            }).ToList()
        };

        return Ok(response);
    }

    /// <summary>
    /// Update a purchase order
    /// </summary>
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdatePurchaseOrder(int id, UpdatePurchaseOrderRequest request)
    {
        var order = await _context.PurchaseOrders.FindAsync(id);
        if (order == null)
        {
            return NotFound();
        }

        if (order.Status == "Received" || order.Status == "Cancelled")
        {
            return BadRequest("Cannot update a purchase order that has been received or cancelled.");
        }

        var currentUserId = GetCurrentUserId();

        order.SupplierName = request.SupplierName ?? order.SupplierName;
        order.SupplierAddress = request.SupplierAddress ?? order.SupplierAddress;
        order.SupplierPhone = request.SupplierPhone ?? order.SupplierPhone;
        order.SupplierEmail = request.SupplierEmail ?? order.SupplierEmail;
        order.ExpectedDeliveryDate = request.ExpectedDeliveryDate ?? order.ExpectedDeliveryDate;
        order.ActualDeliveryDate = request.ActualDeliveryDate ?? order.ActualDeliveryDate;
        order.Status = request.Status ?? order.Status;
        order.Notes = request.Notes ?? order.Notes;
        order.UpdatedAt = DateTime.UtcNow;

        _context.Entry(order).State = EntityState.Modified;
        await _context.SaveChangesAsync();

        await _auditService.LogAsync("PurchaseOrder", order.Id.ToString(), "Updated", $"Updated purchase order {order.OrderNumber}", null, currentUserId);

        return NoContent();
    }

    /// <summary>
    /// Approve a purchase order
    /// </summary>
    [HttpPost("{id}/approve")]
    public async Task<IActionResult> ApprovePurchaseOrder(int id, ApprovePurchaseOrderRequest request)
    {
        var order = await _context.PurchaseOrders.FindAsync(id);
        if (order == null)
        {
            return NotFound();
        }

        if (order.Status != "Pending")
        {
            return BadRequest("Only pending purchase orders can be approved.");
        }

        var currentUserId = GetCurrentUserId();
        order.Status = "Approved";
        order.ApprovedByUserId = currentUserId;
        order.UpdatedAt = DateTime.UtcNow;

        if (!string.IsNullOrEmpty(request.Notes))
        {
            order.Notes = string.IsNullOrEmpty(order.Notes) ? request.Notes : $"{order.Notes}\nApproval Notes: {request.Notes}";
        }

        _context.Entry(order).State = EntityState.Modified;
        await _context.SaveChangesAsync();

        await _auditService.LogAsync("PurchaseOrder", order.Id.ToString(), "Approved", $"Approved purchase order {order.OrderNumber}", null, currentUserId);

        return NoContent();
    }

    /// <summary>
    /// Receive a purchase order (mark as received and update inventory)
    /// </summary>
    [HttpPost("{id}/receive")]
    public async Task<IActionResult> ReceivePurchaseOrder(int id, ReceivePurchaseOrderRequest request)
    {
        var order = await _context.PurchaseOrders
            .Include(po => po.PurchaseItems)
            .FirstOrDefaultAsync(po => po.Id == id);

        if (order == null)
        {
            return NotFound();
        }

        if (order.Status != "Approved")
        {
            return BadRequest("Only approved purchase orders can be received.");
        }

        var currentUserId = GetCurrentUserId();

        // Update order status
        order.Status = "Received";
        order.ActualDeliveryDate = request.ActualDeliveryDate ?? DateTime.UtcNow;
        order.UpdatedAt = DateTime.UtcNow;

        if (!string.IsNullOrEmpty(request.Notes))
        {
            order.Notes = string.IsNullOrEmpty(order.Notes) ? request.Notes : $"{order.Notes}\nReceipt Notes: {request.Notes}";
        }

        // Update inventory for each item
        foreach (var item in order.PurchaseItems)
        {
            var inventory = await _context.ProductInventories
                .FirstOrDefaultAsync(pi => pi.ProductId == item.ProductId && pi.WarehouseId == item.WarehouseId);

            if (inventory == null)
            {
                // Create new inventory record
                inventory = new ProductInventory
                {
                    ProductId = item.ProductId,
                    WarehouseId = item.WarehouseId,
                    Quantity = item.Quantity,
                    Unit = item.Unit
                };
                _context.ProductInventories.Add(inventory);
            }
            else
            {
                // Update existing inventory
                inventory.Quantity += item.Quantity;
                inventory.UpdatedAt = DateTime.UtcNow;
            }
        }

        await _context.SaveChangesAsync();

        await _auditService.LogAsync("PurchaseOrder", order.Id.ToString(), "Received", $"Received purchase order {order.OrderNumber} and updated inventory", null, currentUserId);

        // Update cost tracking
        await _revenueTrackingService.UpdateCostsAfterPurchaseAsync(order.Id, order.TotalAmount);

        return NoContent();
    }

    /// <summary>
    /// Cancel a purchase order
    /// </summary>
    [HttpPost("{id}/cancel")]
    public async Task<IActionResult> CancelPurchaseOrder(int id)
    {
        var order = await _context.PurchaseOrders.FindAsync(id);
        if (order == null)
        {
            return NotFound();
        }

        if (order.Status == "Received" || order.Status == "Cancelled")
        {
            return BadRequest("Cannot cancel a purchase order that has been received or is already cancelled.");
        }

        var currentUserId = GetCurrentUserId();
        order.Status = "Cancelled";
        order.UpdatedAt = DateTime.UtcNow;

        _context.Entry(order).State = EntityState.Modified;
        await _context.SaveChangesAsync();

        await _auditService.LogAsync("PurchaseOrder", order.Id.ToString(), "Cancelled", $"Cancelled purchase order {order.OrderNumber}", null, currentUserId);

        return NoContent();
    }

    /// <summary>
    /// Delete a purchase order (only if it's pending)
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeletePurchaseOrder(int id)
    {
        var order = await _context.PurchaseOrders.FindAsync(id);
        if (order == null)
        {
            return NotFound();
        }

        if (order.Status != "Pending")
        {
            return BadRequest("Only pending purchase orders can be deleted.");
        }

        var currentUserId = GetCurrentUserId();

        _context.PurchaseOrders.Remove(order);
        await _context.SaveChangesAsync();

        await _auditService.LogAsync("PurchaseOrder", order.Id.ToString(), "Deleted", $"Deleted purchase order {order.OrderNumber}", null, currentUserId);

        return NoContent();
    }

    /// <summary>
    /// Get purchase orders by status
    /// </summary>
    [HttpGet("status/{status}")]
    public async Task<ActionResult<IEnumerable<PurchaseOrderListResponse>>> GetPurchaseOrdersByStatus(string status)
    {
        var orders = await _context.PurchaseOrders
            .Where(po => po.Status == status)
            .Include(po => po.CreatedByUser)
            .Include(po => po.ApprovedByUser)
            .OrderByDescending(po => po.CreatedAt)
            .Select(po => new PurchaseOrderListResponse
            {
                Id = po.Id,
                OrderNumber = po.OrderNumber,
                SupplierName = po.SupplierName,
                OrderDate = po.OrderDate,
                ExpectedDeliveryDate = po.ExpectedDeliveryDate,
                ActualDeliveryDate = po.ActualDeliveryDate,
                TotalAmount = po.TotalAmount,
                Status = po.Status,
                CreatedByUserName = po.CreatedByUser.FullName,
                ApprovedByUserName = po.ApprovedByUser != null ? po.ApprovedByUser.FullName : null
            })
            .ToListAsync();

        return Ok(orders);
    }
}

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
public class ProductRequestController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IAuditService _auditService;

    public ProductRequestController(ApplicationDbContext context, IAuditService auditService)
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
    /// Get all product requests
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<ProductRequestListResponse>>> GetProductRequests()
    {
        var currentUserId = GetCurrentUserId();
        var currentUser = await _context.Users.FindAsync(currentUserId);

        // Get user roles to determine filtering
        var userRoles = await _context.UserRoles
            .Where(ur => ur.UserId == currentUserId)
            .Include(ur => ur.Role)
            .Select(ur => ur.Role.Name)
            .ToListAsync();

        IQueryable<ProductRequest> query = _context.ProductRequests
            .Include(pr => pr.RequestedByUser)
            .Include(pr => pr.ApprovedByUser)
            .Include(pr => pr.Warehouse)
            .Include(pr => pr.ProductRequestItems);

        // Apply store-specific filtering based on user role
        // Only SuperAdmin can see all requests - no store filtering
        if (!userRoles.Contains("SuperAdmin"))
        {
            return StatusCode(403, new { Message = "You don't have permission to view product requests" });
        }

        var requests = await query
            .OrderByDescending(pr => pr.RequestDate)
            .Select(pr => new ProductRequestListResponse
            {
                Id = pr.Id,
                RequestedByUserName = pr.RequestedByUser.FullName,
                RequestDate = pr.RequestDate,
                Status = pr.Status,
                WarehouseName = pr.Warehouse.Name,
                ApprovedByUserName = pr.ApprovedByUser != null ? pr.ApprovedByUser.FullName : null,
                ApprovedAt = pr.ApprovedAt,
                ItemCount = pr.ProductRequestItems.Count,
                TotalQuantityRequested = pr.ProductRequestItems.Sum(item => item.QuantityRequested)
            })
            .ToListAsync();

        return Ok(requests);
    }

    /// <summary>
    /// Get a specific product request by ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<ProductRequestResponse>> GetProductRequest(int id)
    {
        var request = await _context.ProductRequests
            .Include(pr => pr.RequestedByUser)
            .Include(pr => pr.ApprovedByUser)
            .Include(pr => pr.Warehouse)
            .Include(pr => pr.ProductRequestItems)
                .ThenInclude(item => item.Product)
            .FirstOrDefaultAsync(pr => pr.Id == id);

        if (request == null)
        {
            return NotFound();
        }

        var response = new ProductRequestResponse
        {
            Id = request.Id,
            RequestedByUserId = request.RequestedByUserId,
            RequestedByUserName = request.RequestedByUser.FullName,
            RequestDate = request.RequestDate,
            Status = request.Status,
            WarehouseId = request.WarehouseId,
            WarehouseName = request.Warehouse.Name,
            Notes = request.Notes,
            ApprovedByUserId = request.ApprovedByUserId,
            ApprovedByUserName = request.ApprovedByUser?.FullName,
            ApprovedAt = request.ApprovedAt,
            RejectedAt = request.RejectedAt,
            RejectionReason = request.RejectionReason,
            CreatedAt = request.CreatedAt,
            UpdatedAt = request.UpdatedAt,
            Items = request.ProductRequestItems.Select(item => new ProductRequestItemResponse
            {
                Id = item.Id,
                ProductId = item.ProductId,
                ProductName = item.Product.Name,
                ProductSKU = item.Product.SKU ?? "",
                QuantityRequested = item.QuantityRequested,
                QuantityApproved = item.QuantityApproved,
                QuantityReceived = item.QuantityReceived,
                Unit = item.Unit,
                Notes = item.Notes,
                IsFullyApproved = item.QuantityApproved >= item.QuantityRequested,
                IsFullyReceived = item.QuantityReceived >= item.QuantityApproved
            }).ToList(),
            TotalItems = request.ProductRequestItems.Count,
            TotalQuantityRequested = request.ProductRequestItems.Sum(item => item.QuantityRequested)
        };

        return Ok(response);
    }

    /// <summary>
    /// Create a new product request
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<ProductRequestResponse>> CreateProductRequest(CreateProductRequestRequest request)
    {
        if (request.Items == null || !request.Items.Any())
        {
            return BadRequest("Product request must contain at least one item.");
        }

        var currentUserId = GetCurrentUserId();
        var currentUser = await _context.Users.FindAsync(currentUserId);

        // Get user roles to determine store restrictions
        var userRoles = await _context.UserRoles
            .Where(ur => ur.UserId == currentUserId)
            .Include(ur => ur.Role)
            .Select(ur => ur.Role.Name)
            .ToListAsync();

        // Only SuperAdmin can create product requests - no store restrictions
        if (!userRoles.Contains("SuperAdmin"))
        {
            return StatusCode(403, new { Message = "You don't have permission to create product requests" });
        }

        // Validate warehouse exists
        var warehouse = await _context.Warehouses.FindAsync(request.WarehouseId);
        if (warehouse == null)
        {
            return BadRequest($"Warehouse with ID {request.WarehouseId} not found.");
        }

        var productRequest = new ProductRequest
        {
            RequestedByUserId = currentUserId,
            RequestDate = DateTime.UtcNow,
            Status = "Pending",
            WarehouseId = request.WarehouseId,
            Notes = request.Notes
        };

        // Validate and add request items
        foreach (var itemRequest in request.Items)
        {
            // Validate product exists
            var product = await _context.Products.FindAsync(itemRequest.ProductId);
            if (product == null)
            {
                return BadRequest($"Product with ID {itemRequest.ProductId} not found.");
            }

            var item = new ProductRequestItem
            {
                ProductId = itemRequest.ProductId,
                QuantityRequested = itemRequest.QuantityRequested,
                Unit = itemRequest.Unit,
                Notes = itemRequest.Notes
            };

            productRequest.ProductRequestItems.Add(item);
        }

        _context.ProductRequests.Add(productRequest);
        await _context.SaveChangesAsync();

        await _auditService.LogAsync("ProductRequest", productRequest.Id.ToString(), "Created", $"Created product request for {productRequest.ProductRequestItems.Count} items", null, currentUserId);

        return CreatedAtAction(nameof(GetProductRequest), new { id = productRequest.Id }, await GetProductRequest(productRequest.Id));
    }

    /// <summary>
    /// Update a product request (only if pending)
    /// </summary>
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateProductRequest(int id, UpdateProductRequestRequest request)
    {
        var productRequest = await _context.ProductRequests.FindAsync(id);
        if (productRequest == null)
        {
            return NotFound();
        }

        if (productRequest.Status != "Pending")
        {
            return BadRequest("Only pending product requests can be updated.");
        }

        var currentUserId = GetCurrentUserId();

        // Validate warehouse if provided
        if (request.WarehouseId.HasValue)
        {
            var warehouse = await _context.Warehouses.FindAsync(request.WarehouseId.Value);
            if (warehouse == null)
            {
                return BadRequest($"Warehouse with ID {request.WarehouseId.Value} not found.");
            }
            productRequest.WarehouseId = request.WarehouseId.Value;
        }

        productRequest.Notes = request.Notes ?? productRequest.Notes;
        productRequest.UpdatedAt = DateTime.UtcNow;

        _context.Entry(productRequest).State = EntityState.Modified;
        await _context.SaveChangesAsync();

        await _auditService.LogAsync("ProductRequest", productRequest.Id.ToString(), "Updated", $"Updated product request", null, currentUserId);

        return NoContent();
    }

    /// <summary>
    /// Approve a product request
    /// </summary>
    [HttpPost("{id}/approve")]
    public async Task<IActionResult> ApproveProductRequest(int id, ApproveProductRequestRequest request)
    {
        var productRequest = await _context.ProductRequests
            .Include(pr => pr.ProductRequestItems)
            .FirstOrDefaultAsync(pr => pr.Id == id);

        if (productRequest == null)
        {
            return NotFound();
        }

        if (productRequest.Status != "Pending")
        {
            return BadRequest("Only pending product requests can be approved.");
        }

        var currentUserId = GetCurrentUserId();

        // Update request status
        productRequest.Status = "Approved";
        productRequest.ApprovedByUserId = currentUserId;
        productRequest.ApprovedAt = DateTime.UtcNow;
        productRequest.UpdatedAt = DateTime.UtcNow;

        if (!string.IsNullOrEmpty(request.Notes))
        {
            productRequest.Notes = string.IsNullOrEmpty(productRequest.Notes) ? request.Notes : $"{productRequest.Notes}\nApproval Notes: {request.Notes}";
        }

        // Update individual items with approved quantities and transfer inventory
        foreach (var itemApproval in request.Items)
        {
            var item = productRequest.ProductRequestItems.FirstOrDefault(i => i.Id == itemApproval.ItemId);
            if (item != null)
            {
                var approvedQuantity = itemApproval.QuantityApproved ?? item.QuantityRequested;
                item.QuantityApproved = approvedQuantity;
                item.QuantityReceived = approvedQuantity; // Auto-receive approved items
                item.UpdatedAt = DateTime.UtcNow;

                if (!string.IsNullOrEmpty(itemApproval.Notes))
                {
                    item.Notes = string.IsNullOrEmpty(item.Notes) ? itemApproval.Notes : $"{item.Notes}\nApproval: {itemApproval.Notes}";
                }

                // Update inventory - items are now available in the warehouse
                var inventory = await _context.ProductInventories
                    .FirstOrDefaultAsync(pi => pi.ProductId == item.ProductId && pi.WarehouseId == productRequest.WarehouseId);

                if (inventory == null)
                {
                    return BadRequest($"Product {item.ProductId} not found in store inventory.");
                }

                // Check if store has enough inventory
                if (inventory.Quantity < approvedQuantity)
                {
                    return BadRequest($"Insufficient store inventory for product {item.ProductId}. Available: {inventory.Quantity}, Requested: {approvedQuantity}");
                }

                // Inventory remains in the warehouse (no POS transfer needed)
                inventory.UpdatedAt = DateTime.UtcNow;
            }
        }

        // Mark request as completed since all items are automatically received
        productRequest.Status = "Completed";
        productRequest.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        await _auditService.LogAsync("ProductRequest", productRequest.Id.ToString(), "Approved", $"Approved product request with {request.Items.Count} items", null, currentUserId);

        return NoContent();
    }

    /// <summary>
    /// Reject a product request
    /// </summary>
    [HttpPost("{id}/reject")]
    public async Task<IActionResult> RejectProductRequest(int id, RejectProductRequestRequest request)
    {
        var productRequest = await _context.ProductRequests.FindAsync(id);
        if (productRequest == null)
        {
            return NotFound();
        }

        if (productRequest.Status != "Pending")
        {
            return BadRequest("Only pending product requests can be rejected.");
        }

        var currentUserId = GetCurrentUserId();

        productRequest.Status = "Rejected";
        productRequest.RejectedAt = DateTime.UtcNow;
        productRequest.RejectionReason = request.RejectionReason;
        productRequest.UpdatedAt = DateTime.UtcNow;

        if (!string.IsNullOrEmpty(request.Notes))
        {
            productRequest.Notes = string.IsNullOrEmpty(productRequest.Notes) ? request.Notes : $"{productRequest.Notes}\nRejection Notes: {request.Notes}";
        }

        _context.Entry(productRequest).State = EntityState.Modified;
        await _context.SaveChangesAsync();

        await _auditService.LogAsync("ProductRequest", productRequest.Id.ToString(), "Rejected", $"Rejected product request: {request.RejectionReason}", null, currentUserId);

        return NoContent();
    }

    /// <summary>
    /// Mark items as received (updates inventory)
    /// </summary>
    [HttpPost("{id}/receive")]
    public async Task<IActionResult> ReceiveProductRequest(int id, List<ReceiveProductRequestItemRequest> request)
    {
        var productRequest = await _context.ProductRequests
            .Include(pr => pr.ProductRequestItems)
                .ThenInclude(item => item.Product)
            .FirstOrDefaultAsync(pr => pr.Id == id);

        if (productRequest == null)
        {
            return NotFound();
        }

        if (productRequest.Status != "Approved")
        {
            return BadRequest("Only approved product requests can be received.");
        }

        var currentUserId = GetCurrentUserId();

        // Process each received item
        foreach (var receivedItem in request)
        {
            var item = productRequest.ProductRequestItems.FirstOrDefault(i => i.Id == receivedItem.ItemId);
            if (item != null)
            {
                // Update received quantity
                item.QuantityReceived = (item.QuantityReceived ?? 0) + receivedItem.QuantityReceived;
                item.UpdatedAt = DateTime.UtcNow;

                if (!string.IsNullOrEmpty(receivedItem.Notes))
                {
                    item.Notes = string.IsNullOrEmpty(item.Notes) ? receivedItem.Notes : $"{item.Notes}\nReceived: {receivedItem.Notes}";
                }

                // Update inventory
                var inventory = await _context.ProductInventories
                    .FirstOrDefaultAsync(pi => pi.ProductId == item.ProductId && pi.WarehouseId == productRequest.WarehouseId);

                if (inventory == null)
                {
                    return BadRequest($"Product {item.ProductId} not found in store inventory. Cannot transfer items that don't exist in store.");
                }
                else
                {
                    // Check if store has enough inventory to transfer
                    if (inventory.Quantity < receivedItem.QuantityReceived)
                    {
                        return BadRequest($"Insufficient store inventory for product {item.ProductId}. Available: {inventory.Quantity}, Requested: {receivedItem.QuantityReceived}");
                    }

                    // Inventory is now available in the warehouse
                    inventory.UpdatedAt = DateTime.UtcNow;
                }
            }
        }

        // Check if all items are fully received
        var allItemsReceived = productRequest.ProductRequestItems.All(item => 
            item.QuantityReceived >= item.QuantityApproved);

        if (allItemsReceived)
        {
            productRequest.Status = "Completed";
            productRequest.UpdatedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();

        await _auditService.LogAsync("ProductRequest", productRequest.Id.ToString(), "Received", $"Received items for product request", null, currentUserId);

        return NoContent();
    }

    /// <summary>
    /// Get product requests by status
    /// </summary>
    [HttpGet("status/{status}")]
    public async Task<ActionResult<IEnumerable<ProductRequestListResponse>>> GetProductRequestsByStatus(string status)
    {
        var requests = await _context.ProductRequests
            .Where(pr => pr.Status == status)
            .Include(pr => pr.RequestedByUser)
            .Include(pr => pr.ApprovedByUser)
            .Include(pr => pr.Warehouse)
            .Include(pr => pr.ProductRequestItems)
            .OrderByDescending(pr => pr.RequestDate)
            .Select(pr => new ProductRequestListResponse
            {
                Id = pr.Id,
                RequestedByUserName = pr.RequestedByUser.FullName,
                RequestDate = pr.RequestDate,
                Status = pr.Status,
                WarehouseName = pr.Warehouse.Name,
                ApprovedByUserName = pr.ApprovedByUser != null ? pr.ApprovedByUser.FullName : null,
                ApprovedAt = pr.ApprovedAt,
                ItemCount = pr.ProductRequestItems.Count,
                TotalQuantityRequested = pr.ProductRequestItems.Sum(item => item.QuantityRequested)
            })
            .ToListAsync();

        return Ok(requests);
    }

    /// <summary>
    /// Get product requests by user
    /// </summary>
    [HttpGet("user/{userId}")]
    public async Task<ActionResult<IEnumerable<ProductRequestListResponse>>> GetProductRequestsByUser(int userId)
    {
        var requests = await _context.ProductRequests
            .Where(pr => pr.RequestedByUserId == userId)
            .Include(pr => pr.RequestedByUser)
            .Include(pr => pr.ApprovedByUser)
            .Include(pr => pr.Warehouse)
            .Include(pr => pr.ProductRequestItems)
            .OrderByDescending(pr => pr.RequestDate)
            .Select(pr => new ProductRequestListResponse
            {
                Id = pr.Id,
                RequestedByUserName = pr.RequestedByUser.FullName,
                RequestDate = pr.RequestDate,
                Status = pr.Status,
                WarehouseName = pr.Warehouse.Name,
                ApprovedByUserName = pr.ApprovedByUser != null ? pr.ApprovedByUser.FullName : null,
                ApprovedAt = pr.ApprovedAt,
                ItemCount = pr.ProductRequestItems.Count,
                TotalQuantityRequested = pr.ProductRequestItems.Sum(item => item.QuantityRequested)
            })
            .ToListAsync();

        return Ok(requests);
    }

    /// <summary>
    /// Get product request report
    /// </summary>
    [HttpGet("report")]
    public async Task<ActionResult<ProductRequestReportResponse>> GetProductRequestReport(DateTime? fromDate = null, DateTime? toDate = null)
    {
        var startDate = fromDate ?? DateTime.UtcNow.AddDays(-30);
        var endDate = toDate ?? DateTime.UtcNow;

        var requests = await _context.ProductRequests
            .Where(pr => pr.RequestDate >= startDate && pr.RequestDate <= endDate)
            .Include(pr => pr.RequestedByUser)
            .Include(pr => pr.ApprovedByUser)
            .Include(pr => pr.Warehouse)
            .Include(pr => pr.ProductRequestItems)
            .ToListAsync();

        var report = new ProductRequestReportResponse
        {
            FromDate = startDate,
            ToDate = endDate,
            TotalRequests = requests.Count,
            PendingRequests = requests.Count(r => r.Status == "Pending"),
            ApprovedRequests = requests.Count(r => r.Status == "Approved"),
            RejectedRequests = requests.Count(r => r.Status == "Rejected"),
            FullyReceivedRequests = requests.Count(r => r.Status == "Completed"),
            PartiallyReceivedRequests = requests.Count(r => r.Status == "Approved" && r.ProductRequestItems.Any(item => item.QuantityReceived > 0 && item.QuantityReceived < item.QuantityApproved)),
            TotalQuantityRequested = requests.Sum(r => r.ProductRequestItems.Sum(item => item.QuantityRequested)),
            TotalQuantityApproved = requests.Sum(r => r.ProductRequestItems.Sum(item => item.QuantityApproved ?? 0)),
            TotalQuantityReceived = requests.Sum(r => r.ProductRequestItems.Sum(item => item.QuantityReceived ?? 0)),
            RecentRequests = requests
                .OrderByDescending(r => r.RequestDate)
                .Take(10)
                .Select(pr => new ProductRequestListResponse
                {
                    Id = pr.Id,
                    RequestedByUserName = pr.RequestedByUser.FullName,
                    RequestDate = pr.RequestDate,
                    Status = pr.Status,
                    WarehouseName = pr.Warehouse.Name,
                    ApprovedByUserName = pr.ApprovedByUser != null ? pr.ApprovedByUser.FullName : null,
                    ApprovedAt = pr.ApprovedAt,
                    ItemCount = pr.ProductRequestItems.Count,
                    TotalQuantityRequested = pr.ProductRequestItems.Sum(item => item.QuantityRequested)
                })
                .ToList()
        };

        return Ok(report);
    }

    /// <summary>
    /// Get product request statistics
    /// </summary>
    [HttpGet("stats")]
    public async Task<ActionResult<ProductRequestStatsResponse>> GetProductRequestStats()
    {
        var requests = await _context.ProductRequests
            .Include(pr => pr.RequestedByUser)
            .Include(pr => pr.ApprovedByUser)
            .Include(pr => pr.Warehouse)
            .Include(pr => pr.ProductRequestItems)
            .ToListAsync();

        var stats = new ProductRequestStatsResponse
        {
            TotalRequests = requests.Count,
            PendingRequests = requests.Count(r => r.Status == "Pending"),
            ApprovedRequests = requests.Count(r => r.Status == "Approved"),
            RejectedRequests = requests.Count(r => r.Status == "Rejected"),
            FullyReceivedRequests = requests.Count(r => r.Status == "Completed"),
            PartiallyReceivedRequests = requests.Count(r => r.Status == "Approved" && r.ProductRequestItems.Any(item => item.QuantityReceived > 0 && item.QuantityReceived < item.QuantityApproved)),
            AverageApprovalTime = requests.Where(r => r.ApprovedAt.HasValue)
                .Average(r => (decimal)(r.ApprovedAt!.Value - r.RequestDate).TotalHours),
            AverageReceiptTime = requests.Where(r => r.ProductRequestItems.Any(item => item.QuantityReceived > 0))
                .Average(r => (decimal)r.ProductRequestItems.Where(item => item.QuantityReceived > 0)
                    .Average(item => ((item.UpdatedAt ?? DateTime.UtcNow) - r.RequestDate).TotalHours)),
            TopRequesters = requests
                .GroupBy(r => r.RequestedByUserId)
                .Select(g => new ProductRequestSummaryResponse
                {
                    RequestedByUserName = g.First().RequestedByUser.FullName,
                    TotalRequests = g.Count(),
                    TotalQuantityRequested = g.Sum(r => r.ProductRequestItems.Sum(item => item.QuantityRequested))
                })
                .OrderByDescending(r => r.TotalRequests)
                .Take(5)
                .ToList(),
            RecentRequests = requests
                .OrderByDescending(r => r.RequestDate)
                .Take(5)
                .Select(pr => new ProductRequestSummaryResponse
                {
                    RequestId = pr.Id,
                    RequestedByUserName = pr.RequestedByUser.FullName,
                    RequestDate = pr.RequestDate,
                    Status = pr.Status,
                    WarehouseName = pr.Warehouse.Name,
                    ItemCount = pr.ProductRequestItems.Count,
                    TotalQuantityRequested = pr.ProductRequestItems.Sum(item => item.QuantityRequested)
                })
                .ToList()
        };

        return Ok(stats);
    }

    /// <summary>
    /// Delete a product request (only if pending)
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteProductRequest(int id)
    {
        var productRequest = await _context.ProductRequests.FindAsync(id);
        if (productRequest == null)
        {
            return NotFound();
        }

        if (productRequest.Status != "Pending")
        {
            return BadRequest("Only pending product requests can be deleted.");
        }

        var currentUserId = GetCurrentUserId();

        _context.ProductRequests.Remove(productRequest);
        await _context.SaveChangesAsync();

        await _auditService.LogAsync("ProductRequest", productRequest.Id.ToString(), "Deleted", $"Deleted product request", null, currentUserId);

        return NoContent();
    }
}

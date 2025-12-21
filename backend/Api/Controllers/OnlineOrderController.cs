using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Api.Data;
using Api.DTOs;
using Api.Models;
using Api.Services;
using System.Security.Claims;
using System.Text.Json;
using System.ComponentModel.DataAnnotations;

namespace Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize] // All endpoints require authentication
public class OnlineOrderController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IAuditService _auditService;
    private readonly IRevenueTrackingService _revenueTrackingService;
    private readonly IOnlineOrderManager _onlineOrderManager;
    private readonly ILogger<OnlineOrderController> _logger;

    public OnlineOrderController(ApplicationDbContext context, IAuditService auditService, IRevenueTrackingService revenueTrackingService, IOnlineOrderManager onlineOrderManager, ILogger<OnlineOrderController> logger)
    {
        _context = context;
        _auditService = auditService;
        _revenueTrackingService = revenueTrackingService;
        _onlineOrderManager = onlineOrderManager;
        _logger = logger;
    }

    private int GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier");
        return userIdClaim != null ? int.Parse(userIdClaim.Value) : 0;
    }

    private async Task<bool> IsSuperAdmin()
    {
        var userId = GetCurrentUserId();
        var userRoles = await _context.UserRoles
            .Include(ur => ur.Role)
            .Where(ur => ur.UserId == userId)
            .Select(ur => ur.Role.Name)
            .ToListAsync();
        
        return userRoles.Contains("SuperAdmin");
    }

    private string GenerateOrderNumber()
    {
        var today = DateTime.UtcNow;
        var prefix = $"ON{today:yyyyMMdd}";
        var count = _context.SalesOrders.Count(so => so.OrderNumber.StartsWith(prefix)) + 1;
        return $"{prefix}{count:D4}";
    }

    /// <summary>
    /// Get all online orders (OnlineManager only)
    /// </summary>
    [HttpGet]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<ActionResult<IEnumerable<OnlineOrderResponse>>> GetOnlineOrders()
    {
        try
        {
            // Get online warehouse ID
            var onlineWarehouse = await _context.Warehouses.FirstOrDefaultAsync(w => w.Name == "Online Store");
            if (onlineWarehouse == null)
            {
                return NotFound("Online store not found");
            }

            var orders = await _context.SalesOrders
                .Include(so => so.SalesItems)
                    .ThenInclude(si => si.Product)
                .Include(so => so.CreatedByUser)
                .Where(so => so.SalesItems.Any(si => si.WarehouseId == onlineWarehouse.Id))
                .Select(so => new OnlineOrderResponse
                {
                    Id = so.Id,
                    OrderNumber = so.OrderNumber,
                    CustomerName = so.CustomerName,
                    CustomerEmail = so.CustomerEmail,
                    CustomerPhone = so.CustomerPhone,
                    CustomerAddress = so.CustomerAddress,
                    OrderDate = so.OrderDate,
                    DeliveryDate = so.DeliveryDate,
                    TotalAmount = so.TotalAmount,
                    Status = so.Status,
                    PaymentStatus = so.PaymentStatus,
                    Notes = so.Notes,
                    CreatedByUserName = so.CreatedByUser.FullName,
                    Items = so.SalesItems.Where(si => si.WarehouseId == onlineWarehouse.Id).Select(si => new OnlineOrderItemResponse
                    {
                        Id = si.Id,
                        ProductId = si.ProductId,
                        ProductName = si.Product.Name,
                        ProductSKU = si.Product.SKU,
                        Quantity = si.Quantity,
                        UnitPrice = si.UnitPrice,
                        TotalPrice = si.TotalPrice,
                        Unit = si.Unit
                    }).ToList()
                })
                .OrderByDescending(so => so.OrderDate)
                .ToListAsync();

            return Ok(orders);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving online orders");
            return StatusCode(500, new { message = "An error occurred while retrieving online orders" });
        }
    }

    /// <summary>
    /// Get online order by ID
    /// </summary>
    [HttpGet("{id}")]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<ActionResult<OnlineOrderResponse>> GetOnlineOrder(int id)
    {
        try
        {
            var onlineWarehouse = await _context.Warehouses.FirstOrDefaultAsync(w => w.Name == "Online Store");
            if (onlineWarehouse == null)
            {
                return NotFound("Online warehouse not found");
            }

            var order = await _context.SalesOrders
                .Include(so => so.SalesItems)
                    .ThenInclude(si => si.Product)
                .Include(so => so.CreatedByUser)
                .FirstOrDefaultAsync(so => so.Id == id && so.SalesItems.Any(si => si.WarehouseId == onlineWarehouse.Id));

            if (order == null)
            {
                return NotFound();
            }

            var response = new OnlineOrderResponse
            {
                Id = order.Id,
                OrderNumber = order.OrderNumber,
                CustomerName = order.CustomerName,
                CustomerEmail = order.CustomerEmail,
                CustomerPhone = order.CustomerPhone,
                CustomerAddress = order.CustomerAddress,
                OrderDate = order.OrderDate,
                DeliveryDate = order.DeliveryDate,
                TotalAmount = order.TotalAmount,
                Status = order.Status,
                PaymentStatus = order.PaymentStatus,
                Notes = order.Notes,
                CreatedByUserName = order.CreatedByUser.FullName,
                Items = order.SalesItems.Where(si => si.WarehouseId == onlineWarehouse.Id).Select(si => new OnlineOrderItemResponse
                {
                    Id = si.Id,
                    ProductId = si.ProductId,
                    ProductName = si.Product.Name,
                    ProductSKU = si.Product.SKU,
                    Quantity = si.Quantity,
                    UnitPrice = si.UnitPrice,
                    TotalPrice = si.TotalPrice,
                    Unit = si.Unit
                }).ToList()
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving online order {OrderId}", id);
            return StatusCode(500, new { message = "An error occurred while retrieving the online order" });
        }
    }

    /// <summary>
    /// Create a new online order using the OnlineOrderManager
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<ActionResult<OnlineOrderResponse>> CreateOnlineOrder([FromBody] CreateOnlineOrderRequest request)
    {
        try
        {
            var currentUserId = GetCurrentUserId();
            var result = await _onlineOrderManager.ProcessNewOrderAsync(request, currentUserId);

            if (!result.Success)
            {
                return BadRequest(new { message = result.ErrorMessage, warnings = result.Warnings });
            }

            return CreatedAtAction(nameof(GetOnlineOrder), new { id = result.Order!.Id }, result.Order);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating online order");
            return StatusCode(500, new { message = "An error occurred while creating the online order" });
        }
    }

    /// <summary>
    /// Update online order status using the OnlineOrderManager
    /// </summary>
    [HttpPut("{id}/status")]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<IActionResult> UpdateOnlineOrderStatus(int id, [FromBody] UpdateOnlineOrderStatusRequest request)
    {
        try
        {
            var currentUserId = GetCurrentUserId();
            var result = await _onlineOrderManager.UpdateOrderStatusAsync(id, request, currentUserId);

            if (!result.Success)
            {
                return BadRequest(new { message = result.ErrorMessage });
            }

            return Ok(new { 
                message = $"Order status updated to {result.NewStatus}",
                previousStatus = result.PreviousStatus,
                newStatus = result.NewStatus,
                actionsPerformed = result.ActionsPerformed
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating online order status for order {OrderId}", id);
            return StatusCode(500, new { message = "An error occurred while updating the order status" });
        }
    }

    /// <summary>
    /// Get online inventory (products available for online orders)
    /// </summary>
    [HttpGet("inventory")]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<ActionResult<IEnumerable<OnlineInventoryResponse>>> GetOnlineInventory()
    {
        try
        {
            var onlineWarehouse = await _context.Warehouses.FirstOrDefaultAsync(w => w.Name == "Online Store");
            if (onlineWarehouse == null)
            {
                return NotFound("Online warehouse not found");
            }

            var inventory = await _context.ProductInventories
                .Include(pi => pi.Product)
                .ThenInclude(p => p.Category)
                .Where(pi => pi.WarehouseId == onlineWarehouse.Id && pi.Quantity > 0)
                .Select(pi => new OnlineInventoryResponse
                {
                    ProductId = pi.ProductId,
                    ProductName = pi.Product.Name,
                    ProductSKU = pi.Product.SKU,
                    CategoryName = pi.Product.Category.Name,
                    Price = pi.Product.Price,
                    AvailableQuantity = pi.Quantity,
                    Unit = pi.Unit,
                    MinimumStockLevel = pi.MinimumStockLevel ?? 0,
                    MaximumStockLevel = pi.MaximumStockLevel ?? 0
                })
                .OrderBy(pi => pi.ProductName)
                .ToListAsync();

            return Ok(inventory);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving online inventory");
            return StatusCode(500, new { message = "An error occurred while retrieving online inventory" });
        }
    }

    /// <summary>
    /// Update online inventory (restock products)
    /// </summary>
    [HttpPut("inventory/{productId}")]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<IActionResult> UpdateOnlineInventory(int productId, [FromBody] UpdateOnlineInventoryRequest request)
    {
        try
        {
            var currentUserId = GetCurrentUserId();
            var onlineWarehouse = await _context.Warehouses.FirstOrDefaultAsync(w => w.Name == "Online Store");
            
            if (onlineWarehouse == null)
            {
                return NotFound("Online warehouse not found");
            }

            var inventory = await _context.ProductInventories
                .FirstOrDefaultAsync(pi => pi.ProductId == productId && pi.WarehouseId == onlineWarehouse.Id);

            if (inventory == null)
            {
                return NotFound("Product not found in online inventory");
            }

            var oldQuantity = inventory.Quantity;
            inventory.Quantity = request.Quantity;
            inventory.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            // Audit log
            await _auditService.LogAsync("OnlineInventory", productId.ToString(), "UPDATE", 
                $"Quantity changed from {oldQuantity} to {request.Quantity}", JsonSerializer.Serialize(new { ProductId = productId, NewQuantity = request.Quantity }), currentUserId);

            return Ok(new { message = "Online inventory updated successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating online inventory for product {ProductId}", productId);
            return StatusCode(500, new { message = "An error occurred while updating online inventory" });
        }
    }

    #region Advanced Order Management Endpoints

    /// <summary>
    /// Accept an online order
    /// </summary>
    [HttpPost("{id}/accept")]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<IActionResult> AcceptOrder(int id, [FromBody] AcceptOrderRequest? request = null)
    {
        try
        {
            var currentUserId = GetCurrentUserId();
            var result = await _onlineOrderManager.AcceptOrderAsync(id, currentUserId, request?.Notes);

            if (!result.Success)
            {
                return BadRequest(new { message = result.ErrorMessage });
            }

            return Ok(new { 
                message = "Order accepted successfully",
                status = result.Status,
                actionsPerformed = result.ActionsPerformed,
                estimatedDeliveryDate = result.EstimatedDeliveryDate
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error accepting order {OrderId}", id);
            return StatusCode(500, new { message = "An error occurred while accepting the order" });
        }
    }

    /// <summary>
    /// Ship an online order
    /// </summary>
    [HttpPost("{id}/ship")]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<IActionResult> ShipOrder(int id, [FromBody] ShipOrderRequest? request = null)
    {
        try
        {
            var currentUserId = GetCurrentUserId();
            var result = await _onlineOrderManager.ShipOrderAsync(id, currentUserId, request?.DeliveryDate, request?.Notes);

            if (!result.Success)
            {
                return BadRequest(new { message = result.ErrorMessage });
            }

            return Ok(new { 
                message = "Order shipped successfully",
                status = result.Status,
                actionsPerformed = result.ActionsPerformed,
                estimatedDeliveryDate = result.EstimatedDeliveryDate
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error shipping order {OrderId}", id);
            return StatusCode(500, new { message = "An error occurred while shipping the order" });
        }
    }

    /// <summary>
    /// Mark order as delivered
    /// </summary>
    [HttpPost("{id}/deliver")]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<IActionResult> DeliverOrder(int id, [FromBody] DeliverOrderRequest? request = null)
    {
        try
        {
            var currentUserId = GetCurrentUserId();
            var result = await _onlineOrderManager.DeliverOrderAsync(id, currentUserId, request?.Notes);

            if (!result.Success)
            {
                return BadRequest(new { message = result.ErrorMessage });
            }

            return Ok(new { 
                message = "Order delivered successfully",
                status = result.Status,
                actionsPerformed = result.ActionsPerformed
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error delivering order {OrderId}", id);
            return StatusCode(500, new { message = "An error occurred while delivering the order" });
        }
    }

    /// <summary>
    /// Cancel an online order
    /// </summary>
    [HttpPost("{id}/cancel")]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<IActionResult> CancelOrder(int id, [FromBody] CancelOrderRequest request)
    {
        try
        {
            var currentUserId = GetCurrentUserId();
            var result = await _onlineOrderManager.CancelOrderAsync(id, currentUserId, request.Reason);

            if (!result.Success)
            {
                return BadRequest(new { message = result.ErrorMessage });
            }

            return Ok(new { 
                message = "Order cancelled successfully",
                status = result.Status,
                actionsPerformed = result.ActionsPerformed
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling order {OrderId}", id);
            return StatusCode(500, new { message = "An error occurred while cancelling the order" });
        }
    }

    /// <summary>
    /// Get orders by status
    /// </summary>
    [HttpGet("status/{status}")]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<ActionResult<IEnumerable<OnlineOrderResponse>>> GetOrdersByStatus(string status)
    {
        try
        {
            var orders = await _onlineOrderManager.GetOrdersByStatusAsync(status);
            return Ok(orders);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting orders by status {Status}", status);
            return StatusCode(500, new { message = "An error occurred while retrieving orders" });
        }
    }

    /// <summary>
    /// Get orders requiring attention
    /// </summary>
    [HttpGet("attention")]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<ActionResult<IEnumerable<OnlineOrderResponse>>> GetOrdersRequiringAttention()
    {
        try
        {
            var orders = await _onlineOrderManager.GetOrdersRequiringAttentionAsync();
            return Ok(orders);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting orders requiring attention");
            return StatusCode(500, new { message = "An error occurred while retrieving orders" });
        }
    }

    /// <summary>
    /// Get order analytics
    /// </summary>
    [HttpGet("analytics")]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<ActionResult<OrderAnalyticsResult>> GetOrderAnalytics([FromQuery] DateTime? fromDate = null, [FromQuery] DateTime? toDate = null)
    {
        try
        {
            var analytics = await _onlineOrderManager.GetOrderAnalyticsAsync(fromDate, toDate);
            return Ok(analytics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting order analytics");
            return StatusCode(500, new { message = "An error occurred while retrieving analytics" });
        }
    }

    /// <summary>
    /// Validate order before processing
    /// </summary>
    [HttpPost("validate")]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<ActionResult<OrderValidationResult>> ValidateOrder([FromBody] CreateOnlineOrderRequest request)
    {
        try
        {
            var validation = await _onlineOrderManager.ValidateOrderAsync(request);
            return Ok(validation);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating order");
            return StatusCode(500, new { message = "An error occurred while validating the order" });
        }
    }

    /// <summary>
    /// Check inventory availability for order items
    /// </summary>
    [HttpPost("check-inventory")]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<ActionResult<InventoryCheckResult>> CheckInventoryAvailability([FromBody] List<CreateOnlineOrderItemRequest> items)
    {
        try
        {
            var result = await _onlineOrderManager.CheckInventoryAvailabilityAsync(items);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking inventory availability");
            return StatusCode(500, new { message = "An error occurred while checking inventory" });
        }
    }

    #endregion
}

// Additional DTOs for the new endpoints
public class AcceptOrderRequest
{
    public string? Notes { get; set; }
}

public class ShipOrderRequest
{
    public DateTime? DeliveryDate { get; set; }
    public string? Notes { get; set; }
}

public class DeliverOrderRequest
{
    public string? Notes { get; set; }
}

public class CancelOrderRequest
{
    [Required]
    public string Reason { get; set; } = string.Empty;
}

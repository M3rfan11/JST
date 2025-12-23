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
public class SalesController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IAuditService _auditService;
    private readonly ILogger<SalesController> _logger;

    public SalesController(ApplicationDbContext context, IAuditService auditService, ILogger<SalesController> logger)
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

    private string GenerateOrderNumber()
    {
        var today = DateTime.UtcNow;
        var prefix = $"SO{today:yyyyMMdd}";
        var count = _context.SalesOrders.Count(so => so.OrderNumber.StartsWith(prefix)) + 1;
        return $"{prefix}{count:D4}";
    }

    /// <summary>
    /// Get all sales orders
    /// </summary>
    [HttpGet]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<ActionResult<IEnumerable<SalesOrderListResponse>>> GetSalesOrders()
    {
        var currentUserId = GetCurrentUserId();
        var currentUser = await _context.Users.FindAsync(currentUserId);
        
        IQueryable<SalesOrder> ordersQuery = _context.SalesOrders
            .Include(so => so.CreatedByUser)
            .Include(so => so.ConfirmedByUser)
            .Include(so => so.SalesItems)
                .ThenInclude(si => si.Product);
        
        // Note: PromoCodeUsages are loaded separately for better performance

        // Apply store-scoped filtering
        var userRoles = await _context.UserRoles
            .Where(ur => ur.UserId == currentUserId)
            .Include(ur => ur.Role)
            .Select(ur => ur.Role.Name)
            .ToListAsync();

        if (userRoles.Contains("SuperAdmin"))
        {
            // SuperAdmin can see all sales
        }
        else
        {
            return StatusCode(403, new { Message = "You don't have permission to view sales orders" });
        }

        var ordersList = await ordersQuery
            .OrderByDescending(so => so.CreatedAt)
            .ToListAsync();

        // Get all promo code usages for these orders
        var orderIds = ordersList.Select(o => o.Id).ToList();
        var promoCodeUsages = await _context.PromoCodeUsages
            .Include(pcu => pcu.PromoCode)
            .Include(pcu => pcu.User)
            .Where(pcu => orderIds.Contains(pcu.SalesOrderId))
            .ToListAsync();

        var orders = ordersList.Select(so => 
        {
            var promoCodeUsage = promoCodeUsages.FirstOrDefault(pcu => pcu.SalesOrderId == so.Id);
            var promoCodeInfo = promoCodeUsage != null ? new PromoCodeUsageInfo
            {
                PromoCodeId = promoCodeUsage.PromoCodeId,
                Code = promoCodeUsage.PromoCode.Code,
                DiscountAmount = promoCodeUsage.DiscountAmount,
                UsedAt = promoCodeUsage.UsedAt,
                UserId = promoCodeUsage.UserId,
                UserName = promoCodeUsage.User?.FullName ?? (promoCodeUsage.UserId.HasValue ? "Unknown User" : "Guest")
            } : null;

            return new SalesOrderListResponse
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
                DownPayment = so.DownPayment,
                Status = so.Status,
                PaymentStatus = so.PaymentStatus,
                Notes = so.Notes,
                CreatedByUserName = so.CreatedByUser?.FullName ?? "Guest",
                ConfirmedByUserName = so.ConfirmedByUser != null ? so.ConfirmedByUser.FullName : null,
                Items = so.SalesItems.Select(si => new SalesItemResponse
                {
                    Id = si.Id,
                    ProductId = si.ProductId,
                    ProductVariantId = null, // SalesItem doesn't have ProductVariantId in the model
                    ProductName = si.Product != null ? si.Product.Name : "Unknown Product",
                    ProductSKU = si.Product != null ? si.Product.SKU : null,
                    VariantAttributes = null, // Will be populated from variant if needed
                    Quantity = si.Quantity,
                    UnitPrice = si.UnitPrice,
                    TotalPrice = si.TotalPrice,
                    Unit = si.Unit
                }).ToList(),
                PromoCode = promoCodeInfo
            };
        }).ToList();

        return Ok(orders);
    }

    /// <summary>
    /// Get a specific sales order by ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<SalesOrderResponse>> GetSalesOrder(int id)
    {
        var order = await _context.SalesOrders
            .Include(so => so.CreatedByUser)
            .Include(so => so.ConfirmedByUser)
            .Include(so => so.SalesItems)
                .ThenInclude(si => si.Product)
            .Include(so => so.SalesItems)
                .ThenInclude(si => si.Warehouse)
            .FirstOrDefaultAsync(so => so.Id == id);

        if (order == null)
        {
            return NotFound();
        }

        var response = new SalesOrderResponse
        {
            Id = order.Id,
            OrderNumber = order.OrderNumber,
            CustomerName = order.CustomerName,
            CustomerAddress = order.CustomerAddress,
            CustomerPhone = order.CustomerPhone,
            CustomerEmail = order.CustomerEmail,
            OrderDate = order.OrderDate,
            DeliveryDate = order.DeliveryDate,
            TotalAmount = order.TotalAmount,
            Status = order.Status,
            PaymentStatus = order.PaymentStatus,
            Notes = order.Notes,
            CreatedByUserId = order.CreatedByUserId,
            CreatedByUserName = order.CreatedByUser.FullName,
            ConfirmedByUserId = order.ConfirmedByUserId,
            ConfirmedByUserName = order.ConfirmedByUser?.FullName,
            CreatedAt = order.CreatedAt,
            UpdatedAt = order.UpdatedAt,
            Items = order.SalesItems.Select(si => new SalesItemResponse
            {
                Id = si.Id,
                ProductId = si.ProductId,
                ProductName = si.Product.Name,
                ProductSKU = si.Product.SKU ?? "",
                ProductDescription = si.Product.Description ?? "",
                WarehouseId = si.WarehouseId,
                WarehouseName = si.Warehouse.Name,
                Quantity = si.Quantity,
                UnitPrice = si.UnitPrice,
                TotalPrice = si.TotalPrice,
                Unit = si.Unit,
                Notes = si.Notes,
                AvailableQuantity = 0 // Will be calculated separately if needed
            }).ToList()
        };

        return Ok(response);
    }

    /// <summary>
    /// Create a new sales order
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<ActionResult<SalesOrderResponse>> CreateSalesOrder(CreateSalesOrderRequest request)
    {
        if (request.Items == null || !request.Items.Any())
        {
            return BadRequest("Sales order must contain at least one item.");
        }

        var orderNumber = GenerateOrderNumber();
        var currentUserId = GetCurrentUserId();

        var order = new SalesOrder
        {
            OrderNumber = orderNumber,
            CustomerName = request.CustomerName,
            CustomerAddress = request.CustomerAddress,
            CustomerPhone = request.CustomerPhone,
            CustomerEmail = request.CustomerEmail,
            DeliveryDate = request.DeliveryDate,
            Notes = request.Notes,
            CreatedByUserId = currentUserId,
            Status = "Pending",
            PaymentStatus = "Pending"
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

            // Check stock availability
            var inventory = await _context.ProductInventories
                .FirstOrDefaultAsync(pi => pi.ProductId == itemRequest.ProductId && pi.WarehouseId == itemRequest.WarehouseId);

            if (inventory == null || inventory.Quantity < itemRequest.Quantity)
            {
                return BadRequest($"Insufficient stock for product {product.Name} in warehouse {warehouse.Name}. Available: {inventory?.Quantity ?? 0}, Requested: {itemRequest.Quantity}");
            }

            var itemTotalPrice = itemRequest.Quantity * itemRequest.UnitPrice;
            totalAmount += itemTotalPrice;

            var item = new SalesItem
            {
                ProductId = itemRequest.ProductId,
                WarehouseId = itemRequest.WarehouseId,
                Quantity = itemRequest.Quantity,
                UnitPrice = itemRequest.UnitPrice,
                TotalPrice = itemTotalPrice,
                Unit = itemRequest.Unit,
                Notes = itemRequest.Notes
            };

            order.SalesItems.Add(item);
        }

        order.TotalAmount = totalAmount;

        _context.SalesOrders.Add(order);
        await _context.SaveChangesAsync();

        await _auditService.LogAsync("SalesOrder", order.Id.ToString(), "Created", $"Created sales order {orderNumber}", null, currentUserId);

        // Return simple response with order number
        return Ok(new { 
            id = order.Id, 
            orderNumber = orderNumber,
            status = order.Status,
            totalAmount = order.TotalAmount,
            customerName = order.CustomerName
        });
    }

    /// <summary>
    /// Update a sales order
    /// </summary>
    [HttpPut("{id}")]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<IActionResult> UpdateSalesOrder(int id, UpdateSalesOrderRequest request)
    {
        var order = await _context.SalesOrders.FindAsync(id);
        if (order == null)
        {
            return NotFound();
        }

        if (order.Status == "Delivered" || order.Status == "Cancelled")
        {
            return BadRequest("Cannot update a sales order that has been delivered or cancelled.");
        }

        var currentUserId = GetCurrentUserId();

        var oldStatus = order.Status;
        
        order.CustomerName = request.CustomerName ?? order.CustomerName;
        order.CustomerAddress = request.CustomerAddress ?? order.CustomerAddress;
        order.CustomerPhone = request.CustomerPhone ?? order.CustomerPhone;
        order.CustomerEmail = request.CustomerEmail ?? order.CustomerEmail;
        
        // Handle status change to Delivered - set actual delivery date
        if (!string.IsNullOrEmpty(request.Status) && request.Status == "Delivered" && oldStatus != "Delivered")
        {
            order.DeliveryDate = request.DeliveryDate ?? DateTime.UtcNow;
        }
        else
        {
            order.DeliveryDate = request.DeliveryDate ?? order.DeliveryDate;
        }
        
        order.Status = request.Status ?? order.Status;
        order.PaymentStatus = request.PaymentStatus ?? order.PaymentStatus;
        if (request.DownPayment.HasValue)
        {
            order.DownPayment = request.DownPayment.Value;
        }
        order.Notes = request.Notes ?? order.Notes;
        order.UpdatedAt = DateTime.UtcNow;

        // Create tracking entry if status changed
        if (!string.IsNullOrEmpty(request.Status) && request.Status != oldStatus)
        {
            var tracking = new OrderTracking
            {
                OrderId = order.Id,
                Status = request.Status,
                Notes = $"Status changed from {oldStatus} to {request.Status}" + (!string.IsNullOrEmpty(request.Notes) ? $". {request.Notes}" : ""),
                Timestamp = DateTime.UtcNow,
                UpdatedByUserId = currentUserId
            };
            _context.OrderTrackings.Add(tracking);
        }

        _context.Entry(order).State = EntityState.Modified;
        await _context.SaveChangesAsync();

        await _auditService.LogAsync("SalesOrder", order.Id.ToString(), "Updated", $"Updated sales order {order.OrderNumber}", null, currentUserId);

        return NoContent();
    }

    /// <summary>
    /// Confirm a sales order
    /// </summary>
    [HttpPost("{id}/confirm")]
    public async Task<IActionResult> ConfirmSalesOrder(int id, ConfirmSalesOrderRequest request)
    {
        var order = await _context.SalesOrders.FindAsync(id);
        if (order == null)
        {
            return NotFound();
        }

        if (order.Status != "Pending")
        {
            return BadRequest("Only pending sales orders can be confirmed.");
        }

        var currentUserId = GetCurrentUserId();
        order.Status = "Confirmed";
        order.ConfirmedByUserId = currentUserId;
        order.UpdatedAt = DateTime.UtcNow;

        if (!string.IsNullOrEmpty(request.Notes))
        {
            order.Notes = string.IsNullOrEmpty(order.Notes) ? request.Notes : $"{order.Notes}\nConfirmation Notes: {request.Notes}";
        }

        _context.Entry(order).State = EntityState.Modified;
        await _context.SaveChangesAsync();

        await _auditService.LogAsync("SalesOrder", order.Id.ToString(), "Confirmed", $"Confirmed sales order {order.OrderNumber}", null, currentUserId);

        return NoContent();
    }

    /// <summary>
    /// Ship a sales order (mark as shipped and update inventory)
    /// </summary>
    [HttpPost("{id}/ship")]
    public async Task<IActionResult> ShipSalesOrder(int id, ShipSalesOrderRequest request)
    {
        var order = await _context.SalesOrders
            .Include(so => so.SalesItems)
            .FirstOrDefaultAsync(so => so.Id == id);

        if (order == null)
        {
            return NotFound();
        }

        if (order.Status != "Confirmed")
        {
            return BadRequest("Only confirmed sales orders can be shipped.");
        }

        var currentUserId = GetCurrentUserId();

        // Update order status
        order.Status = "Shipped";
        order.DeliveryDate = request.DeliveryDate ?? DateTime.UtcNow.AddDays(1);
        order.UpdatedAt = DateTime.UtcNow;

        if (!string.IsNullOrEmpty(request.Notes))
        {
            order.Notes = string.IsNullOrEmpty(order.Notes) ? request.Notes : $"{order.Notes}\nShipping Notes: {request.Notes}";
        }

        // Update inventory for each item (reduce stock)
        foreach (var item in order.SalesItems)
        {
            var inventory = await _context.ProductInventories
                .FirstOrDefaultAsync(pi => pi.ProductId == item.ProductId && pi.WarehouseId == item.WarehouseId);

            if (inventory != null)
            {
                inventory.Quantity -= item.Quantity;
                inventory.UpdatedAt = DateTime.UtcNow;
            }
        }

        await _context.SaveChangesAsync();

        await _auditService.LogAsync("SalesOrder", order.Id.ToString(), "Shipped", $"Shipped sales order {order.OrderNumber} and updated inventory", null, currentUserId);

        return NoContent();
    }

    /// <summary>
    /// Deliver a sales order
    /// </summary>
    [HttpPost("{id}/deliver")]
    public async Task<IActionResult> DeliverSalesOrder(int id, DeliverSalesOrderRequest request)
    {
        var order = await _context.SalesOrders.FindAsync(id);
        if (order == null)
        {
            return NotFound();
        }

        if (order.Status != "Shipped")
        {
            return BadRequest("Only shipped sales orders can be delivered.");
        }

        var currentUserId = GetCurrentUserId();
        order.Status = "Delivered";
        order.DeliveryDate = request.ActualDeliveryDate ?? DateTime.UtcNow;
        order.UpdatedAt = DateTime.UtcNow;

        if (!string.IsNullOrEmpty(request.Notes))
        {
            order.Notes = string.IsNullOrEmpty(order.Notes) ? request.Notes : $"{order.Notes}\nDelivery Notes: {request.Notes}";
        }

        _context.Entry(order).State = EntityState.Modified;
        await _context.SaveChangesAsync();

        await _auditService.LogAsync("SalesOrder", order.Id.ToString(), "Delivered", $"Delivered sales order {order.OrderNumber}", null, currentUserId);

        return NoContent();
    }

    /// <summary>
    /// Cancel a sales order
    /// </summary>
    [HttpPost("{id}/cancel")]
    public async Task<IActionResult> CancelSalesOrder(int id)
    {
        var order = await _context.SalesOrders.FindAsync(id);
        if (order == null)
        {
            return NotFound();
        }

        if (order.Status == "Delivered" || order.Status == "Cancelled")
        {
            return BadRequest("Cannot cancel a sales order that has been delivered or is already cancelled.");
        }

        var currentUserId = GetCurrentUserId();
        order.Status = "Cancelled";
        order.UpdatedAt = DateTime.UtcNow;

        _context.Entry(order).State = EntityState.Modified;
        await _context.SaveChangesAsync();

        await _auditService.LogAsync("SalesOrder", order.Id.ToString(), "Cancelled", $"Cancelled sales order {order.OrderNumber}", null, currentUserId);

        return NoContent();
    }

    /// <summary>
    /// Delete a sales order (only if it's pending)
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteSalesOrder(int id)
    {
        var order = await _context.SalesOrders.FindAsync(id);
        if (order == null)
        {
            return NotFound();
        }

        if (order.Status != "Pending")
        {
            return BadRequest("Only pending sales orders can be deleted.");
        }

        var currentUserId = GetCurrentUserId();

        _context.SalesOrders.Remove(order);
        await _context.SaveChangesAsync();

        await _auditService.LogAsync("SalesOrder", order.Id.ToString(), "Deleted", $"Deleted sales order {order.OrderNumber}", null, currentUserId);

        return NoContent();
    }

    /// <summary>
    /// Get all customers (users who have ordered or signed up)
    /// </summary>
    [HttpGet("customers")]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<ActionResult<IEnumerable<CustomerSummaryResponse>>> GetCustomers()
    {
        try
        {
            // Get all registered users (who have signed up) - only Customer role
            var customerRole = await _context.Roles.FirstOrDefaultAsync(r => r.Name == "Customer");
            var registeredUsers = await _context.Users
                .Where(u => u.IsActive && u.UserRoles.Any(ur => ur.RoleId == customerRole.Id))
                .Select(u => new
                {
                    Email = u.Email,
                    FullName = u.FullName,
                    Phone = (string?)null,
                    Address = (string?)null,
                    IsRegistered = true,
                    UserId = u.Id,
                    CreatedAt = u.CreatedAt
                })
                .ToListAsync();

            // Get unique customers from orders (grouped by email)
            var orderCustomersByEmail = await _context.SalesOrders
                .Where(so => !string.IsNullOrEmpty(so.CustomerEmail))
                .GroupBy(so => so.CustomerEmail!.ToLower())
                .Select(g => new
                {
                    Email = g.Key,
                    FullName = g.OrderByDescending(so => so.CreatedAt).First().CustomerName ?? "Unknown",
                    Phone = g.OrderByDescending(so => so.CreatedAt).First().CustomerPhone,
                    Address = g.OrderByDescending(so => so.CreatedAt).First().CustomerAddress,
                    OrderCount = g.Count(),
                    TotalSpent = g.Sum(so => so.TotalAmount),
                    FirstOrderDate = g.Min(so => so.OrderDate),
                    LastOrderDate = g.Max(so => so.OrderDate),
                    CreatedAt = g.Min(so => so.CreatedAt)
                })
                .ToListAsync();

            // Get customers by phone (for those without email)
            var orderCustomersByPhone = await _context.SalesOrders
                .Where(so => string.IsNullOrEmpty(so.CustomerEmail) && !string.IsNullOrEmpty(so.CustomerPhone))
                .GroupBy(so => so.CustomerPhone!)
                .Select(g => new
                {
                    Email = (string?)null,
                    FullName = g.OrderByDescending(so => so.CreatedAt).First().CustomerName ?? "Unknown",
                    Phone = g.Key,
                    Address = g.OrderByDescending(so => so.CreatedAt).First().CustomerAddress,
                    OrderCount = g.Count(),
                    TotalSpent = g.Sum(so => so.TotalAmount),
                    FirstOrderDate = g.Min(so => so.OrderDate),
                    LastOrderDate = g.Max(so => so.OrderDate),
                    CreatedAt = g.Min(so => so.CreatedAt)
                })
                .ToListAsync();

            // Combine customers
            var customerMap = new Dictionary<string, CustomerSummaryResponse>();

            // Add registered users
            foreach (var user in registeredUsers)
            {
                var key = user.Email?.ToLower() ?? "";
                if (!string.IsNullOrEmpty(key))
                {
                    // Get order stats for this user
                    var userOrders = await _context.SalesOrders
                        .Where(so => so.CustomerEmail != null && so.CustomerEmail.ToLower() == key)
                        .ToListAsync();

                    customerMap[key] = new CustomerSummaryResponse
                    {
                        Email = user.Email,
                        FullName = user.FullName,
                        Phone = user.Phone,
                        Address = user.Address,
                        IsRegistered = true,
                        UserId = user.UserId,
                        OrderCount = userOrders.Count,
                        TotalSpent = userOrders.Sum(o => o.TotalAmount),
                        FirstOrderDate = userOrders.Any() ? userOrders.Min(o => o.OrderDate) : user.CreatedAt,
                        LastOrderDate = userOrders.Any() ? userOrders.Max(o => o.OrderDate) : (DateTime?)null,
                        CreatedAt = user.CreatedAt
                    };
                }
            }

            // Add order customers by email
            foreach (var orderCustomer in orderCustomersByEmail)
            {
                var key = orderCustomer.Email.ToLower();
                if (customerMap.ContainsKey(key))
                {
                    // Merge with existing
                    var existing = customerMap[key];
                    existing.OrderCount = orderCustomer.OrderCount;
                    existing.TotalSpent = orderCustomer.TotalSpent;
                    if (orderCustomer.Phone != null) existing.Phone = orderCustomer.Phone;
                    if (orderCustomer.Address != null) existing.Address = orderCustomer.Address;
                    if (orderCustomer.FirstOrderDate < existing.FirstOrderDate)
                        existing.FirstOrderDate = orderCustomer.FirstOrderDate;
                    if (orderCustomer.LastOrderDate > (existing.LastOrderDate ?? DateTime.MinValue))
                        existing.LastOrderDate = orderCustomer.LastOrderDate;
                }
                else
                {
                    customerMap[key] = new CustomerSummaryResponse
                    {
                        Email = orderCustomer.Email,
                        FullName = orderCustomer.FullName,
                        Phone = orderCustomer.Phone,
                        Address = orderCustomer.Address,
                        IsRegistered = false,
                        UserId = null,
                        OrderCount = orderCustomer.OrderCount,
                        TotalSpent = orderCustomer.TotalSpent,
                        FirstOrderDate = orderCustomer.FirstOrderDate,
                        LastOrderDate = orderCustomer.LastOrderDate,
                        CreatedAt = orderCustomer.CreatedAt
                    };
                }
            }

            // Add customers by phone (no email)
            foreach (var orderCustomer in orderCustomersByPhone)
            {
                var key = $"phone_{orderCustomer.Phone}";
                if (!customerMap.ContainsKey(key))
                {
                    customerMap[key] = new CustomerSummaryResponse
                    {
                        Email = orderCustomer.Email,
                        FullName = orderCustomer.FullName,
                        Phone = orderCustomer.Phone,
                        Address = orderCustomer.Address,
                        IsRegistered = false,
                        UserId = null,
                        OrderCount = orderCustomer.OrderCount,
                        TotalSpent = orderCustomer.TotalSpent,
                        FirstOrderDate = orderCustomer.FirstOrderDate,
                        LastOrderDate = orderCustomer.LastOrderDate,
                        CreatedAt = orderCustomer.CreatedAt
                    };
                }
            }

            var customers = customerMap.Values
                .OrderByDescending(c => c.LastOrderDate ?? c.CreatedAt)
                .ToList();

            return Ok(customers);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving customers");
            return StatusCode(500, new { message = "An error occurred while retrieving customers" });
        }
    }

    /// <summary>
    /// Get sales orders by status
    /// </summary>
    [HttpGet("status/{status}")]
    public async Task<ActionResult<IEnumerable<SalesOrderListResponse>>> GetSalesOrdersByStatus(string status)
    {
        var orders = await _context.SalesOrders
            .Where(so => so.Status == status)
            .Include(so => so.CreatedByUser)
            .Include(so => so.ConfirmedByUser)
            .OrderByDescending(so => so.CreatedAt)
            .Select(so => new SalesOrderListResponse
            {
                Id = so.Id,
                OrderNumber = so.OrderNumber,
                CustomerName = so.CustomerName,
                OrderDate = so.OrderDate,
                DeliveryDate = so.DeliveryDate,
                TotalAmount = so.TotalAmount,
                Status = so.Status,
                PaymentStatus = so.PaymentStatus,
                CreatedByUserName = so.CreatedByUser.FullName,
                ConfirmedByUserName = so.ConfirmedByUser != null ? so.ConfirmedByUser.FullName : null
            })
            .ToListAsync();

        return Ok(orders);
    }

    /// <summary>
    /// Get products in card-style layout for selection
    /// </summary>
    [HttpGet("products/cards")]
    public async Task<ActionResult<IEnumerable<ProductCardResponse>>> GetProductCards()
    {
        var products = await _context.Products
            .Where(p => p.IsActive)
            .Include(p => p.Category)
            .Include(p => p.ProductInventories)
                .ThenInclude(pi => pi.Warehouse)
            .Select(p => new ProductCardResponse
            {
                Id = p.Id,
                Name = p.Name,
                Description = p.Description,
                Price = p.Price,
                Unit = p.Unit,
                SKU = p.SKU,
                Brand = p.Brand,
                ImageUrl = null, // You can add image URL field to Product model if needed
                CategoryName = p.Category.Name,
                IsActive = p.IsActive,
                WarehouseStocks = p.ProductInventories.Select(pi => new ProductWarehouseStock
                {
                    WarehouseId = pi.WarehouseId,
                    WarehouseName = pi.Warehouse.Name,
                    AvailableQuantity = pi.Quantity,
                    Unit = pi.Unit,
                    MinimumStockLevel = pi.MinimumStockLevel,
                    IsLowStock = pi.MinimumStockLevel.HasValue && pi.Quantity <= pi.MinimumStockLevel
                }).ToList()
            })
            .ToListAsync();

        return Ok(products);
    }

    /// <summary>
    /// Get sales report
    /// </summary>
    [HttpGet("report")]
    public async Task<ActionResult<SalesSummaryResponse>> GetSalesReport(DateTime? fromDate = null, DateTime? toDate = null)
    {
        var startDate = fromDate ?? DateTime.UtcNow.AddDays(-30);
        var endDate = toDate ?? DateTime.UtcNow;

        var orders = await _context.SalesOrders
            .Where(so => so.OrderDate >= startDate && so.OrderDate <= endDate)
            .Include(so => so.CreatedByUser)
            .Include(so => so.ConfirmedByUser)
            .ToListAsync();

        var report = new SalesSummaryResponse
        {
            FromDate = startDate,
            ToDate = endDate,
            TotalOrders = orders.Count,
            TotalRevenue = orders.Sum(o => o.TotalAmount),
            PendingOrders = orders.Count(o => o.Status == "Pending"),
            ConfirmedOrders = orders.Count(o => o.Status == "Confirmed"),
            ShippedOrders = orders.Count(o => o.Status == "Shipped"),
            DeliveredOrders = orders.Count(o => o.Status == "Delivered"),
            RecentOrders = orders
                .OrderByDescending(o => o.CreatedAt)
                .Take(10)
                .Select(so => new SalesOrderListResponse
                {
                    Id = so.Id,
                    OrderNumber = so.OrderNumber,
                    CustomerName = so.CustomerName,
                    OrderDate = so.OrderDate,
                    DeliveryDate = so.DeliveryDate,
                    TotalAmount = so.TotalAmount,
                    Status = so.Status,
                    PaymentStatus = so.PaymentStatus,
                    CreatedByUserName = so.CreatedByUser.FullName,
                    ConfirmedByUserName = so.ConfirmedByUser != null ? so.ConfirmedByUser.FullName : null
                })
                .ToList()
        };

        return Ok(report);
    }
}

using Microsoft.EntityFrameworkCore;
using Api.Data;
using Api.DTOs;
using Api.Models;
using Api.Services;
using System.Text.Json;

namespace Api.Services;

/// <summary>
/// Comprehensive online order management service
/// </summary>
public class OnlineOrderManager : IOnlineOrderManager
{
    private readonly ApplicationDbContext _context;
    private readonly IAuditService _auditService;
    private readonly IRevenueTrackingService _revenueTrackingService;
    private readonly ILogger<OnlineOrderManager> _logger;

    // Business Rules Configuration
    private readonly Dictionary<string, object> _businessRules = new()
    {
        { "MaxOrderValue", 10000m },
        { "MinOrderValue", 10m },
        { "MaxItemsPerOrder", 50 },
        { "InventoryReservationHours", 24 },
        { "DeliveryTimeframeDays", 7 },
        { "PriorityThresholdValue", 500m },
        { "ExpressDeliveryThreshold", 200m }
    };

    public OnlineOrderManager(
        ApplicationDbContext context,
        IAuditService auditService,
        IRevenueTrackingService revenueTrackingService,
        ILogger<OnlineOrderManager> logger)
    {
        _context = context;
        _auditService = auditService;
        _revenueTrackingService = revenueTrackingService;
        _logger = logger;
    }

    #region Order Processing

    public async Task<OnlineOrderProcessingResult> ProcessNewOrderAsync(CreateOnlineOrderRequest request, int userId)
    {
        var startTime = DateTime.UtcNow;
        var result = new OnlineOrderProcessingResult();

        try
        {
            // Step 1: Validate the order
            var validation = await ValidateOrderAsync(request);
            if (!validation.IsValid)
            {
                result.Success = false;
                result.ErrorMessage = string.Join(", ", validation.Errors);
                return result;
            }

            result.Warnings.AddRange(validation.Warnings);

            // Step 2: Validate business rules
            var businessRulesValidation = await ValidateBusinessRulesAsync(request);
            if (!businessRulesValidation.PassesAllRules)
            {
                var errors = businessRulesValidation.Violations
                    .Where(v => v.Severity == "Error")
                    .Select(v => v.Description);
                result.Success = false;
                result.ErrorMessage = string.Join(", ", errors);
                return result;
            }

            // Step 3: Get online warehouse
            var onlineWarehouse = await GetOnlineWarehouseAsync();
            if (onlineWarehouse == null)
            {
                result.Success = false;
                result.ErrorMessage = "Online warehouse not found";
                return result;
            }

            // Step 4: Generate order number
            var orderNumber = await GenerateOrderNumberAsync();

            // Step 5: Process the order in a transaction
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // Create sales order
                var salesOrder = new SalesOrder
                {
                    OrderNumber = orderNumber,
                    CustomerName = request.CustomerName,
                    CustomerEmail = request.CustomerEmail,
                    CustomerPhone = request.CustomerPhone,
                    CustomerAddress = request.CustomerAddress,
                    OrderDate = DateTime.UtcNow,
                    DeliveryDate = request.DeliveryDate,
                    Status = "Pending",
                    PaymentStatus = "Pending",
                    Notes = request.Notes,
                    CreatedByUserId = userId,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.SalesOrders.Add(salesOrder);
                await _context.SaveChangesAsync();

                // Create sales items and reserve inventory
                var salesItems = new List<SalesItem>();
                decimal totalAmount = 0;

                foreach (var itemRequest in request.Items)
                {
                    // Reserve inventory (includes variant if specified)
                    var inventoryReservation = await ReserveInventoryForItemAsync(
                        salesOrder.Id, itemRequest.ProductId, itemRequest.Quantity, onlineWarehouse.Id, itemRequest.VariantId);

                    if (!inventoryReservation.Success)
                    {
                        await transaction.RollbackAsync();
                        result.Success = false;
                        result.ErrorMessage = inventoryReservation.ErrorMessage;
                        return result;
                    }

                    var itemTotalPrice = itemRequest.Quantity * itemRequest.UnitPrice;
                    totalAmount += itemTotalPrice;

                    var salesItem = new SalesItem
                    {
                        SalesOrderId = salesOrder.Id,
                        ProductId = itemRequest.ProductId,
                        ProductVariantId = itemRequest.VariantId, // Track which variant was ordered
                        WarehouseId = onlineWarehouse.Id,
                        Quantity = itemRequest.Quantity,
                        UnitPrice = itemRequest.UnitPrice,
                        TotalPrice = itemTotalPrice,
                        Unit = itemRequest.Unit,
                        Notes = itemRequest.Notes,
                        CreatedAt = DateTime.UtcNow
                    };

                    salesItems.Add(salesItem);
                }

                salesOrder.TotalAmount = totalAmount;
                _context.SalesItems.AddRange(salesItems);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                // Step 6: Create response
                result.Order = await CreateOrderResponseAsync(salesOrder, salesItems);
                result.Success = true;

                // Step 7: Send notifications
                await SendOrderConfirmationAsync(salesOrder.Id);

                // Step 8: Audit and metrics
                await _auditService.LogAsync("OnlineOrder", salesOrder.Id.ToString(), "CREATE",
                    null, JsonSerializer.Serialize(result.Order), userId);

                result.Metrics = new OrderProcessingMetrics
                {
                    ProcessingTime = DateTime.UtcNow - startTime,
                    ItemsProcessed = salesItems.Count,
                    TotalValue = totalAmount,
                    InventoryReserved = true,
                    PaymentProcessed = false
                };

                _logger.LogInformation("Online order {OrderId} processed successfully in {ProcessingTime}ms",
                    salesOrder.Id, result.Metrics.ProcessingTime.TotalMilliseconds);

                return result;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                throw;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing online order");
            result.Success = false;
            result.ErrorMessage = "An error occurred while processing the order";
            return result;
        }
    }

    public async Task<OrderStatusUpdateResult> UpdateOrderStatusAsync(int orderId, UpdateOnlineOrderStatusRequest request, int userId)
    {
        var result = new OrderStatusUpdateResult();

        try
        {
            var order = await GetOnlineOrderAsync(orderId);
            if (order == null)
            {
                result.Success = false;
                result.ErrorMessage = "Order not found";
                return result;
            }

            result.PreviousStatus = order.Status;

            // Validate status transition
            if (!IsValidStatusTransition(order.Status, request.Status))
            {
                result.Success = false;
                result.ErrorMessage = $"Invalid status transition from {order.Status} to {request.Status}";
                return result;
            }

            // Update order status
            order.Status = request.Status;
            order.UpdatedAt = DateTime.UtcNow;

            if (!string.IsNullOrEmpty(request.Notes))
            {
                order.Notes = string.IsNullOrEmpty(order.Notes) ? request.Notes : $"{order.Notes}\nStatus Update: {request.Notes}";
            }

            // Handle specific status transitions
            switch (request.Status)
            {
                case "Shipped":
                    order.DeliveryDate = request.DeliveryDate ?? DateTime.UtcNow.AddDays(3);
                    result.ActionsPerformed.Add("Delivery date set");
                    break;
                case "Delivered":
                    order.PaymentStatus = "Paid";
                    result.ActionsPerformed.Add("Payment status updated to Paid");
                    break;
                case "Cancelled":
                    await ReleaseInventoryAsync(orderId);
                    result.ActionsPerformed.Add("Inventory released");
                    break;
            }

            await _context.SaveChangesAsync();

            result.Success = true;
            result.NewStatus = request.Status;

            // Send notification
            await SendStatusUpdateNotificationAsync(orderId, request.Status);

            // Audit
            await _auditService.LogAsync("OnlineOrder", orderId.ToString(), "STATUS_UPDATE",
                $"Status changed from {result.PreviousStatus} to {request.Status}",
                JsonSerializer.Serialize(new { Status = request.Status, Notes = request.Notes }), userId);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating order status for order {OrderId}", orderId);
            result.Success = false;
            result.ErrorMessage = "An error occurred while updating the order status";
            return result;
        }
    }

    #endregion

    #region Order Workflow Management

    public async Task<OrderWorkflowResult> AcceptOrderAsync(int orderId, int userId, string? notes = null)
    {
        var result = new OrderWorkflowResult();

        try
        {
            var order = await GetOnlineOrderAsync(orderId);
            if (order == null)
            {
                result.Success = false;
                result.ErrorMessage = "Order not found";
                return result;
            }

            if (order.Status != "Pending")
            {
                result.Success = false;
                result.ErrorMessage = $"Cannot accept order in {order.Status} status";
                return result;
            }

            // Update status
            var statusUpdate = new UpdateOnlineOrderStatusRequest
            {
                Status = "Accepted",
                Notes = notes ?? "Order accepted by manager"
            };

            var statusResult = await UpdateOrderStatusAsync(orderId, statusUpdate, userId);
            if (!statusResult.Success)
            {
                result.Success = false;
                result.ErrorMessage = statusResult.ErrorMessage;
                return result;
            }

            result.Success = true;
            result.Status = "Accepted";
            result.ActionsPerformed.Add("Order accepted");
            result.ActionsPerformed.Add("Inventory confirmed");
            result.EstimatedDeliveryDate = DateTime.UtcNow.AddDays(3);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error accepting order {OrderId}", orderId);
            result.Success = false;
            result.ErrorMessage = "An error occurred while accepting the order";
            return result;
        }
    }

    public async Task<OrderWorkflowResult> ShipOrderAsync(int orderId, int userId, DateTime? deliveryDate = null, string? notes = null)
    {
        var result = new OrderWorkflowResult();

        try
        {
            var order = await GetOnlineOrderAsync(orderId);
            if (order == null)
            {
                result.Success = false;
                result.ErrorMessage = "Order not found";
                return result;
            }

            if (order.Status != "Accepted")
            {
                result.Success = false;
                result.ErrorMessage = $"Cannot ship order in {order.Status} status";
                return result;
            }

            // Update status
            var statusUpdate = new UpdateOnlineOrderStatusRequest
            {
                Status = "Shipped",
                DeliveryDate = deliveryDate ?? DateTime.UtcNow.AddDays(3),
                Notes = notes ?? "Order shipped"
            };

            var statusResult = await UpdateOrderStatusAsync(orderId, statusUpdate, userId);
            if (!statusResult.Success)
            {
                result.Success = false;
                result.ErrorMessage = statusResult.ErrorMessage;
                return result;
            }

            result.Success = true;
            result.Status = "Shipped";
            result.ActionsPerformed.Add("Order shipped");
            result.ActionsPerformed.Add("Tracking information generated");
            result.EstimatedDeliveryDate = deliveryDate ?? DateTime.UtcNow.AddDays(3);

            // Send delivery notification
            await SendDeliveryNotificationAsync(orderId);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error shipping order {OrderId}", orderId);
            result.Success = false;
            result.ErrorMessage = "An error occurred while shipping the order";
            return result;
        }
    }

    public async Task<OrderWorkflowResult> DeliverOrderAsync(int orderId, int userId, string? notes = null)
    {
        var result = new OrderWorkflowResult();

        try
        {
            var order = await GetOnlineOrderAsync(orderId);
            if (order == null)
            {
                result.Success = false;
                result.ErrorMessage = "Order not found";
                return result;
            }

            if (order.Status != "Shipped")
            {
                result.Success = false;
                result.ErrorMessage = $"Cannot deliver order in {order.Status} status";
                return result;
            }

            // Update status
            var statusUpdate = new UpdateOnlineOrderStatusRequest
            {
                Status = "Delivered",
                Notes = notes ?? "Order delivered successfully"
            };

            var statusResult = await UpdateOrderStatusAsync(orderId, statusUpdate, userId);
            if (!statusResult.Success)
            {
                result.Success = false;
                result.ErrorMessage = statusResult.ErrorMessage;
                return result;
            }

            result.Success = true;
            result.Status = "Delivered";
            result.ActionsPerformed.Add("Order delivered");
            result.ActionsPerformed.Add("Payment processed");
            result.ActionsPerformed.Add("Customer satisfaction survey sent");

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error delivering order {OrderId}", orderId);
            result.Success = false;
            result.ErrorMessage = "An error occurred while delivering the order";
            return result;
        }
    }

    public async Task<OrderWorkflowResult> CancelOrderAsync(int orderId, int userId, string reason)
    {
        var result = new OrderWorkflowResult();

        try
        {
            var order = await GetOnlineOrderAsync(orderId);
            if (order == null)
            {
                result.Success = false;
                result.ErrorMessage = "Order not found";
                return result;
            }

            if (order.Status == "Delivered" || order.Status == "Cancelled")
            {
                result.Success = false;
                result.ErrorMessage = $"Cannot cancel order in {order.Status} status";
                return result;
            }

            // Update status
            var statusUpdate = new UpdateOnlineOrderStatusRequest
            {
                Status = "Cancelled",
                Notes = $"Order cancelled: {reason}"
            };

            var statusResult = await UpdateOrderStatusAsync(orderId, statusUpdate, userId);
            if (!statusResult.Success)
            {
                result.Success = false;
                result.ErrorMessage = statusResult.ErrorMessage;
                return result;
            }

            result.Success = true;
            result.Status = "Cancelled";
            result.ActionsPerformed.Add("Order cancelled");
            result.ActionsPerformed.Add("Inventory released");
            result.ActionsPerformed.Add("Refund processed");

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling order {OrderId}", orderId);
            result.Success = false;
            result.ErrorMessage = "An error occurred while cancelling the order";
            return result;
        }
    }

    #endregion

    #region Inventory Management

    public async Task<InventoryCheckResult> CheckInventoryAvailabilityAsync(List<CreateOnlineOrderItemRequest> items)
    {
        var result = new InventoryCheckResult();

        try
        {
            var onlineWarehouse = await GetOnlineWarehouseAsync();
            if (onlineWarehouse == null)
            {
                return result; // Empty result
            }

            foreach (var item in items)
            {
                var inventory = await _context.ProductInventories
                    .FirstOrDefaultAsync(pi => pi.ProductId == item.ProductId && pi.WarehouseId == onlineWarehouse.Id);

                var product = await _context.Products.FindAsync(item.ProductId);

                var availabilityInfo = new InventoryAvailabilityInfo
                {
                    ProductId = item.ProductId,
                    ProductName = product?.Name ?? "Unknown Product",
                    RequestedQuantity = item.Quantity,
                    AvailableQuantity = inventory?.Quantity ?? 0
                };

                result.Items.Add(availabilityInfo);
                result.TotalEstimatedValue += item.Quantity * item.UnitPrice;
            }

            result.AllItemsAvailable = result.Items.All(i => i.IsAvailable);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking inventory availability");
            return result;
        }
    }

    public async Task<InventoryReservationResult> ReserveInventoryAsync(int orderId, List<CreateOnlineOrderItemRequest> items)
    {
        var result = new InventoryReservationResult();

        try
        {
            var onlineWarehouse = await GetOnlineWarehouseAsync();
            if (onlineWarehouse == null)
            {
                result.Success = false;
                result.ErrorMessage = "Online warehouse not found";
                return result;
            }

            foreach (var item in items)
            {
                var reservation = await ReserveInventoryForItemAsync(orderId, item.ProductId, item.Quantity, onlineWarehouse.Id, item.VariantId);
                if (!reservation.Success)
                {
                    result.Success = false;
                    result.ErrorMessage = reservation.ErrorMessage;
                    return result;
                }

                result.Reservations.Add(new InventoryReservation
                {
                    ProductId = item.ProductId,
                    ReservedQuantity = item.Quantity,
                    ReservedUntil = DateTime.UtcNow.AddHours((int)_businessRules["InventoryReservationHours"])
                });
            }

            result.Success = true;
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reserving inventory for order {OrderId}", orderId);
            result.Success = false;
            result.ErrorMessage = "An error occurred while reserving inventory";
            return result;
        }
    }

    public async Task<InventoryReleaseResult> ReleaseInventoryAsync(int orderId)
    {
        var result = new InventoryReleaseResult();

        try
        {
            var order = await GetOnlineOrderAsync(orderId);
            if (order == null)
            {
                result.Success = false;
                result.ErrorMessage = "Order not found";
                return result;
            }

            var salesItems = await _context.SalesItems
                .Where(si => si.SalesOrderId == orderId)
                .ToListAsync();

            var onlineWarehouse = await GetOnlineWarehouseAsync();
            if (onlineWarehouse == null)
            {
                result.Success = false;
                result.ErrorMessage = "Online warehouse not found";
                return result;
            }

            foreach (var item in salesItems)
            {
                // Check if product is AlwaysAvailable (no inventory was deducted)
                var product = await _context.Products.FindAsync(item.ProductId);
                if (product != null && product.AlwaysAvailable)
                {
                    continue; // No inventory to release for AlwaysAvailable products
                }

                if (item.ProductVariantId.HasValue)
                {
                    // Release variant inventory
                    var variantInventory = await _context.VariantInventories
                        .FirstOrDefaultAsync(vi => vi.ProductVariantId == item.ProductVariantId.Value && vi.WarehouseId == onlineWarehouse.Id);

                    if (variantInventory != null)
                    {
                        variantInventory.Quantity += item.Quantity;
                        variantInventory.UpdatedAt = DateTime.UtcNow;

                        _logger.LogInformation("Released {Quantity} to variant inventory {VariantId}. New quantity: {NewQuantity}", 
                            item.Quantity, item.ProductVariantId.Value, variantInventory.Quantity);

                        result.Releases.Add(new InventoryRelease
                        {
                            ProductId = item.ProductId,
                            ReleasedQuantity = item.Quantity
                        });
                    }
                }
                else
                {
                    // Release product inventory
                    var inventory = await _context.ProductInventories
                        .FirstOrDefaultAsync(pi => pi.ProductId == item.ProductId && pi.WarehouseId == onlineWarehouse.Id);

                    if (inventory != null)
                    {
                        inventory.Quantity += item.Quantity;
                        inventory.UpdatedAt = DateTime.UtcNow;

                        _logger.LogInformation("Released {Quantity} to product inventory {ProductId}. New quantity: {NewQuantity}", 
                            item.Quantity, item.ProductId, inventory.Quantity);

                        result.Releases.Add(new InventoryRelease
                        {
                            ProductId = item.ProductId,
                            ReleasedQuantity = item.Quantity
                        });
                    }
                }
            }

            await _context.SaveChangesAsync();
            result.Success = true;

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error releasing inventory for order {OrderId}", orderId);
            result.Success = false;
            result.ErrorMessage = "An error occurred while releasing inventory";
            return result;
        }
    }

    #endregion

    #region Order Analytics and Reporting

    public async Task<OrderAnalyticsResult> GetOrderAnalyticsAsync(DateTime? fromDate = null, DateTime? toDate = null)
    {
        var result = new OrderAnalyticsResult();

        try
        {
            var onlineWarehouse = await GetOnlineWarehouseAsync();
            if (onlineWarehouse == null)
            {
                return result;
            }

            var query = _context.SalesOrders
                .Where(so => so.SalesItems.Any(si => si.WarehouseId == onlineWarehouse.Id));

            if (fromDate.HasValue)
                query = query.Where(so => so.OrderDate >= fromDate.Value);

            if (toDate.HasValue)
                query = query.Where(so => so.OrderDate <= toDate.Value);

            var orders = await query.ToListAsync();

            result.TotalOrders = orders.Count;
            result.PendingOrders = orders.Count(o => o.Status == "Pending");
            result.AcceptedOrders = orders.Count(o => o.Status == "Accepted");
            result.ShippedOrders = orders.Count(o => o.Status == "Shipped");
            result.DeliveredOrders = orders.Count(o => o.Status == "Delivered");
            result.CancelledOrders = orders.Count(o => o.Status == "Cancelled");
            result.TotalRevenue = orders.Where(o => o.Status == "Delivered").Sum(o => o.TotalAmount);
            result.AverageOrderValue = result.TotalOrders > 0 ? result.TotalRevenue / result.TotalOrders : 0;

            // Calculate average processing time
            var deliveredOrders = orders.Where(o => o.Status == "Delivered").ToList();
            if (deliveredOrders.Any())
            {
                var totalProcessingTime = deliveredOrders.Sum(o => ((o.UpdatedAt ?? o.CreatedAt) - o.CreatedAt).Ticks);
                result.AverageProcessingTime = TimeSpan.FromTicks(totalProcessingTime / deliveredOrders.Count);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting order analytics");
            return result;
        }
    }

    public async Task<List<OnlineOrderResponse>> GetOrdersByStatusAsync(string status)
    {
        try
        {
            var onlineWarehouse = await GetOnlineWarehouseAsync();
            if (onlineWarehouse == null)
            {
                return new List<OnlineOrderResponse>();
            }

            var orders = await _context.SalesOrders
                .Include(so => so.SalesItems)
                    .ThenInclude(si => si.Product)
                .Include(so => so.CreatedByUser)
                .Where(so => so.Status == status && so.SalesItems.Any(si => si.WarehouseId == onlineWarehouse.Id))
                .OrderByDescending(so => so.OrderDate)
                .ToListAsync();

            var result = new List<OnlineOrderResponse>();
            foreach (var order in orders)
            {
                var salesItems = order.SalesItems.Where(si => si.WarehouseId == onlineWarehouse.Id).ToList();
                result.Add(await CreateOrderResponseAsync(order, salesItems));
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting orders by status {Status}", status);
            return new List<OnlineOrderResponse>();
        }
    }

    public async Task<List<OnlineOrderResponse>> GetOrdersRequiringAttentionAsync()
    {
        try
        {
            var result = new List<OnlineOrderResponse>();

            // Get pending orders older than 24 hours
            var pendingOrders = await GetOrdersByStatusAsync("Pending");
            var oldPendingOrders = pendingOrders.Where(o => o.OrderDate < DateTime.UtcNow.AddHours(-24)).ToList();
            result.AddRange(oldPendingOrders);

            // Get accepted orders older than 48 hours
            var acceptedOrders = await GetOrdersByStatusAsync("Accepted");
            var oldAcceptedOrders = acceptedOrders.Where(o => o.OrderDate < DateTime.UtcNow.AddHours(-48)).ToList();
            result.AddRange(oldAcceptedOrders);

            // Get shipped orders past delivery date
            var shippedOrders = await GetOrdersByStatusAsync("Shipped");
            var overdueOrders = shippedOrders.Where(o => o.DeliveryDate.HasValue && o.DeliveryDate < DateTime.UtcNow).ToList();
            result.AddRange(overdueOrders);

            return result.DistinctBy(o => o.Id).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting orders requiring attention");
            return new List<OnlineOrderResponse>();
        }
    }

    #endregion

    #region Automation and Notifications

    public async Task<NotificationResult> SendOrderConfirmationAsync(int orderId)
    {
        var result = new NotificationResult { NotificationType = "OrderConfirmation" };

        try
        {
            var order = await GetOnlineOrderAsync(orderId);
            if (order == null)
            {
                result.Success = false;
                result.ErrorMessage = "Order not found";
                return result;
            }

            // In a real implementation, this would send actual notifications
            // For now, we'll just log the notification
            _logger.LogInformation("Order confirmation sent for order {OrderId} to {CustomerEmail}",
                orderId, order.CustomerEmail);

            result.Success = true;
            result.Recipients.Add(order.CustomerEmail ?? "No email provided");

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending order confirmation for order {OrderId}", orderId);
            result.Success = false;
            result.ErrorMessage = "An error occurred while sending the confirmation";
            return result;
        }
    }

    public async Task<NotificationResult> SendStatusUpdateNotificationAsync(int orderId, string newStatus)
    {
        var result = new NotificationResult { NotificationType = "StatusUpdate" };

        try
        {
            var order = await GetOnlineOrderAsync(orderId);
            if (order == null)
            {
                result.Success = false;
                result.ErrorMessage = "Order not found";
                return result;
            }

            _logger.LogInformation("Status update notification sent for order {OrderId} - Status: {Status}",
                orderId, newStatus);

            result.Success = true;
            result.Recipients.Add(order.CustomerEmail ?? "No email provided");

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending status update notification for order {OrderId}", orderId);
            result.Success = false;
            result.ErrorMessage = "An error occurred while sending the notification";
            return result;
        }
    }

    public async Task<NotificationResult> SendDeliveryNotificationAsync(int orderId)
    {
        var result = new NotificationResult { NotificationType = "DeliveryNotification" };

        try
        {
            var order = await GetOnlineOrderAsync(orderId);
            if (order == null)
            {
                result.Success = false;
                result.ErrorMessage = "Order not found";
                return result;
            }

            _logger.LogInformation("Delivery notification sent for order {OrderId}",
                orderId);

            result.Success = true;
            result.Recipients.Add(order.CustomerEmail ?? "No email provided");

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending delivery notification for order {OrderId}", orderId);
            result.Success = false;
            result.ErrorMessage = "An error occurred while sending the notification";
            return result;
        }
    }

    #endregion

    #region Business Rules and Validation

    public async Task<BusinessRuleValidationResult> ValidateBusinessRulesAsync(CreateOnlineOrderRequest request)
    {
        var result = new BusinessRuleValidationResult();

        try
        {
            // Calculate total order value
            var totalValue = request.Items.Sum(i => i.Quantity * i.UnitPrice);

            // Check minimum order value
            if (totalValue < (decimal)_businessRules["MinOrderValue"])
            {
                result.Violations.Add(new BusinessRuleViolation
                {
                    RuleName = "MinimumOrderValue",
                    Description = $"Order value must be at least ${_businessRules["MinOrderValue"]}",
                    Severity = "Error"
                });
            }

            // Check maximum order value
            if (totalValue > (decimal)_businessRules["MaxOrderValue"])
            {
                result.Violations.Add(new BusinessRuleViolation
                {
                    RuleName = "MaximumOrderValue",
                    Description = $"Order value cannot exceed ${_businessRules["MaxOrderValue"]}",
                    Severity = "Error"
                });
            }

            // Check maximum items per order
            if (request.Items.Count > (int)_businessRules["MaxItemsPerOrder"])
            {
                result.Violations.Add(new BusinessRuleViolation
                {
                    RuleName = "MaximumItemsPerOrder",
                    Description = $"Order cannot contain more than {_businessRules["MaxItemsPerOrder"]} items",
                    Severity = "Error"
                });
            }

            // Check for duplicate products
            var duplicateProducts = request.Items.GroupBy(i => i.ProductId)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key);

            if (duplicateProducts.Any())
            {
                result.Violations.Add(new BusinessRuleViolation
                {
                    RuleName = "DuplicateProducts",
                    Description = "Order contains duplicate products",
                    Severity = "Warning"
                });
            }

            // Check delivery date
            if (request.DeliveryDate.HasValue && request.DeliveryDate < DateTime.UtcNow.AddDays(1))
            {
                result.Violations.Add(new BusinessRuleViolation
                {
                    RuleName = "DeliveryDateTooSoon",
                    Description = "Delivery date must be at least 1 day in the future",
                    Severity = "Warning"
                });
            }

            result.PassesAllRules = !result.Violations.Any(v => v.Severity == "Error");

            // Add recommendations
            if (totalValue >= (decimal)_businessRules["PriorityThresholdValue"])
            {
                result.Recommendations.Add("High-value order - consider priority processing");
            }

            if (totalValue >= (decimal)_businessRules["ExpressDeliveryThreshold"])
            {
                result.Recommendations.Add("Consider express delivery option");
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating business rules");
            result.PassesAllRules = false;
            result.Violations.Add(new BusinessRuleViolation
            {
                RuleName = "ValidationError",
                Description = "An error occurred during validation",
                Severity = "Error"
            });
            return result;
        }
    }

    public async Task<OrderPriorityResult> CalculateOrderPriorityAsync(CreateOnlineOrderRequest request)
    {
        var result = new OrderPriorityResult();

        try
        {
            var totalValue = request.Items.Sum(i => i.Quantity * i.UnitPrice);
            var priority = 5; // Base priority
            var factors = new List<string>();

            // High value orders get higher priority
            if (totalValue >= (decimal)_businessRules["PriorityThresholdValue"])
            {
                priority += 3;
                factors.Add("High order value");
            }

            // Express delivery requests get higher priority
            if (request.DeliveryDate.HasValue && request.DeliveryDate < DateTime.UtcNow.AddDays(3))
            {
                priority += 2;
                factors.Add("Express delivery requested");
            }

            // Large orders get slightly higher priority
            if (request.Items.Count > 10)
            {
                priority += 1;
                factors.Add("Large order");
            }

            // VIP customers (if we had customer data) would get higher priority
            // This is a placeholder for future enhancement

            result.Priority = Math.Min(10, Math.Max(1, priority));
            result.PriorityReason = priority >= 8 ? "High Priority" : priority >= 6 ? "Medium Priority" : "Standard Priority";
            result.Factors = factors;

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating order priority");
            result.Priority = 5;
            result.PriorityReason = "Standard Priority";
            return result;
        }
    }

    #endregion

    #region Helper Methods

    public async Task<OrderValidationResult> ValidateOrderAsync(CreateOnlineOrderRequest request)
    {
        var result = new OrderValidationResult();

        try
        {
            // Basic validation
            if (string.IsNullOrWhiteSpace(request.CustomerName))
            {
                result.Errors.Add("Customer name is required");
            }

            if (request.Items == null || !request.Items.Any())
            {
                result.Errors.Add("Order must contain at least one item");
            }

            // Validate each item
            foreach (var item in request.Items)
            {
                if (item.ProductId <= 0)
                {
                    result.Errors.Add("Invalid product ID");
                }

                if (item.Quantity <= 0)
                {
                    result.Errors.Add("Quantity must be greater than 0");
                }

                if (item.UnitPrice <= 0)
                {
                    result.Errors.Add("Unit price must be greater than 0");
                }
            }

            // Check inventory availability
            var inventoryCheck = await CheckInventoryAvailabilityAsync(request.Items);
            result.InventoryInfo = inventoryCheck.Items;

            foreach (var item in inventoryCheck.Items)
            {
                if (!item.IsAvailable)
                {
                    result.Errors.Add($"Insufficient inventory for {item.ProductName}. Available: {item.AvailableQuantity}, Requested: {item.RequestedQuantity}");
                }
            }

            result.CalculatedTotal = request.Items.Sum(i => i.Quantity * i.UnitPrice);
            result.IsValid = !result.Errors.Any();

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating order");
            result.Errors.Add("An error occurred during validation");
            result.IsValid = false;
            return result;
        }
    }

    private async Task<SalesOrder?> GetOnlineOrderAsync(int orderId)
    {
        var onlineWarehouse = await GetOnlineWarehouseAsync();
        if (onlineWarehouse == null)
        {
            return null;
        }

        return await _context.SalesOrders
            .FirstOrDefaultAsync(so => so.Id == orderId && so.SalesItems.Any(si => si.WarehouseId == onlineWarehouse.Id));
    }

    private async Task<Warehouse?> GetOnlineWarehouseAsync()
    {
        return await _context.Warehouses.FirstOrDefaultAsync(w => w.Name == "Online Store");
    }

    private async Task<string> GenerateOrderNumberAsync()
    {
        var today = DateTime.UtcNow;
        var prefix = $"ON{today:yyyyMMdd}";
        var count = await _context.SalesOrders.CountAsync(so => so.OrderNumber.StartsWith(prefix)) + 1;
        return $"{prefix}{count:D4}";
    }

    private async Task<OnlineOrderResponse> CreateOrderResponseAsync(SalesOrder order, List<SalesItem> salesItems)
    {
        var createdByUser = await _context.Users.FindAsync(order.CreatedByUserId);

        return new OnlineOrderResponse
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
            CreatedByUserName = createdByUser?.FullName ?? "Unknown",
            Items = salesItems.Select(si => new OnlineOrderItemResponse
            {
                Id = si.Id,
                ProductId = si.ProductId,
                ProductName = _context.Products.Find(si.ProductId)?.Name ?? "Unknown Product",
                ProductSKU = _context.Products.Find(si.ProductId)?.SKU ?? "",
                Quantity = si.Quantity,
                UnitPrice = si.UnitPrice,
                TotalPrice = si.TotalPrice,
                Unit = si.Unit
            }).ToList()
        };
    }

    private bool IsValidStatusTransition(string currentStatus, string newStatus)
    {
        var validTransitions = new Dictionary<string, string[]>
        {
            { "Pending", new[] { "Accepted", "Cancelled" } },
            { "Accepted", new[] { "Shipped", "Cancelled" } },
            { "Shipped", new[] { "Delivered", "Cancelled" } },
            { "Delivered", new string[0] },
            { "Cancelled", new string[0] }
        };

        return validTransitions.ContainsKey(currentStatus) && validTransitions[currentStatus].Contains(newStatus);
    }

    private async Task<InventoryReservationResult> ReserveInventoryForItemAsync(int orderId, int productId, decimal quantity, int warehouseId, int? variantId = null)
    {
        var result = new InventoryReservationResult();

        try
        {
            // Check if product is AlwaysAvailable
            var product = await _context.Products.FindAsync(productId);
            if (product != null && product.AlwaysAvailable)
            {
                // Skip inventory reservation for AlwaysAvailable products
                result.Success = true;
                result.Reservations.Add(new InventoryReservation
                {
                    ProductId = productId,
                    ReservedQuantity = quantity,
                    ReservedUntil = DateTime.UtcNow.AddHours((int)_businessRules["InventoryReservationHours"])
                });
                return result;
            }

            if (variantId.HasValue)
            {
                // Reserve from variant inventory
                var variantInventory = await _context.VariantInventories
                    .FirstOrDefaultAsync(vi => vi.ProductVariantId == variantId.Value && vi.WarehouseId == warehouseId);

                if (variantInventory == null)
                {
                    result.Success = false;
                    result.ErrorMessage = $"Variant {variantId.Value} not found in online inventory";
                    return result;
                }

                if (variantInventory.Quantity < quantity)
                {
                    result.Success = false;
                    result.ErrorMessage = $"Insufficient inventory for variant {variantId.Value}. Available: {variantInventory.Quantity}, Requested: {quantity}";
                    return result;
                }

                // Reserve inventory
                variantInventory.Quantity -= quantity;
                variantInventory.UpdatedAt = DateTime.UtcNow;

                _logger.LogInformation("Reserved {Quantity} from variant inventory {VariantId}. New quantity: {NewQuantity}", 
                    quantity, variantId.Value, variantInventory.Quantity);
            }
            else
            {
                // Reserve from product inventory
                var inventory = await _context.ProductInventories
                    .FirstOrDefaultAsync(pi => pi.ProductId == productId && pi.WarehouseId == warehouseId);

                if (inventory == null)
                {
                    result.Success = false;
                    result.ErrorMessage = $"Product {productId} not found in online inventory";
                    return result;
                }

                if (inventory.Quantity < quantity)
                {
                    result.Success = false;
                    result.ErrorMessage = $"Insufficient inventory for product {productId}. Available: {inventory.Quantity}, Requested: {quantity}";
                    return result;
                }

                // Reserve inventory
                inventory.Quantity -= quantity;
                inventory.UpdatedAt = DateTime.UtcNow;

                _logger.LogInformation("Reserved {Quantity} from product inventory {ProductId}. New quantity: {NewQuantity}", 
                    quantity, productId, inventory.Quantity);
            }

            result.Success = true;
            result.Reservations.Add(new InventoryReservation
            {
                ProductId = productId,
                ReservedQuantity = quantity,
                ReservedUntil = DateTime.UtcNow.AddHours((int)_businessRules["InventoryReservationHours"])
            });

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reserving inventory for product {ProductId}", productId);
            result.Success = false;
            result.ErrorMessage = "An error occurred while reserving inventory";
            return result;
        }
    }

    #endregion
}

using Api.DTOs;
using Api.Models;

namespace Api.Services;

/// <summary>
/// Interface for managing online order processing workflows
/// </summary>
public interface IOnlineOrderManager
{
    // Order Processing
    Task<OnlineOrderProcessingResult> ProcessNewOrderAsync(CreateOnlineOrderRequest request, int userId);
    Task<OrderStatusUpdateResult> UpdateOrderStatusAsync(int orderId, UpdateOnlineOrderStatusRequest request, int userId);
    Task<OrderValidationResult> ValidateOrderAsync(CreateOnlineOrderRequest request);
    
    // Order Workflow Management
    Task<OrderWorkflowResult> AcceptOrderAsync(int orderId, int userId, string? notes = null);
    Task<OrderWorkflowResult> ShipOrderAsync(int orderId, int userId, DateTime? deliveryDate = null, string? notes = null);
    Task<OrderWorkflowResult> DeliverOrderAsync(int orderId, int userId, string? notes = null);
    Task<OrderWorkflowResult> CancelOrderAsync(int orderId, int userId, string reason);
    
    // Inventory Management
    Task<InventoryCheckResult> CheckInventoryAvailabilityAsync(List<CreateOnlineOrderItemRequest> items);
    Task<InventoryReservationResult> ReserveInventoryAsync(int orderId, List<CreateOnlineOrderItemRequest> items);
    Task<InventoryReleaseResult> ReleaseInventoryAsync(int orderId);
    
    // Order Analytics and Reporting
    Task<OrderAnalyticsResult> GetOrderAnalyticsAsync(DateTime? fromDate = null, DateTime? toDate = null);
    Task<List<OnlineOrderResponse>> GetOrdersByStatusAsync(string status);
    Task<List<OnlineOrderResponse>> GetOrdersRequiringAttentionAsync();
    
    // Automation and Notifications
    Task<NotificationResult> SendOrderConfirmationAsync(int orderId);
    Task<NotificationResult> SendStatusUpdateNotificationAsync(int orderId, string newStatus);
    Task<NotificationResult> SendDeliveryNotificationAsync(int orderId);
    
    // Business Rules and Validation
    Task<BusinessRuleValidationResult> ValidateBusinessRulesAsync(CreateOnlineOrderRequest request);
    Task<OrderPriorityResult> CalculateOrderPriorityAsync(CreateOnlineOrderRequest request);
}

// Result Classes
public class OnlineOrderProcessingResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public OnlineOrderResponse? Order { get; set; }
    public List<string> Warnings { get; set; } = new List<string>();
    public OrderProcessingMetrics? Metrics { get; set; }
}

public class OrderStatusUpdateResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public string? PreviousStatus { get; set; }
    public string? NewStatus { get; set; }
    public List<string> ActionsPerformed { get; set; } = new List<string>();
}

public class OrderValidationResult
{
    public bool IsValid { get; set; }
    public List<string> Errors { get; set; } = new List<string>();
    public List<string> Warnings { get; set; } = new List<string>();
    public decimal CalculatedTotal { get; set; }
    public List<InventoryAvailabilityInfo> InventoryInfo { get; set; } = new List<InventoryAvailabilityInfo>();
}

public class OrderWorkflowResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public string? Status { get; set; }
    public List<string> ActionsPerformed { get; set; } = new List<string>();
    public DateTime? EstimatedDeliveryDate { get; set; }
}

public class InventoryCheckResult
{
    public bool AllItemsAvailable { get; set; }
    public List<InventoryAvailabilityInfo> Items { get; set; } = new List<InventoryAvailabilityInfo>();
    public decimal TotalEstimatedValue { get; set; }
}

public class InventoryAvailabilityInfo
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public decimal RequestedQuantity { get; set; }
    public decimal AvailableQuantity { get; set; }
    public bool IsAvailable => AvailableQuantity >= RequestedQuantity;
    public decimal Shortage => Math.Max(0, RequestedQuantity - AvailableQuantity);
}

public class InventoryReservationResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public List<InventoryReservation> Reservations { get; set; } = new List<InventoryReservation>();
}

public class InventoryReservation
{
    public int ProductId { get; set; }
    public decimal ReservedQuantity { get; set; }
    public DateTime ReservedUntil { get; set; }
}

public class InventoryReleaseResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public List<InventoryRelease> Releases { get; set; } = new List<InventoryRelease>();
}

public class InventoryRelease
{
    public int ProductId { get; set; }
    public decimal ReleasedQuantity { get; set; }
}

public class OrderAnalyticsResult
{
    public int TotalOrders { get; set; }
    public int PendingOrders { get; set; }
    public int AcceptedOrders { get; set; }
    public int ShippedOrders { get; set; }
    public int DeliveredOrders { get; set; }
    public int CancelledOrders { get; set; }
    public decimal TotalRevenue { get; set; }
    public decimal AverageOrderValue { get; set; }
    public TimeSpan AverageProcessingTime { get; set; }
    public List<OrderTrendData> Trends { get; set; } = new List<OrderTrendData>();
}

public class OrderTrendData
{
    public DateTime Date { get; set; }
    public int OrderCount { get; set; }
    public decimal Revenue { get; set; }
}

public class NotificationResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public string? NotificationType { get; set; }
    public List<string> Recipients { get; set; } = new List<string>();
}

public class BusinessRuleValidationResult
{
    public bool PassesAllRules { get; set; }
    public List<BusinessRuleViolation> Violations { get; set; } = new List<BusinessRuleViolation>();
    public List<string> Recommendations { get; set; } = new List<string>();
}

public class BusinessRuleViolation
{
    public string RuleName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty; // Error, Warning, Info
}

public class OrderPriorityResult
{
    public int Priority { get; set; } // 1-10, 10 being highest
    public string PriorityReason { get; set; } = string.Empty;
    public List<string> Factors { get; set; } = new List<string>();
}

public class OrderProcessingMetrics
{
    public TimeSpan ProcessingTime { get; set; }
    public int ItemsProcessed { get; set; }
    public decimal TotalValue { get; set; }
    public bool InventoryReserved { get; set; }
    public bool PaymentProcessed { get; set; }
}

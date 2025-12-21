using System.Text.Json;

namespace Api.DTOs
{
    public class CreateSalesOrderRequest
    {
        public string? CustomerName { get; set; }
        public string? CustomerAddress { get; set; }
        public string? CustomerPhone { get; set; }
        public string? CustomerEmail { get; set; }
        public DateTime? DeliveryDate { get; set; }
        public string? Notes { get; set; }
        public List<CreateSalesItemRequest> Items { get; set; } = new List<CreateSalesItemRequest>();
    }

    public class CreateSalesItemRequest
    {
        public int ProductId { get; set; }
        public int WarehouseId { get; set; }
        public decimal Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public string? Unit { get; set; }
        public string? Notes { get; set; }
    }

    public class UpdateSalesOrderRequest
    {
        public string? CustomerName { get; set; }
        public string? CustomerAddress { get; set; }
        public string? CustomerPhone { get; set; }
        public string? CustomerEmail { get; set; }
        public DateTime? DeliveryDate { get; set; }
        public string? Status { get; set; }
        public string? PaymentStatus { get; set; }
        public decimal? DownPayment { get; set; }
        public string? Notes { get; set; }
    }

    public class UpdateSalesItemRequest
    {
        public decimal? Quantity { get; set; }
        public decimal? UnitPrice { get; set; }
        public string? Unit { get; set; }
        public string? Notes { get; set; }
    }

    public class SalesOrderResponse
    {
        public int Id { get; set; }
        public string OrderNumber { get; set; } = string.Empty;
        public string? CustomerName { get; set; }
        public string? CustomerAddress { get; set; }
        public string? CustomerPhone { get; set; }
        public string? CustomerEmail { get; set; }
        public DateTime OrderDate { get; set; }
        public DateTime? DeliveryDate { get; set; }
        public decimal TotalAmount { get; set; }
        public string Status { get; set; } = string.Empty;
        public string PaymentStatus { get; set; } = string.Empty;
        public string? Notes { get; set; }
        public int CreatedByUserId { get; set; }
        public string CreatedByUserName { get; set; } = string.Empty;
        public int? ConfirmedByUserId { get; set; }
        public string? ConfirmedByUserName { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public List<SalesItemResponse> Items { get; set; } = new List<SalesItemResponse>();
    }

    public class SalesOrderListResponse
    {
        public int Id { get; set; }
        public string OrderNumber { get; set; } = string.Empty;
        public string? CustomerName { get; set; }
        public string? CustomerEmail { get; set; }
        public string? CustomerPhone { get; set; }
        public string? CustomerAddress { get; set; }
        public DateTime OrderDate { get; set; }
        public DateTime? DeliveryDate { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal? DownPayment { get; set; }
        public string Status { get; set; } = string.Empty;
        public string PaymentStatus { get; set; } = string.Empty;
        public string? Notes { get; set; }
        public string CreatedByUserName { get; set; } = string.Empty;
        public string? ConfirmedByUserName { get; set; }
        public List<SalesItemResponse> Items { get; set; } = new List<SalesItemResponse>();
        // Promo code information
        public PromoCodeUsageInfo? PromoCode { get; set; }
    }

    public class PromoCodeUsageInfo
    {
        public int PromoCodeId { get; set; }
        public string Code { get; set; } = string.Empty;
        public decimal DiscountAmount { get; set; }
        public DateTime UsedAt { get; set; }
        public int? UserId { get; set; }
        public string? UserName { get; set; }
    }

    public class SalesItemResponse
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public int? ProductVariantId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string ProductSKU { get; set; } = string.Empty;
        public string ProductDescription { get; set; } = string.Empty;
        public string? VariantAttributes { get; set; }
        public int WarehouseId { get; set; }
        public string WarehouseName { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal TotalPrice { get; set; }
        public string? Unit { get; set; }
        public string? Notes { get; set; }
        public decimal AvailableQuantity { get; set; } // Available stock for this product in this warehouse
    }

    public class ConfirmSalesOrderRequest
    {
        public string? Notes { get; set; }
    }

    public class ShipSalesOrderRequest
    {
        public DateTime? DeliveryDate { get; set; }
        public string? Notes { get; set; }
    }

    public class DeliverSalesOrderRequest
    {
        public DateTime? ActualDeliveryDate { get; set; }
        public string? Notes { get; set; }
    }

    public class ProductCardResponse
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public decimal Price { get; set; }
        public string? Unit { get; set; }
        public string? SKU { get; set; }
        public string? Brand { get; set; }
        public string? ImageUrl { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public List<ProductWarehouseStock> WarehouseStocks { get; set; } = new List<ProductWarehouseStock>();
    }

    public class ProductWarehouseStock
    {
        public int WarehouseId { get; set; }
        public string WarehouseName { get; set; } = string.Empty;
        public decimal AvailableQuantity { get; set; }
        public string? Unit { get; set; }
        public decimal? MinimumStockLevel { get; set; }
        public bool IsLowStock { get; set; }
    }

    public class SalesSummaryResponse
    {
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public int TotalOrders { get; set; }
        public decimal TotalRevenue { get; set; }
        public int PendingOrders { get; set; }
        public int ConfirmedOrders { get; set; }
        public int ShippedOrders { get; set; }
        public int DeliveredOrders { get; set; }
        public List<SalesOrderListResponse> RecentOrders { get; set; } = new List<SalesOrderListResponse>();
    }

    // POS System DTOs
    public class POSProductResponse
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public decimal AvailableQuantity { get; set; }
        public string Unit { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public string? SKU { get; set; }
        public bool IsAssemblyOffer { get; set; } = false;
        public int? AssemblyId { get; set; }
    }

    public class POSSaleRequest
    {
        public string? CustomerName { get; set; }
        public string? CustomerEmail { get; set; }
        public string? CustomerPhone { get; set; }
        public string? CustomerAddress { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal? DiscountAmount { get; set; }
        public decimal? TaxAmount { get; set; }
        public decimal FinalAmount { get; set; }
        public string PaymentMethod { get; set; } = "Cash";
        public string? Notes { get; set; }
        public List<POSItemRequest> Items { get; set; } = new List<POSItemRequest>();
    }

    public class POSItemRequest
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal TotalPrice { get; set; }
    }

    public class POSSaleResponse
    {
        public string SaleNumber { get; set; } = string.Empty;
        public int SaleId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal FinalAmount { get; set; }
        public string PaymentMethod { get; set; } = string.Empty;
        public List<POSItemResponse> Items { get; set; } = new List<POSItemResponse>();
        public DateTime SaleDate { get; set; }
        public string CashierName { get; set; } = string.Empty;
        public string StoreName { get; set; } = string.Empty;
    }

    public class POSItemResponse
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal TotalPrice { get; set; }
    }

    public class POSSaleHistoryResponse
    {
        public int SaleId { get; set; }
        public string SaleNumber { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
        public decimal FinalAmount { get; set; }
        public string PaymentMethod { get; set; } = string.Empty;
        public DateTime SaleDate { get; set; }
        public string CashierName { get; set; } = string.Empty;
        public int ItemCount { get; set; }
    }

    public class CustomerSummaryResponse
    {
        public string? Email { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public string? Address { get; set; }
        public bool IsRegistered { get; set; }
        public int? UserId { get; set; }
        public int OrderCount { get; set; }
        public decimal TotalSpent { get; set; }
        public DateTime FirstOrderDate { get; set; }
        public DateTime? LastOrderDate { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}

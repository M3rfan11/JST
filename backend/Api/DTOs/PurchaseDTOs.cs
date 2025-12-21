namespace Api.DTOs
{
    public class CreatePurchaseOrderRequest
    {
        public string? SupplierName { get; set; }
        public string? SupplierAddress { get; set; }
        public string? SupplierPhone { get; set; }
        public string? SupplierEmail { get; set; }
        public DateTime? ExpectedDeliveryDate { get; set; }
        public string? Notes { get; set; }
        public List<CreatePurchaseItemRequest> Items { get; set; } = new List<CreatePurchaseItemRequest>();
    }

    public class CreatePurchaseItemRequest
    {
        public int ProductId { get; set; }
        public int WarehouseId { get; set; }
        public decimal Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public string? Unit { get; set; }
        public string? Notes { get; set; }
    }

    public class UpdatePurchaseOrderRequest
    {
        public string? SupplierName { get; set; }
        public string? SupplierAddress { get; set; }
        public string? SupplierPhone { get; set; }
        public string? SupplierEmail { get; set; }
        public DateTime? ExpectedDeliveryDate { get; set; }
        public DateTime? ActualDeliveryDate { get; set; }
        public string? Status { get; set; }
        public string? Notes { get; set; }
    }

    public class UpdatePurchaseItemRequest
    {
        public decimal? Quantity { get; set; }
        public decimal? UnitPrice { get; set; }
        public string? Unit { get; set; }
        public string? Notes { get; set; }
    }

    public class PurchaseOrderResponse
    {
        public int Id { get; set; }
        public string OrderNumber { get; set; } = string.Empty;
        public string? SupplierName { get; set; }
        public string? SupplierAddress { get; set; }
        public string? SupplierPhone { get; set; }
        public string? SupplierEmail { get; set; }
        public DateTime OrderDate { get; set; }
        public DateTime? ExpectedDeliveryDate { get; set; }
        public DateTime? ActualDeliveryDate { get; set; }
        public decimal TotalAmount { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? Notes { get; set; }
        public int CreatedByUserId { get; set; }
        public string CreatedByUserName { get; set; } = string.Empty;
        public int? ApprovedByUserId { get; set; }
        public string? ApprovedByUserName { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public List<PurchaseItemResponse> Items { get; set; } = new List<PurchaseItemResponse>();
    }

    public class PurchaseOrderListResponse
    {
        public int Id { get; set; }
        public string OrderNumber { get; set; } = string.Empty;
        public string? SupplierName { get; set; }
        public DateTime OrderDate { get; set; }
        public DateTime? ExpectedDeliveryDate { get; set; }
        public DateTime? ActualDeliveryDate { get; set; }
        public decimal TotalAmount { get; set; }
        public string Status { get; set; } = string.Empty;
        public string CreatedByUserName { get; set; } = string.Empty;
        public string? ApprovedByUserName { get; set; }
    }

    public class PurchaseItemResponse
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string ProductSKU { get; set; } = string.Empty;
        public int WarehouseId { get; set; }
        public string WarehouseName { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal TotalPrice { get; set; }
        public string? Unit { get; set; }
        public string? Notes { get; set; }
    }

    public class ReceivePurchaseOrderRequest
    {
        public DateTime? ActualDeliveryDate { get; set; }
        public string? Notes { get; set; }
    }

    public class ApprovePurchaseOrderRequest
    {
        public string? Notes { get; set; }
    }
}

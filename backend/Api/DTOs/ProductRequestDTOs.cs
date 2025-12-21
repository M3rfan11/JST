namespace Api.DTOs
{
    public class CreateProductRequestRequest
    {
        public int WarehouseId { get; set; }
        public string? Notes { get; set; }
        public List<CreateProductRequestItemRequest> Items { get; set; } = new List<CreateProductRequestItemRequest>();
    }

    public class CreateProductRequestItemRequest
    {
        public int ProductId { get; set; }
        public decimal QuantityRequested { get; set; }
        public string? Unit { get; set; }
        public string? Notes { get; set; }
    }

    public class UpdateProductRequestRequest
    {
        public int? WarehouseId { get; set; }
        public string? Notes { get; set; }
    }

    public class UpdateProductRequestItemRequest
    {
        public decimal? QuantityRequested { get; set; }
        public string? Unit { get; set; }
        public string? Notes { get; set; }
    }

    public class ProductRequestResponse
    {
        public int Id { get; set; }
        public int RequestedByUserId { get; set; }
        public string RequestedByUserName { get; set; } = string.Empty;
        public DateTime RequestDate { get; set; }
        public string Status { get; set; } = string.Empty;
        public int WarehouseId { get; set; }
        public string WarehouseName { get; set; } = string.Empty;
        public string? Notes { get; set; }
        public int? ApprovedByUserId { get; set; }
        public string? ApprovedByUserName { get; set; }
        public DateTime? ApprovedAt { get; set; }
        public DateTime? RejectedAt { get; set; }
        public string? RejectionReason { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public List<ProductRequestItemResponse> Items { get; set; } = new List<ProductRequestItemResponse>();
        public int TotalItems { get; set; }
        public decimal TotalQuantityRequested { get; set; }
    }

    public class ProductRequestListResponse
    {
        public int Id { get; set; }
        public string RequestedByUserName { get; set; } = string.Empty;
        public DateTime RequestDate { get; set; }
        public string Status { get; set; } = string.Empty;
        public string WarehouseName { get; set; } = string.Empty;
        public string? ApprovedByUserName { get; set; }
        public DateTime? ApprovedAt { get; set; }
        public int ItemCount { get; set; }
        public decimal TotalQuantityRequested { get; set; }
    }

    public class ProductRequestItemResponse
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string ProductSKU { get; set; } = string.Empty;
        public decimal QuantityRequested { get; set; }
        public decimal? QuantityApproved { get; set; }
        public decimal? QuantityReceived { get; set; }
        public string? Unit { get; set; }
        public string? Notes { get; set; }
        public bool IsFullyApproved { get; set; }
        public bool IsFullyReceived { get; set; }
    }

    public class ApproveProductRequestRequest
    {
        public List<ApproveProductRequestItemRequest> Items { get; set; } = new List<ApproveProductRequestItemRequest>();
        public string? Notes { get; set; }
    }

    public class ApproveProductRequestItemRequest
    {
        public int ItemId { get; set; }
        public decimal? QuantityApproved { get; set; } // If null, approves full requested quantity
        public string? Notes { get; set; }
    }

    public class RejectProductRequestRequest
    {
        public string RejectionReason { get; set; } = string.Empty;
        public string? Notes { get; set; }
    }

    public class ReceiveProductRequestItemRequest
    {
        public int ItemId { get; set; }
        public decimal QuantityReceived { get; set; }
        public string? Notes { get; set; }
    }

    public class ProductRequestReportResponse
    {
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public int TotalRequests { get; set; }
        public int PendingRequests { get; set; }
        public int ApprovedRequests { get; set; }
        public int RejectedRequests { get; set; }
        public int FullyReceivedRequests { get; set; }
        public int PartiallyReceivedRequests { get; set; }
        public decimal TotalQuantityRequested { get; set; }
        public decimal TotalQuantityApproved { get; set; }
        public decimal TotalQuantityReceived { get; set; }
        public List<ProductRequestListResponse> RecentRequests { get; set; } = new List<ProductRequestListResponse>();
    }

    public class ProductRequestSummaryResponse
    {
        public int RequestId { get; set; }
        public string RequestedByUserName { get; set; } = string.Empty;
        public DateTime RequestDate { get; set; }
        public string Status { get; set; } = string.Empty;
        public string WarehouseName { get; set; } = string.Empty;
        public int ItemCount { get; set; }
        public int TotalRequests { get; set; }
        public decimal TotalQuantityRequested { get; set; }
        public decimal TotalQuantityApproved { get; set; }
        public decimal TotalQuantityReceived { get; set; }
        public decimal ApprovalRate { get; set; } // Percentage of requested quantity approved
        public decimal ReceiptRate { get; set; } // Percentage of approved quantity received
    }

    public class ProductRequestStatsResponse
    {
        public int TotalRequests { get; set; }
        public int PendingRequests { get; set; }
        public int ApprovedRequests { get; set; }
        public int RejectedRequests { get; set; }
        public int FullyReceivedRequests { get; set; }
        public int PartiallyReceivedRequests { get; set; }
        public decimal AverageApprovalTime { get; set; } // In hours
        public decimal AverageReceiptTime { get; set; } // In hours
        public List<ProductRequestSummaryResponse> TopRequesters { get; set; } = new List<ProductRequestSummaryResponse>();
        public List<ProductRequestSummaryResponse> RecentRequests { get; set; } = new List<ProductRequestSummaryResponse>();
    }
}

namespace Api.DTOs
{
    public class CreateProductAssemblyRequest
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public decimal Quantity { get; set; }
        public string? Unit { get; set; }
        public string? Instructions { get; set; }
        public string? Notes { get; set; }
        public int? StoreId { get; set; }
        public bool IsActive { get; set; } = true;
        public decimal? SalePrice { get; set; }
        public List<CreateBillOfMaterialRequest> BillOfMaterials { get; set; } = new List<CreateBillOfMaterialRequest>();
    }

    public class CreateBillOfMaterialRequest
    {
        public int RawProductId { get; set; }
        public decimal RequiredQuantity { get; set; }
        public string? Unit { get; set; }
        public string? Notes { get; set; }
    }

    public class CreateAssemblyOfferRequest
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public decimal? SalePrice { get; set; } // Custom price for the offer
        public int? StoreId { get; set; } // Store ID for the assembly offer
        public List<AssemblyOfferItem> Items { get; set; } = new List<AssemblyOfferItem>();
    }

    public class AssemblyOfferItem
    {
        public int ProductId { get; set; }
        public decimal Quantity { get; set; }
        public string? Notes { get; set; }
    }

    public class UpdateProductAssemblyRequest
    {
        public string? Name { get; set; }
        public string? Description { get; set; }
        public decimal? Quantity { get; set; }
        public string? Unit { get; set; }
        public string? Instructions { get; set; }
        public string? Status { get; set; }
        public string? Notes { get; set; }
        public bool? IsActive { get; set; }
        public decimal? SalePrice { get; set; }
    }

    public class UpdateBillOfMaterialRequest
    {
        public decimal? RequiredQuantity { get; set; }
        public string? Unit { get; set; }
        public string? Notes { get; set; }
    }

    public class ProductAssemblyResponse
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public decimal Quantity { get; set; }
        public string? Unit { get; set; }
        public string? Instructions { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? Notes { get; set; }
        public int CreatedByUserId { get; set; }
        public string CreatedByUserName { get; set; } = string.Empty;
        public int? CompletedByUserId { get; set; }
        public string? CompletedByUserName { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public DateTime? StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public int? StoreId { get; set; }
        public string? StoreName { get; set; }
        public bool IsActive { get; set; }
        public decimal? SalePrice { get; set; }
        public List<BillOfMaterialResponse> BillOfMaterials { get; set; } = new List<BillOfMaterialResponse>();
        public bool CanStart { get; set; } // Whether assembly can be started (all materials available)
        public string? ValidationMessage { get; set; } // Why assembly cannot start
    }

    public class ProductAssemblyListResponse
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public decimal Quantity { get; set; }
        public string? Unit { get; set; }
        public string Status { get; set; } = string.Empty;
        public string CreatedByUserName { get; set; } = string.Empty;
        public string? CompletedByUserName { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public int? StoreId { get; set; }
        public string? StoreName { get; set; }
        public bool IsActive { get; set; }
        public decimal? SalePrice { get; set; }
        public int MaterialCount { get; set; } // Number of materials in BOM
        public bool CanStart { get; set; }
    }

    public class BillOfMaterialResponse
    {
        public int Id { get; set; }
        public int RawProductId { get; set; }
        public string RawProductName { get; set; } = string.Empty;
        public string RawProductSKU { get; set; } = string.Empty;
        public int WarehouseId { get; set; }
        public string WarehouseName { get; set; } = string.Empty;
        public decimal RequiredQuantity { get; set; }
        public decimal AvailableQuantity { get; set; }
        public decimal ShortageQuantity { get; set; } // Required - Available (if negative, means shortage)
        public string? Unit { get; set; }
        public string? Notes { get; set; }
        public bool IsAvailable { get; set; } // Whether sufficient quantity is available
    }

    public class StartAssemblyRequest
    {
        public string? Notes { get; set; }
    }

    public class CompleteAssemblyRequest
    {
        public string? Notes { get; set; }
    }

    public class AssemblyValidationResponse
    {
        public bool CanStart { get; set; }
        public List<MaterialShortageResponse> Shortages { get; set; } = new List<MaterialShortageResponse>();
        public string Message { get; set; } = string.Empty;
    }

    public class MaterialShortageResponse
    {
        public int RawProductId { get; set; }
        public string RawProductName { get; set; } = string.Empty;
        public int WarehouseId { get; set; }
        public string WarehouseName { get; set; } = string.Empty;
        public decimal RequiredQuantity { get; set; }
        public decimal AvailableQuantity { get; set; }
        public decimal ShortageQuantity { get; set; }
        public string? Unit { get; set; }
    }

    public class AssemblyReportResponse
    {
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public int TotalAssemblies { get; set; }
        public int PendingAssemblies { get; set; }
        public int InProgressAssemblies { get; set; }
        public int CompletedAssemblies { get; set; }
        public int CancelledAssemblies { get; set; }
        public decimal TotalQuantityProduced { get; set; }
        public List<ProductAssemblyListResponse> RecentAssemblies { get; set; } = new List<ProductAssemblyListResponse>();
    }

    public class AssemblyCostAnalysisResponse
    {
        public int AssemblyId { get; set; }
        public string AssemblyName { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
        public string? Unit { get; set; }
        public decimal TotalCost { get; set; }
        public decimal CostPerUnit { get; set; }
        public List<MaterialCostResponse> MaterialCosts { get; set; } = new List<MaterialCostResponse>();
    }

    public class MaterialCostResponse
    {
        public int RawProductId { get; set; }
        public string RawProductName { get; set; } = string.Empty;
        public decimal RequiredQuantity { get; set; }
        public decimal UnitCost { get; set; }
        public decimal TotalCost { get; set; }
        public string? Unit { get; set; }
    }

    public class POSOfferResponse
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public decimal Quantity { get; set; }
        public string? Unit { get; set; }
        public decimal? SalePrice { get; set; }
        public bool IsAvailable { get; set; }
        public string? AvailabilityMessage { get; set; }
        public List<POSOfferItemResponse> Items { get; set; } = new List<POSOfferItemResponse>();
        public List<BillOfMaterialResponse> BillOfMaterials { get; set; } = new List<BillOfMaterialResponse>();
    }

    public class POSOfferItemResponse
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string ProductSKU { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
        public string? Unit { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal TotalPrice { get; set; }
        public string WarehouseName { get; set; } = string.Empty;
        public bool IsAvailable { get; set; }
        public string? Notes { get; set; }
    }

    public class POSAssemblySaleRequest
    {
        public int AssemblyId { get; set; }
        public string? CustomerName { get; set; }
        public string? CustomerEmail { get; set; }
        public string? CustomerPhone { get; set; }
        public string? CustomerAddress { get; set; }
        public string PaymentMethod { get; set; } = "Cash";
        public string? Notes { get; set; }
    }

    public class POSAssemblySaleResponse
    {
        public int SaleId { get; set; }
        public string SaleNumber { get; set; } = string.Empty;
        public string AssemblyName { get; set; } = string.Empty;
        public int AssemblyId { get; set; }
        public decimal TotalAmount { get; set; }
        public List<POSOfferItemResponse> ItemsSold { get; set; } = new List<POSOfferItemResponse>();
        public DateTime SaleDate { get; set; }
        public string CashierName { get; set; } = string.Empty;
    }

    public class SuggestOffersRequest
    {
        public int? StoreId { get; set; }
        public int? MaxSuggestions { get; set; } = 10;
    }

    public class SuggestedOfferResponse
    {
        public string OfferName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public List<SuggestedOfferItem> Items { get; set; } = new List<SuggestedOfferItem>();
        public decimal IndividualTotalPrice { get; set; }
        public decimal SuggestedPrice { get; set; }
        public decimal Savings { get; set; }
        public decimal DiscountPercentage { get; set; }
        public int MaxQuantityAvailable { get; set; }
        public decimal EstimatedProfitMargin { get; set; }
    }

    public class SuggestedOfferItem
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public int SuggestedQuantity { get; set; }
        public decimal UnitPrice { get; set; }
        public int AvailableQuantity { get; set; }
        public string Category { get; set; } = string.Empty;
    }
}

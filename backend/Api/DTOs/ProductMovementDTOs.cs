namespace Api.DTOs
{
    public class ProductMovementResponse
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string ProductSKU { get; set; } = string.Empty;
        public int WarehouseId { get; set; }
        public string WarehouseName { get; set; } = string.Empty;
        public string MovementType { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
        public string? Unit { get; set; }
        public string Direction { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? ReferenceNumber { get; set; }
        public int? ReferenceId { get; set; }
        public string? ReferenceType { get; set; }
        public string? CreatedByUserName { get; set; }
        public DateTime MovementDate { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? Notes { get; set; }
    }

    public class ProductMovementSummaryResponse
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string ProductSKU { get; set; } = string.Empty;
        public int WarehouseId { get; set; }
        public string WarehouseName { get; set; } = string.Empty;
        public DateTime SummaryDate { get; set; }
        public decimal OpeningBalance { get; set; }
        public decimal TotalIn { get; set; }
        public decimal TotalOut { get; set; }
        public decimal ClosingBalance { get; set; }
        public int PurchaseCount { get; set; }
        public int SaleCount { get; set; }
        public int AssemblyCount { get; set; }
        public int TransferCount { get; set; }
        public int AdjustmentCount { get; set; }
        public decimal NetMovement { get; set; } // TotalIn - TotalOut
    }

    public class ProductMovementReportRequest
    {
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public int? ProductId { get; set; }
        public int? WarehouseId { get; set; }
        public string? MovementType { get; set; }
        public string? Direction { get; set; }
        public int? PageNumber { get; set; } = 1;
        public int? PageSize { get; set; } = 50;
        public string? SortBy { get; set; } = "MovementDate";
        public string? SortDirection { get; set; } = "desc";
    }

    public class ProductMovementReportResponse
    {
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public int? ProductId { get; set; }
        public string? ProductName { get; set; }
        public int? WarehouseId { get; set; }
        public string? WarehouseName { get; set; }
        public string? MovementType { get; set; }
        public string? Direction { get; set; }
        public int TotalRecords { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }
        public List<ProductMovementResponse> Movements { get; set; } = new List<ProductMovementResponse>();
        public ProductMovementSummaryResponse? Summary { get; set; }
    }

    public class ProductMovementAnalyticsResponse
    {
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public int TotalMovements { get; set; }
        public int TotalProducts { get; set; }
        public int TotalWarehouses { get; set; }
        public decimal TotalQuantityIn { get; set; }
        public decimal TotalQuantityOut { get; set; }
        public decimal NetMovement { get; set; }
        public Dictionary<string, int> MovementTypeCounts { get; set; } = new Dictionary<string, int>();
        public Dictionary<string, decimal> MovementTypeQuantities { get; set; } = new Dictionary<string, decimal>();
        public List<ProductMovementSummaryResponse> TopMovingProducts { get; set; } = new List<ProductMovementSummaryResponse>();
        public List<ProductMovementSummaryResponse> WarehouseActivity { get; set; } = new List<ProductMovementSummaryResponse>();
        public List<ProductMovementResponse> RecentMovements { get; set; } = new List<ProductMovementResponse>();
    }

    public class ProductMovementTrendResponse
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public int WarehouseId { get; set; }
        public string WarehouseName { get; set; } = string.Empty;
        public DateTime Date { get; set; }
        public decimal OpeningBalance { get; set; }
        public decimal Purchases { get; set; }
        public decimal Sales { get; set; }
        public decimal AssemblyProduction { get; set; }
        public decimal AssemblyConsumption { get; set; }
        public decimal Transfers { get; set; }
        public decimal Adjustments { get; set; }
        public decimal ClosingBalance { get; set; }
        public decimal NetMovement { get; set; }
    }

    public class ProductMovementFilterResponse
    {
        public List<ProductFilterOption> Products { get; set; } = new List<ProductFilterOption>();
        public List<WarehouseFilterOption> Warehouses { get; set; } = new List<WarehouseFilterOption>();
        public List<string> MovementTypes { get; set; } = new List<string>();
        public List<string> Directions { get; set; } = new List<string>();
        public DateTime MinDate { get; set; }
        public DateTime MaxDate { get; set; }
    }

    public class ProductFilterOption
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string SKU { get; set; } = string.Empty;
        public int MovementCount { get; set; }
    }

    public class WarehouseFilterOption
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int MovementCount { get; set; }
    }

    public class ProductMovementExportRequest
    {
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public int? ProductId { get; set; }
        public int? WarehouseId { get; set; }
        public string? MovementType { get; set; }
        public string? Direction { get; set; }
        public string Format { get; set; } = "csv"; // csv, excel, pdf
    }

    public class ProductMovementComparisonResponse
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public int WarehouseId { get; set; }
        public string WarehouseName { get; set; } = string.Empty;
        public ProductMovementPeriod CurrentPeriod { get; set; } = new ProductMovementPeriod();
        public ProductMovementPeriod PreviousPeriod { get; set; } = new ProductMovementPeriod();
        public ProductMovementComparison Comparison { get; set; } = new ProductMovementComparison();
    }

    public class ProductMovementPeriod
    {
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public decimal TotalIn { get; set; }
        public decimal TotalOut { get; set; }
        public decimal NetMovement { get; set; }
        public int MovementCount { get; set; }
        public decimal AverageDailyMovement { get; set; }
    }

    public class ProductMovementComparison
    {
        public decimal InChange { get; set; } // Percentage change
        public decimal OutChange { get; set; } // Percentage change
        public decimal NetChange { get; set; } // Percentage change
        public int CountChange { get; set; } // Percentage change
        public decimal DailyAverageChange { get; set; } // Percentage change
        public string Trend { get; set; } = string.Empty; // Increasing, Decreasing, Stable
    }

    public class ProductMovementAlertResponse
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public int WarehouseId { get; set; }
        public string WarehouseName { get; set; } = string.Empty;
        public string AlertType { get; set; } = string.Empty; // LowStock, HighMovement, NoMovement, etc.
        public string Message { get; set; } = string.Empty;
        public decimal CurrentStock { get; set; }
        public decimal MovementThreshold { get; set; }
        public DateTime AlertDate { get; set; }
        public string Severity { get; set; } = string.Empty; // Low, Medium, High, Critical
    }
}

namespace Api.DTOs;

public class SalesReportResponse
{
    public int Year { get; set; }
    public int? Quarter { get; set; }
    public int? StoreId { get; set; }
    public decimal TotalSales { get; set; }
    public int TotalOrders { get; set; }
    public decimal AverageOrderValue { get; set; }
    public List<QuarterlySalesData> QuarterlyData { get; set; } = new List<QuarterlySalesData>();
    public List<TopProductData> TopProducts { get; set; } = new List<TopProductData>();
    public List<StorePerformanceData> StorePerformance { get; set; } = new List<StorePerformanceData>();
    public DateTime GeneratedAt { get; set; }
}

public class DetailedSalesReportResponse
{
    public int Year { get; set; }
    public int? Quarter { get; set; }
    public int? StoreId { get; set; }
    public decimal TotalSales { get; set; }
    public int TotalOrders { get; set; }
    public decimal AverageOrderValue { get; set; }
    public List<QuarterlySalesData> QuarterlyData { get; set; } = new List<QuarterlySalesData>();
    public List<TopProductData> TopProducts { get; set; } = new List<TopProductData>();
    public List<StorePerformanceData> StorePerformance { get; set; } = new List<StorePerformanceData>();
    public DateTime GeneratedAt { get; set; }
}

public class QuarterlySalesData
{
    public int Quarter { get; set; }
    public decimal TotalSales { get; set; }
    public int OrderCount { get; set; }
    public decimal AverageOrderValue { get; set; }
    public List<TopProductData> TopProducts { get; set; } = new List<TopProductData>();
    public List<PeakHourData> PeakHours { get; set; } = new List<PeakHourData>();
    public List<StoreBreakdownData> StoreBreakdown { get; set; } = new List<StoreBreakdownData>();
}

public class PurchaseReportResponse
{
    public int Year { get; set; }
    public int? Quarter { get; set; }
    public int? StoreId { get; set; }
    public decimal TotalPurchases { get; set; }
    public int TotalOrders { get; set; }
    public decimal AverageOrderValue { get; set; }
    public List<QuarterlyPurchaseData> QuarterlyData { get; set; } = new List<QuarterlyPurchaseData>();
    public List<TopSupplierData> TopSuppliers { get; set; } = new List<TopSupplierData>();
    public List<RecentPurchaseOrderData> RecentOrders { get; set; } = new List<RecentPurchaseOrderData>();
    public DateTime GeneratedAt { get; set; }
}

public class RecentPurchaseOrderData
{
    public int Id { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public DateTime OrderDate { get; set; }
    public string SupplierName { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public string Status { get; set; } = string.Empty;
}

public class QuarterlyPurchaseData
{
    public int Quarter { get; set; }
    public decimal TotalPurchases { get; set; }
    public int OrderCount { get; set; }
    public decimal AverageOrderValue { get; set; }
    public List<TopSupplierData> TopSuppliers { get; set; } = new List<TopSupplierData>();
    public List<TopProductData> TopProducts { get; set; } = new List<TopProductData>();
    public List<StoreBreakdownData> StoreBreakdown { get; set; } = new List<StoreBreakdownData>();
}

public class PeakSalesResponse
{
    public int Year { get; set; }
    public int? Quarter { get; set; }
    public int? StoreId { get; set; }
    public List<HourlySalesData> HourlyAnalysis { get; set; } = new List<HourlySalesData>();
    public List<DailySalesData> DailyAnalysis { get; set; } = new List<DailySalesData>();
    public List<PeakHourData> PeakHours { get; set; } = new List<PeakHourData>();
    public List<DailySalesData> PeakDays { get; set; } = new List<DailySalesData>();
    public DateTime GeneratedAt { get; set; }
}

public class TopProductData
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public decimal TotalQuantity { get; set; }
    public decimal TotalRevenue { get; set; }
}

public class TopSupplierData
{
    public string SupplierName { get; set; } = string.Empty;
    public int TotalOrders { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal AverageOrderValue { get; set; }
}

public class PeakHourData
{
    public int Hour { get; set; }
    public decimal TotalSales { get; set; }
    public int OrderCount { get; set; }
}

public class HourlySalesData
{
    public int Hour { get; set; }
    public decimal TotalSales { get; set; }
    public int OrderCount { get; set; }
    public decimal AverageOrderValue { get; set; }
}

public class DailySalesData
{
    public DateTime Date { get; set; }
    public string DayOfWeek { get; set; } = string.Empty;
    public decimal TotalSales { get; set; }
    public int OrderCount { get; set; }
    public decimal AverageOrderValue { get; set; }
}

public class StoreBreakdownData
{
    public int StoreId { get; set; }
    public string StoreName { get; set; } = string.Empty;
    public decimal TotalSales { get; set; }
    public int OrderCount { get; set; }
}

public class StorePerformanceData
{
    public int StoreId { get; set; }
    public string StoreName { get; set; } = string.Empty;
    public decimal TotalSales { get; set; }
    public decimal AverageOrderValue { get; set; }
}

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

// ==================== REVENUE OVER TIME ====================
public class RevenueOverTimeResponse
{
    public string Period { get; set; } = string.Empty; // "daily", "weekly", "monthly", "yearly"
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public List<RevenueDataPoint> Data { get; set; } = new List<RevenueDataPoint>();
    public decimal TotalRevenue { get; set; }
    public decimal AverageRevenue { get; set; }
    public decimal GrowthPercentage { get; set; }
    public DateTime GeneratedAt { get; set; }
}

public class RevenueDataPoint
{
    public DateTime Date { get; set; }
    public string Label { get; set; } = string.Empty;
    public decimal Revenue { get; set; }
    public int OrderCount { get; set; }
    public decimal AverageOrderValue { get; set; }
}

// ==================== CATEGORY SALES ====================
public class CategorySalesResponse
{
    public int Year { get; set; }
    public int? Month { get; set; }
    public List<CategorySalesData> Categories { get; set; } = new List<CategorySalesData>();
    public decimal TotalRevenue { get; set; }
    public DateTime GeneratedAt { get; set; }
}

public class CategorySalesData
{
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public decimal TotalRevenue { get; set; }
    public int TotalQuantity { get; set; }
    public int OrderCount { get; set; }
    public decimal PercentageOfTotal { get; set; }
    public List<TopProductData> TopProducts { get; set; } = new List<TopProductData>();
}

// ==================== CUSTOMER ANALYTICS ====================
public class CustomerAnalyticsResponse
{
    public int TotalCustomers { get; set; }
    public int NewCustomersThisMonth { get; set; }
    public int NewCustomersLastMonth { get; set; }
    public decimal CustomerGrowthPercentage { get; set; }
    public decimal AverageCustomerLifetimeValue { get; set; }
    public decimal RepeatPurchaseRate { get; set; }
    public List<TopCustomerData> TopCustomers { get; set; } = new List<TopCustomerData>();
    public List<CustomerAcquisitionData> AcquisitionTrend { get; set; } = new List<CustomerAcquisitionData>();
    public List<GeographicDistributionData> GeographicDistribution { get; set; } = new List<GeographicDistributionData>();
    public DateTime GeneratedAt { get; set; }
}

public class TopCustomerData
{
    public int CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public decimal TotalSpent { get; set; }
    public int OrderCount { get; set; }
    public DateTime LastOrderDate { get; set; }
    public decimal AverageOrderValue { get; set; }
}

public class CustomerAcquisitionData
{
    public DateTime Date { get; set; }
    public string Label { get; set; } = string.Empty;
    public int NewCustomers { get; set; }
}

public class GeographicDistributionData
{
    public string Region { get; set; } = string.Empty;
    public int CustomerCount { get; set; }
    public decimal TotalRevenue { get; set; }
    public decimal PercentageOfTotal { get; set; }
}

// ==================== INVENTORY REPORTS ====================
public class InventoryReportResponse
{
    public int TotalProducts { get; set; }
    public int TotalVariants { get; set; }
    public decimal TotalInventoryValue { get; set; }
    public int LowStockCount { get; set; }
    public int OutOfStockCount { get; set; }
    public List<InventoryTurnoverData> TurnoverByProduct { get; set; } = new List<InventoryTurnoverData>();
    public List<DeadStockData> DeadStock { get; set; } = new List<DeadStockData>();
    public List<VariantPerformanceData> VariantPerformance { get; set; } = new List<VariantPerformanceData>();
    public List<StockForecastData> StockForecast { get; set; } = new List<StockForecastData>();
    public DateTime GeneratedAt { get; set; }
}

public class InventoryTurnoverData
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public decimal CurrentStock { get; set; }
    public decimal SoldQuantity { get; set; }
    public decimal TurnoverRate { get; set; } // SoldQuantity / AverageStock
    public int DaysSinceLastSale { get; set; }
}

public class DeadStockData
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string? SKU { get; set; }
    public decimal CurrentStock { get; set; }
    public decimal StockValue { get; set; }
    public int DaysWithoutSale { get; set; }
    public DateTime? LastSaleDate { get; set; }
}

public class VariantPerformanceData
{
    public int VariantId { get; set; }
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string VariantAttributes { get; set; } = string.Empty;
    public decimal TotalSold { get; set; }
    public decimal Revenue { get; set; }
    public decimal CurrentStock { get; set; }
}

public class StockForecastData
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public decimal CurrentStock { get; set; }
    public decimal AverageDailySales { get; set; }
    public int DaysUntilStockout { get; set; }
    public DateTime? PredictedStockoutDate { get; set; }
    public string RiskLevel { get; set; } = string.Empty; // "Critical", "High", "Medium", "Low"
}

// ==================== FINANCIAL REPORTS ====================
public class FinancialReportResponse
{
    public int Year { get; set; }
    public int? Month { get; set; }
    public decimal GrossRevenue { get; set; }
    public decimal TotalCosts { get; set; }
    public decimal GrossProfit { get; set; }
    public decimal GrossMarginPercentage { get; set; }
    public decimal TotalDiscounts { get; set; }
    public decimal NetRevenue { get; set; }
    public List<ProductMarginData> ProductMargins { get; set; } = new List<ProductMarginData>();
    public List<MonthlyFinancialData> MonthlyTrend { get; set; } = new List<MonthlyFinancialData>();
    public PaymentStatusSummary PaymentStatus { get; set; } = new PaymentStatusSummary();
    public DownPaymentSummary DownPayments { get; set; } = new DownPaymentSummary();
    public DateTime GeneratedAt { get; set; }
}

public class ProductMarginData
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public decimal Cost { get; set; }
    public decimal GrossMargin { get; set; }
    public decimal GrossMarginPercentage { get; set; }
    public decimal TotalRevenue { get; set; }
    public decimal TotalProfit { get; set; }
    public int QuantitySold { get; set; }
}

public class MonthlyFinancialData
{
    public int Month { get; set; }
    public string MonthName { get; set; } = string.Empty;
    public decimal Revenue { get; set; }
    public decimal Costs { get; set; }
    public decimal Profit { get; set; }
    public decimal MarginPercentage { get; set; }
}

public class PaymentStatusSummary
{
    public decimal PaidAmount { get; set; }
    public int PaidOrders { get; set; }
    public decimal PendingAmount { get; set; }
    public int PendingOrders { get; set; }
    public decimal PartiallyPaidAmount { get; set; }
    public int PartiallyPaidOrders { get; set; }
    public decimal RefundedAmount { get; set; }
    public int RefundedOrders { get; set; }
}

public class DownPaymentSummary
{
    public decimal TotalDownPayments { get; set; }
    public int OrdersWithDownPayments { get; set; }
    public decimal AverageDownPaymentPercentage { get; set; }
    public decimal OutstandingBalance { get; set; }
}

// ==================== PROMO CODE ANALYTICS ====================
public class PromoCodeAnalyticsResponse
{
    public int TotalPromoCodes { get; set; }
    public int ActivePromoCodes { get; set; }
    public decimal TotalDiscountsGiven { get; set; }
    public int TotalUsageCount { get; set; }
    public decimal AverageDiscountPerUse { get; set; }
    public decimal ConversionRateWithPromo { get; set; }
    public decimal ConversionRateWithoutPromo { get; set; }
    public List<PromoCodePerformanceData> TopPromoCodes { get; set; } = new List<PromoCodePerformanceData>();
    public List<PromoCodeUsageTrendData> UsageTrend { get; set; } = new List<PromoCodeUsageTrendData>();
    public DateTime GeneratedAt { get; set; }
}

public class PromoCodePerformanceData
{
    public int PromoCodeId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string DiscountType { get; set; } = string.Empty;
    public decimal DiscountValue { get; set; }
    public int UsageCount { get; set; }
    public int UsageLimit { get; set; }
    public decimal TotalDiscountGiven { get; set; }
    public decimal RevenueGenerated { get; set; }
    public decimal ROI { get; set; } // (RevenueGenerated - TotalDiscountGiven) / TotalDiscountGiven
    public bool IsActive { get; set; }
    public DateTime? ExpiryDate { get; set; }
}

public class PromoCodeUsageTrendData
{
    public DateTime Date { get; set; }
    public string Label { get; set; } = string.Empty;
    public int UsageCount { get; set; }
    public decimal DiscountAmount { get; set; }
}

// ==================== ORDER STATUS DISTRIBUTION ====================
public class OrderStatusDistributionResponse
{
    public int TotalOrders { get; set; }
    public List<OrderStatusData> StatusDistribution { get; set; } = new List<OrderStatusData>();
    public List<OrderStatusTrendData> StatusTrend { get; set; } = new List<OrderStatusTrendData>();
    public decimal AverageFulfillmentTimeHours { get; set; }
    public DateTime GeneratedAt { get; set; }
}

public class OrderStatusData
{
    public string Status { get; set; } = string.Empty;
    public int Count { get; set; }
    public decimal Percentage { get; set; }
    public decimal TotalValue { get; set; }
}

public class OrderStatusTrendData
{
    public DateTime Date { get; set; }
    public string Label { get; set; } = string.Empty;
    public int Pending { get; set; }
    public int Confirmed { get; set; }
    public int Shipped { get; set; }
    public int Delivered { get; set; }
    public int Cancelled { get; set; }
}
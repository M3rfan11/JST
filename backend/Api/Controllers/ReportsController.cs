using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Api.Data;
using Api.DTOs;
using Api.Models;
using System.Security.Claims;

namespace Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ReportsController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<ReportsController> _logger;

    public ReportsController(ApplicationDbContext context, ILogger<ReportsController> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Get sales report with quarterly breakdown
    /// </summary>
    [HttpGet("sales")]
    public async Task<ActionResult<SalesReportResponse>> GetSalesReport(
        [FromQuery] int? year = null,
        [FromQuery] int? quarter = null,
        [FromQuery] int? storeId = null)
    {
        try
        {
            var currentUserId = GetCurrentUserId();
            var currentUserRoles = await GetCurrentUserRoles(currentUserId);
            
            // Default to current year if not specified
            var reportYear = year ?? DateTime.UtcNow.Year;
            
            // Only SuperAdmin can access reports - no store filtering
            if (!currentUserRoles.Contains("SuperAdmin"))
            {
                return StatusCode(403, new { Message = "You don't have permission to view reports" });
            }

            // Simplified query to avoid complex joins
            var salesQuery = _context.SalesOrders
                .Where(so => so.OrderDate.Year == reportYear);

            // Apply store filter if specified
            if (storeId.HasValue)
            {
                salesQuery = salesQuery.Where(so => so.SalesItems.Any(si => si.WarehouseId == storeId.Value));
            }

            // Apply quarter filter if specified
            if (quarter.HasValue)
            {
                var quarterMonths = GetQuarterMonths(quarter.Value);
                salesQuery = salesQuery.Where(so => quarterMonths.Contains(so.OrderDate.Month));
            }

            var sales = await salesQuery.ToListAsync();

            // Calculate quarterly data
            var quarterlyData = new List<QuarterlySalesData>();
            for (int q = 1; q <= 4; q++)
            {
                var quarterMonths = GetQuarterMonths(q);
                var quarterSales = sales.Where(s => quarterMonths.Contains(s.OrderDate.Month)).ToList();
                
                var quarterData = new QuarterlySalesData
                {
                    Quarter = q,
                    TotalSales = quarterSales.Sum(s => s.TotalAmount),
                    OrderCount = quarterSales.Count,
                    AverageOrderValue = quarterSales.Any() ? quarterSales.Average(s => s.TotalAmount) : 0,
                    TopProducts = GetTopProducts(quarterSales, storeId),
                    PeakHours = GetPeakSalesHours(quarterSales),
                    StoreBreakdown = GetStoreBreakdown(quarterSales, storeId)
                };
                
                quarterlyData.Add(quarterData);
            }

            // Calculate overall statistics
            var totalSales = sales.Sum(s => s.TotalAmount);
            var totalOrders = sales.Count;
            var averageOrderValue = sales.Any() ? sales.Average(s => s.TotalAmount) : 0;

            // Get top products overall (simplified)
            var allProducts = new List<TopProductData>();
            if (sales.Any())
            {
                // Get sales items for the sales orders
                var salesItemIds = sales.Select(s => s.Id).ToList();
                var salesItems = await _context.SalesItems
                    .Include(si => si.Product)
                    .Where(si => salesItemIds.Contains(si.SalesOrderId))
                    .ToListAsync();

                allProducts = salesItems
                    .Where(si => si.Product != null)
                    .GroupBy(si => new { si.ProductId, ProductName = si.Product!.Name })
                    .Select(g => new TopProductData
                    {
                        ProductId = g.Key.ProductId,
                        ProductName = g.Key.ProductName,
                        TotalQuantity = g.Sum(si => si.Quantity),
                        TotalRevenue = g.Sum(si => si.TotalPrice)
                    })
                    .OrderByDescending(p => p.TotalRevenue)
                    .Take(10)
                    .ToList();
            }

            // Get store performance analysis (simplified)
            var storePerformance = new List<StorePerformanceData>();
            
            // Get all stores for admin, or specific store for store manager
            var stores = new List<Warehouse>();
            if (storeId.HasValue)
            {
                var store = await _context.Warehouses.FindAsync(storeId.Value);
                if (store != null) stores.Add(store);
            }
            else
            {
                stores = await _context.Warehouses.Where(w => w.IsActive).ToListAsync();
            }

            foreach (var store in stores)
            {
                var storeSales = sales.Where(s => s.SalesItems.Any(si => si.WarehouseId == store.Id)).ToList();
                var storeTotal = storeSales.Sum(s => s.TotalAmount);
                
                storePerformance.Add(new StorePerformanceData
                {
                    StoreId = store.Id,
                    StoreName = store.Name,
                    TotalSales = storeTotal,
                    AverageOrderValue = storeSales.Any() ? storeSales.Average(s => s.TotalAmount) : 0
                });
            }

            return Ok(new SalesReportResponse
            {
                Year = reportYear,
                Quarter = quarter,
                StoreId = storeId,
                TotalSales = totalSales,
                TotalOrders = totalOrders,
                AverageOrderValue = averageOrderValue,
                QuarterlyData = quarterlyData,
                TopProducts = allProducts,
                StorePerformance = storePerformance,
                GeneratedAt = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating sales report");
            return StatusCode(500, new { message = "An error occurred while generating the sales report" });
        }
    }

    /// <summary>
    /// Get purchase report with quarterly breakdown
    /// </summary>
    [HttpGet("purchases")]
    public async Task<ActionResult<PurchaseReportResponse>> GetPurchaseReport(
        [FromQuery] int? year = null,
        [FromQuery] int? quarter = null,
        [FromQuery] int? storeId = null)
    {
        try
        {
            var currentUserId = GetCurrentUserId();
            var currentUserRoles = await GetCurrentUserRoles(currentUserId);
            
            // Default to current year if not specified
            var reportYear = year ?? DateTime.UtcNow.Year;
            
            // Only SuperAdmin can access reports - no store filtering
            if (!currentUserRoles.Contains("SuperAdmin"))
            {
                return StatusCode(403, new { Message = "You don't have permission to view reports" });
            }

            // Fixed Entity Framework query with proper Include syntax
            var purchasesQuery = _context.PurchaseOrders
                .Include(po => po.PurchaseItems)
                    .ThenInclude(pi => pi.Product)
                .Include(po => po.CreatedByUser)
                .Where(po => po.OrderDate.Year == reportYear);

            // Apply store filter if specified
            if (storeId.HasValue)
            {
                purchasesQuery = purchasesQuery.Where(po => po.PurchaseItems.Any(pi => pi.WarehouseId == storeId.Value));
            }

            // Apply quarter filter if specified
            if (quarter.HasValue)
            {
                var quarterMonths = GetQuarterMonths(quarter.Value);
                purchasesQuery = purchasesQuery.Where(po => quarterMonths.Contains(po.OrderDate.Month));
            }

            var purchases = await purchasesQuery.ToListAsync();

            // Load warehouses for all purchase items
            var warehouseIds = purchases.SelectMany(p => p.PurchaseItems).Select(pi => pi.WarehouseId).Distinct().ToList();
            var warehouses = await _context.Warehouses.Where(w => warehouseIds.Contains(w.Id)).ToListAsync();
            
            // Manually set warehouse navigation properties
            foreach (var purchase in purchases)
            {
                foreach (var item in purchase.PurchaseItems)
                {
                    item.Warehouse = warehouses.FirstOrDefault(w => w.Id == item.WarehouseId);
                }
            }

            // Calculate quarterly data
            var quarterlyData = new List<QuarterlyPurchaseData>();
            for (int q = 1; q <= 4; q++)
            {
                var quarterMonths = GetQuarterMonths(q);
                var quarterPurchases = purchases.Where(p => quarterMonths.Contains(p.OrderDate.Month)).ToList();
                
                var quarterData = new QuarterlyPurchaseData
                {
                    Quarter = q,
                    TotalPurchases = quarterPurchases.Sum(p => p.TotalAmount),
                    OrderCount = quarterPurchases.Count,
                    AverageOrderValue = quarterPurchases.Any() ? quarterPurchases.Average(p => p.TotalAmount) : 0,
                    TopSuppliers = GetTopSuppliers(quarterPurchases),
                    TopProducts = GetTopPurchasedProducts(quarterPurchases, storeId),
                    StoreBreakdown = GetPurchaseStoreBreakdown(quarterPurchases, storeId)
                };
                
                quarterlyData.Add(quarterData);
            }

            // Calculate overall statistics
            var totalPurchases = purchases.Sum(p => p.TotalAmount);
            var totalOrders = purchases.Count;
            var averageOrderValue = purchases.Any() ? purchases.Average(p => p.TotalAmount) : 0;

            // Get top suppliers overall
            var topSuppliers = GetTopSuppliers(purchases);

            // Get recent orders for detailed view
            var recentOrders = purchases
                .OrderByDescending(p => p.OrderDate)
                .Take(10)
                .Select(p => new RecentPurchaseOrderData
                {
                    Id = p.Id,
                    OrderNumber = p.OrderNumber,
                    OrderDate = p.OrderDate,
                    SupplierName = p.SupplierName,
                    TotalAmount = p.TotalAmount,
                    Status = p.Status
                })
                .ToList();

            return Ok(new PurchaseReportResponse
            {
                Year = reportYear,
                Quarter = quarter,
                StoreId = storeId,
                TotalPurchases = totalPurchases,
                TotalOrders = totalOrders,
                AverageOrderValue = averageOrderValue,
                QuarterlyData = quarterlyData,
                TopSuppliers = topSuppliers,
                RecentOrders = recentOrders,
                GeneratedAt = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating purchase report");
            return StatusCode(500, new { message = "An error occurred while generating the purchase report" });
        }
    }

    /// <summary>
    /// Get peak sales analysis with hourly and daily breakdown
    /// </summary>
    [HttpGet("peak-sales")]
    public async Task<ActionResult<PeakSalesResponse>> GetPeakSalesAnalysis(
        [FromQuery] int? year = null,
        [FromQuery] int? quarter = null,
        [FromQuery] int? storeId = null)
    {
        try
        {
            var currentUserId = GetCurrentUserId();
            var currentUserRoles = await GetCurrentUserRoles(currentUserId);
            
            // Default to current year if not specified
            var reportYear = year ?? DateTime.UtcNow.Year;
            
            // Only SuperAdmin can access reports - no store filtering
            if (!currentUserRoles.Contains("SuperAdmin"))
            {
                return StatusCode(403, new { Message = "You don't have permission to view reports" });
            }

            // Get sales data
            var salesQuery = _context.SalesOrders
                .Include(s => s.SalesItems)
                    .ThenInclude(si => si.Warehouse)
                .Where(s => s.OrderDate.Year == reportYear);

            if (quarter.HasValue)
            {
                var quarterMonths = GetQuarterMonths(quarter.Value);
                salesQuery = salesQuery.Where(s => quarterMonths.Contains(s.OrderDate.Month));
            }

            if (storeId.HasValue)
            {
                salesQuery = salesQuery.Where(s => s.SalesItems.Any(si => si.WarehouseId == storeId.Value));
            }

            var sales = await salesQuery.ToListAsync();

            // Generate peak sales analysis
            var hourlyAnalysis = GetHourlySalesAnalysis(sales);
            var dailyAnalysis = GetDailySalesAnalysis(sales);
            var peakHours = GetPeakSalesHours(sales);
            var peakDays = dailyAnalysis.OrderByDescending(d => d.TotalSales).Take(5).ToList();

            return Ok(new PeakSalesResponse
            {
                Year = reportYear,
                Quarter = quarter,
                StoreId = storeId,
                HourlyAnalysis = hourlyAnalysis,
                DailyAnalysis = dailyAnalysis,
                PeakHours = peakHours,
                PeakDays = peakDays,
                GeneratedAt = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating peak sales analysis");
            return StatusCode(500, new { message = "An error occurred while generating the peak sales analysis" });
        }
    }

    private int[] GetQuarterMonths(int quarter)
    {
        return quarter switch
        {
            1 => new[] { 1, 2, 3 },
            2 => new[] { 4, 5, 6 },
            3 => new[] { 7, 8, 9 },
            4 => new[] { 10, 11, 12 },
            _ => throw new ArgumentException("Invalid quarter")
        };
    }

    private List<TopProductData> GetTopProducts(List<SalesOrder> sales, int? storeId)
    {
        return sales.SelectMany(s => s.SalesItems)
            .Where(si => si.Product != null && (!storeId.HasValue || si.WarehouseId == storeId.Value))
            .GroupBy(si => new { si.ProductId, ProductName = si.Product!.Name })
            .Select(g => new TopProductData
            {
                ProductId = g.Key.ProductId,
                ProductName = g.Key.ProductName,
                TotalQuantity = g.Sum(si => si.Quantity),
                TotalRevenue = g.Sum(si => si.TotalPrice)
            })
            .OrderByDescending(p => p.TotalRevenue)
            .Take(5)
            .ToList();
    }

    private List<PeakHourData> GetPeakSalesHours(List<SalesOrder> sales)
    {
        return sales.GroupBy(s => s.OrderDate.Hour)
            .Select(g => new PeakHourData
            {
                Hour = g.Key,
                OrderCount = g.Count(),
                TotalSales = g.Sum(s => s.TotalAmount)
            })
            .OrderByDescending(h => h.OrderCount)
            .Take(5)
            .ToList();
    }

    private List<HourlySalesData> GetHourlySalesAnalysis(List<SalesOrder> sales)
    {
        return Enumerable.Range(0, 24)
            .Select(hour => new HourlySalesData
            {
                Hour = hour,
                TotalSales = sales.Where(s => s.OrderDate.Hour == hour).Sum(s => s.TotalAmount),
                OrderCount = sales.Count(s => s.OrderDate.Hour == hour),
                AverageOrderValue = sales.Where(s => s.OrderDate.Hour == hour).Count() > 0 
                    ? sales.Where(s => s.OrderDate.Hour == hour).Average(s => s.TotalAmount) 
                    : 0
            })
            .ToList();
    }

    private List<DailySalesData> GetDailySalesAnalysis(List<SalesOrder> sales)
    {
        return sales.GroupBy(s => s.OrderDate.Date)
            .Select(g => new DailySalesData
            {
                Date = g.Key,
                DayOfWeek = g.Key.DayOfWeek.ToString(),
                TotalSales = g.Sum(s => s.TotalAmount),
                OrderCount = g.Count(),
                AverageOrderValue = g.Average(s => s.TotalAmount)
            })
            .OrderByDescending(d => d.TotalSales)
            .ToList();
    }

    private List<StoreBreakdownData> GetStoreBreakdown(List<SalesOrder> sales, int? storeId)
    {
        if (storeId.HasValue)
        {
            var storeSales = sales.Where(s => s.SalesItems.Any(si => si.WarehouseId == storeId.Value)).ToList();
            return new List<StoreBreakdownData>
            {
                new StoreBreakdownData
                {
                    StoreId = storeId.Value,
                    StoreName = "Current Store",
                    TotalSales = storeSales.Sum(s => s.TotalAmount),
                    OrderCount = storeSales.Count
                }
            };
        }

        // Return breakdown by all stores with null checks
        return sales.SelectMany(s => s.SalesItems)
            .Where(si => si.Warehouse != null)
            .GroupBy(si => new { si.WarehouseId, WarehouseName = si.Warehouse!.Name })
            .Select(g => new StoreBreakdownData
            {
                StoreId = g.Key.WarehouseId,
                StoreName = g.Key.WarehouseName,
                TotalSales = g.Sum(si => si.TotalPrice),
                OrderCount = sales.Count(s => s.SalesItems.Any(si => si.WarehouseId == g.Key.WarehouseId))
            })
            .OrderByDescending(s => s.TotalSales)
            .ToList();
    }

    private List<TopSupplierData> GetTopSuppliers(List<PurchaseOrder> purchases)
    {
        return purchases
            .Where(p => !string.IsNullOrEmpty(p.SupplierName))
            .GroupBy(p => p.SupplierName!)
            .Select(g => new TopSupplierData
            {
                SupplierName = g.Key,
                TotalOrders = g.Count(),
                TotalAmount = g.Sum(p => p.TotalAmount),
                AverageOrderValue = g.Average(p => p.TotalAmount)
            })
            .OrderByDescending(s => s.TotalAmount)
            .Take(5)
            .ToList();
    }

    private List<TopProductData> GetTopPurchasedProducts(List<PurchaseOrder> purchases, int? storeId)
    {
        return purchases.SelectMany(p => p.PurchaseItems)
            .Where(pi => pi.Product != null && (!storeId.HasValue || pi.WarehouseId == storeId.Value))
            .GroupBy(pi => new { pi.ProductId, ProductName = pi.Product!.Name })
            .Select(g => new TopProductData
            {
                ProductId = g.Key.ProductId,
                ProductName = g.Key.ProductName,
                TotalQuantity = g.Sum(pi => pi.Quantity),
                TotalRevenue = g.Sum(pi => pi.TotalPrice)
            })
            .OrderByDescending(p => p.TotalQuantity)
            .Take(5)
            .ToList();
    }

    private List<StoreBreakdownData> GetPurchaseStoreBreakdown(List<PurchaseOrder> purchases, int? storeId)
    {
        if (storeId.HasValue)
        {
            var storePurchases = purchases.Where(p => p.PurchaseItems.Any(pi => pi.WarehouseId == storeId.Value)).ToList();
            return new List<StoreBreakdownData>
            {
                new StoreBreakdownData
                {
                    StoreId = storeId.Value,
                    StoreName = "Current Store",
                    TotalSales = storePurchases.Sum(p => p.TotalAmount),
                    OrderCount = storePurchases.Count
                }
            };
        }

        // Return breakdown by all stores with null checks
        return purchases.SelectMany(p => p.PurchaseItems)
            .Where(pi => pi.Warehouse != null)
            .GroupBy(pi => new { pi.WarehouseId, WarehouseName = pi.Warehouse!.Name })
            .Select(g => new StoreBreakdownData
            {
                StoreId = g.Key.WarehouseId,
                StoreName = g.Key.WarehouseName,
                TotalSales = g.Sum(pi => pi.TotalPrice),
                OrderCount = purchases.Count(p => p.PurchaseItems.Any(pi => pi.WarehouseId == g.Key.WarehouseId))
            })
            .OrderByDescending(s => s.TotalSales)
            .ToList();
    }

    private async Task<List<StorePerformanceData>> GetStorePerformanceAnalysis(int year, int? quarter, int? storeId)
    {
        // Get current period data
        var currentQuery = _context.SalesOrders
            .Include(so => so.SalesItems)
                .ThenInclude(si => si.Warehouse)
            .Where(so => so.OrderDate.Year == year);

        if (quarter.HasValue)
        {
            var quarterMonths = GetQuarterMonths(quarter.Value);
            currentQuery = currentQuery.Where(so => quarterMonths.Contains(so.OrderDate.Month));
        }

        if (storeId.HasValue)
        {
            currentQuery = currentQuery.Where(so => so.SalesItems.Any(si => si.WarehouseId == storeId.Value));
        }

        var currentSales = await currentQuery.ToListAsync();

        // Get previous period data for comparison
        var previousYear = year - 1;
        var previousQuery = _context.SalesOrders
            .Include(so => so.SalesItems)
                .ThenInclude(si => si.Warehouse)
            .Where(so => so.OrderDate.Year == previousYear);

        if (quarter.HasValue)
        {
            var quarterMonths = GetQuarterMonths(quarter.Value);
            previousQuery = previousQuery.Where(so => quarterMonths.Contains(so.OrderDate.Month));
        }

        if (storeId.HasValue)
        {
            previousQuery = previousQuery.Where(so => so.SalesItems.Any(si => si.WarehouseId == storeId.Value));
        }

        var previousSales = await previousQuery.ToListAsync();

        // Calculate performance metrics
        var storePerformance = new List<StorePerformanceData>();

        if (storeId.HasValue)
        {
            // Single store analysis
            var currentStoreSales = currentSales.Where(s => s.SalesItems.Any(si => si.WarehouseId == storeId.Value)).ToList();

            var currentTotal = currentStoreSales.Sum(s => s.TotalAmount);

            storePerformance.Add(new StorePerformanceData
            {
                StoreId = storeId.Value,
                StoreName = "Current Store",
                TotalSales = currentTotal,
                AverageOrderValue = currentStoreSales.Any() ? currentStoreSales.Average(s => s.TotalAmount) : 0
            });
        }
        else
        {
            // All stores analysis
            var allStores = await _context.Warehouses.Where(w => w.IsActive).ToListAsync();

            foreach (var store in allStores)
            {
                var currentStoreSales = currentSales.Where(s => s.SalesItems.Any(si => si.WarehouseId == store.Id)).ToList();

                var currentTotal = currentStoreSales.Sum(s => s.TotalAmount);

                storePerformance.Add(new StorePerformanceData
                {
                    StoreId = store.Id,
                    StoreName = store.Name,
                    TotalSales = currentTotal,
                    AverageOrderValue = currentStoreSales.Any() ? currentStoreSales.Average(s => s.TotalAmount) : 0
                });
            }
        }

        return storePerformance.OrderByDescending(sp => sp.TotalSales).ToList();
    }

    private int GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        return userIdClaim != null && int.TryParse(userIdClaim.Value, out var userId) ? userId : 0;
    }

    private async Task<List<string>> GetCurrentUserRoles(int userId)
    {
        return await _context.UserRoles
            .Where(ur => ur.UserId == userId)
            .Select(ur => ur.Role.Name)
            .ToListAsync();
    }
}
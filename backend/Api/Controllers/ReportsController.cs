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

    /// <summary>
    /// Get revenue over time with customizable granularity
    /// </summary>
    [HttpGet("revenue-over-time")]
    public async Task<ActionResult<RevenueOverTimeResponse>> GetRevenueOverTime(
        [FromQuery] string period = "daily", // daily, weekly, monthly, yearly
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null)
    {
        try
        {
            var currentUserId = GetCurrentUserId();
            var currentUserRoles = await GetCurrentUserRoles(currentUserId);
            
            if (!currentUserRoles.Contains("SuperAdmin"))
            {
                return StatusCode(403, new { Message = "You don't have permission to view reports" });
            }

            var end = endDate ?? DateTime.UtcNow;
            var start = startDate ?? period switch
            {
                "daily" => end.AddDays(-30),
                "weekly" => end.AddMonths(-3),
                "monthly" => end.AddYears(-1),
                "yearly" => end.AddYears(-5),
                _ => end.AddDays(-30)
            };

            var sales = await _context.SalesOrders
                .Where(so => so.OrderDate >= start && so.OrderDate <= end)
                .Where(so => so.Status == "Completed" || so.Status == "Delivered")
                .ToListAsync();

            var dataPoints = new List<RevenueDataPoint>();

            if (period == "daily")
            {
                var grouped = sales.GroupBy(s => s.OrderDate.Date)
                    .OrderBy(g => g.Key);
                foreach (var group in grouped)
                {
                    dataPoints.Add(new RevenueDataPoint
                    {
                        Date = group.Key,
                        Label = group.Key.ToString("MMM dd"),
                        Revenue = group.Sum(s => s.TotalAmount),
                        OrderCount = group.Count(),
                        AverageOrderValue = group.Any() ? group.Average(s => s.TotalAmount) : 0
                    });
                }
            }
            else if (period == "weekly")
            {
                var grouped = sales.GroupBy(s => new { Year = s.OrderDate.Year, Week = GetWeekOfYear(s.OrderDate) })
                    .OrderBy(g => g.Key.Year).ThenBy(g => g.Key.Week);
                foreach (var group in grouped)
                {
                    var firstDayOfWeek = FirstDateOfWeek(group.Key.Year, group.Key.Week);
                    dataPoints.Add(new RevenueDataPoint
                    {
                        Date = firstDayOfWeek,
                        Label = $"Week {group.Key.Week}, {group.Key.Year}",
                        Revenue = group.Sum(s => s.TotalAmount),
                        OrderCount = group.Count(),
                        AverageOrderValue = group.Any() ? group.Average(s => s.TotalAmount) : 0
                    });
                }
            }
            else if (period == "monthly")
            {
                var grouped = sales.GroupBy(s => new { s.OrderDate.Year, s.OrderDate.Month })
                    .OrderBy(g => g.Key.Year).ThenBy(g => g.Key.Month);
                foreach (var group in grouped)
                {
                    dataPoints.Add(new RevenueDataPoint
                    {
                        Date = new DateTime(group.Key.Year, group.Key.Month, 1),
                        Label = new DateTime(group.Key.Year, group.Key.Month, 1).ToString("MMM yyyy"),
                        Revenue = group.Sum(s => s.TotalAmount),
                        OrderCount = group.Count(),
                        AverageOrderValue = group.Any() ? group.Average(s => s.TotalAmount) : 0
                    });
                }
            }
            else // yearly
            {
                var grouped = sales.GroupBy(s => s.OrderDate.Year)
                    .OrderBy(g => g.Key);
                foreach (var group in grouped)
                {
                    dataPoints.Add(new RevenueDataPoint
                    {
                        Date = new DateTime(group.Key, 1, 1),
                        Label = group.Key.ToString(),
                        Revenue = group.Sum(s => s.TotalAmount),
                        OrderCount = group.Count(),
                        AverageOrderValue = group.Any() ? group.Average(s => s.TotalAmount) : 0
                    });
                }
            }

            var totalRevenue = dataPoints.Sum(d => d.Revenue);
            var avgRevenue = dataPoints.Any() ? dataPoints.Average(d => d.Revenue) : 0;
            
            // Calculate growth percentage
            decimal growthPercentage = 0;
            if (dataPoints.Count >= 2)
            {
                var firstHalf = dataPoints.Take(dataPoints.Count / 2).Sum(d => d.Revenue);
                var secondHalf = dataPoints.Skip(dataPoints.Count / 2).Sum(d => d.Revenue);
                if (firstHalf > 0)
                {
                    growthPercentage = ((secondHalf - firstHalf) / firstHalf) * 100;
                }
            }

            return Ok(new RevenueOverTimeResponse
            {
                Period = period,
                StartDate = start,
                EndDate = end,
                Data = dataPoints,
                TotalRevenue = totalRevenue,
                AverageRevenue = avgRevenue,
                GrowthPercentage = growthPercentage,
                GeneratedAt = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating revenue over time report");
            return StatusCode(500, new { message = "An error occurred while generating the revenue report" });
        }
    }

    /// <summary>
    /// Get sales by category
    /// </summary>
    [HttpGet("category-sales")]
    public async Task<ActionResult<CategorySalesResponse>> GetCategorySales(
        [FromQuery] int? year = null,
        [FromQuery] int? month = null)
    {
        try
        {
            var currentUserId = GetCurrentUserId();
            var currentUserRoles = await GetCurrentUserRoles(currentUserId);
            
            if (!currentUserRoles.Contains("SuperAdmin"))
            {
                return StatusCode(403, new { Message = "You don't have permission to view reports" });
            }

            var reportYear = year ?? DateTime.UtcNow.Year;

            var salesQuery = _context.SalesItems
                .Include(si => si.SalesOrder)
                .Include(si => si.Product)
                    .ThenInclude(p => p.ProductCategories)
                        .ThenInclude(pc => pc.Category)
                .Where(si => si.SalesOrder.OrderDate.Year == reportYear);

            if (month.HasValue)
            {
                salesQuery = salesQuery.Where(si => si.SalesOrder.OrderDate.Month == month.Value);
            }

            var salesItems = await salesQuery.ToListAsync();

            var categorySales = salesItems
                .SelectMany(si => si.Product.ProductCategories.Select(pc => new { si, Category = pc.Category }))
                .GroupBy(x => new { x.Category.Id, x.Category.Name })
                .Select(g => new CategorySalesData
                {
                    CategoryId = g.Key.Id,
                    CategoryName = g.Key.Name,
                    TotalRevenue = g.Sum(x => x.si.TotalPrice),
                    TotalQuantity = (int)g.Sum(x => x.si.Quantity),
                    OrderCount = g.Select(x => x.si.SalesOrderId).Distinct().Count(),
                    TopProducts = g.GroupBy(x => new { x.si.ProductId, x.si.Product.Name })
                        .Select(pg => new TopProductData
                        {
                            ProductId = pg.Key.ProductId,
                            ProductName = pg.Key.Name,
                            TotalQuantity = pg.Sum(x => x.si.Quantity),
                            TotalRevenue = pg.Sum(x => x.si.TotalPrice)
                        })
                        .OrderByDescending(p => p.TotalRevenue)
                        .Take(5)
                        .ToList()
                })
                .OrderByDescending(c => c.TotalRevenue)
                .ToList();

            var totalRevenue = categorySales.Sum(c => c.TotalRevenue);
            foreach (var cat in categorySales)
            {
                cat.PercentageOfTotal = totalRevenue > 0 ? (cat.TotalRevenue / totalRevenue) * 100 : 0;
            }

            return Ok(new CategorySalesResponse
            {
                Year = reportYear,
                Month = month,
                Categories = categorySales,
                TotalRevenue = totalRevenue,
                GeneratedAt = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating category sales report");
            return StatusCode(500, new { message = "An error occurred while generating the category sales report" });
        }
    }

    /// <summary>
    /// Get customer analytics
    /// </summary>
    [HttpGet("customer-analytics")]
    public async Task<ActionResult<CustomerAnalyticsResponse>> GetCustomerAnalytics()
    {
        try
        {
            var currentUserId = GetCurrentUserId();
            var currentUserRoles = await GetCurrentUserRoles(currentUserId);
            
            if (!currentUserRoles.Contains("SuperAdmin"))
            {
                return StatusCode(403, new { Message = "You don't have permission to view reports" });
            }

            var now = DateTime.UtcNow;
            var thisMonthStart = new DateTime(now.Year, now.Month, 1);
            var lastMonthStart = thisMonthStart.AddMonths(-1);

            // Get all registered customers with their orders
            var registeredCustomers = await _context.Customers
                .Include(c => c.SalesOrders)
                .Where(c => c.IsActive)
                .ToListAsync();

            // Get all guest customers from SalesOrders (orders without a linked CustomerId or with guest info)
            // Group by email first, then by phone if no email
            var guestOrdersByEmail = await _context.SalesOrders
                .Where(so => !string.IsNullOrEmpty(so.CustomerEmail))
                .GroupBy(so => so.CustomerEmail!.ToLower().Trim())
                .Select(g => new
                {
                    Email = g.Key,
                    Phone = g.OrderByDescending(so => so.CreatedAt).First().CustomerPhone,
                    Name = g.OrderByDescending(so => so.CreatedAt).First().CustomerName ?? "Guest",
                    Address = g.OrderByDescending(so => so.CreatedAt).First().CustomerAddress,
                    Orders = g.ToList(),
                    FirstOrderDate = g.Min(so => so.CreatedAt),
                    CreatedAt = g.Min(so => so.CreatedAt) // Use first order date as creation date
                })
                .ToListAsync();

            var guestOrdersByPhone = await _context.SalesOrders
                .Where(so => string.IsNullOrEmpty(so.CustomerEmail) && !string.IsNullOrEmpty(so.CustomerPhone))
                .GroupBy(so => so.CustomerPhone!.Trim())
                .Select(g => new
                {
                    Email = (string?)null,
                    Phone = g.Key,
                    Name = g.OrderByDescending(so => so.CreatedAt).First().CustomerName ?? "Guest",
                    Address = g.OrderByDescending(so => so.CreatedAt).First().CustomerAddress,
                    Orders = g.ToList(),
                    FirstOrderDate = g.Min(so => so.CreatedAt),
                    CreatedAt = g.Min(so => so.CreatedAt)
                })
                .ToListAsync();

            // Combine registered and guest customers
            // Create a unified customer list for analytics
            var allCustomers = new List<(int? CustomerId, string Name, string? Email, string? Phone, string? Address, DateTime CreatedAt, List<SalesOrder> Orders)>();

            // Add registered customers
            foreach (var customer in registeredCustomers)
            {
                allCustomers.Add((
                    customer.Id,
                    customer.FullName,
                    customer.Email,
                    customer.PhoneNumber,
                    customer.Address,
                    customer.CreatedAt,
                    customer.SalesOrders.ToList()
                ));
            }

            // Add guest customers (only those not already in registered customers)
            var registeredEmails = registeredCustomers
                .Where(c => !string.IsNullOrEmpty(c.Email))
                .Select(c => c.Email!.ToLower().Trim())
                .ToHashSet();
            
            var registeredPhones = registeredCustomers
                .Where(c => !string.IsNullOrEmpty(c.PhoneNumber))
                .Select(c => c.PhoneNumber!.Trim())
                .ToHashSet();

            foreach (var guest in guestOrdersByEmail)
            {
                // Skip if this email is already a registered customer
                if (!registeredEmails.Contains(guest.Email))
                {
                    allCustomers.Add((
                        null, // No CustomerId for guests
                        guest.Name,
                        guest.Email,
                        guest.Phone,
                        guest.Address,
                        guest.CreatedAt,
                        guest.Orders
                    ));
                }
            }

            foreach (var guest in guestOrdersByPhone)
            {
                // Skip if this phone is already a registered customer or if we already added by email
                if (!registeredPhones.Contains(guest.Phone) && 
                    (string.IsNullOrEmpty(guest.Email) || !registeredEmails.Contains(guest.Email.ToLower().Trim())))
                {
                    allCustomers.Add((
                        null, // No CustomerId for guests
                        guest.Name,
                        guest.Email,
                        guest.Phone,
                        guest.Address,
                        guest.CreatedAt,
                        guest.Orders
                    ));
                }
            }

            // Calculate statistics
            var totalCustomers = allCustomers.Count;
            var newThisMonth = allCustomers.Count(c => c.CreatedAt >= thisMonthStart);
            var newLastMonth = allCustomers.Count(c => c.CreatedAt >= lastMonthStart && c.CreatedAt < thisMonthStart);
            
            var growthPercentage = newLastMonth > 0 
                ? ((decimal)(newThisMonth - newLastMonth) / newLastMonth) * 100 
                : (newThisMonth > 0 ? 100 : 0);

            // Calculate CLV (only customers with orders)
            var customersWithOrders = allCustomers.Where(c => c.Orders.Any()).ToList();
            var avgClv = customersWithOrders.Any() 
                ? customersWithOrders.Average(c => c.Orders.Sum(o => o.TotalAmount)) 
                : 0;

            // Repeat purchase rate
            var repeatCustomers = customersWithOrders.Count(c => c.Orders.Count > 1);
            var repeatPurchaseRate = customersWithOrders.Any() 
                ? ((decimal)repeatCustomers / customersWithOrders.Count) * 100 
                : 0;

            // Top customers
            var topCustomers = customersWithOrders
                .OrderByDescending(c => c.Orders.Sum(o => o.TotalAmount))
                .Take(10)
                .Select((c, index) => new TopCustomerData
                {
                    // Generate unique negative ID for guest customers based on email/phone hash
                    CustomerId = c.CustomerId ?? -(Math.Abs((c.Email ?? c.Phone ?? $"guest-{index}").GetHashCode())),
                    CustomerName = c.Name,
                    Email = c.Email,
                    TotalSpent = c.Orders.Sum(o => o.TotalAmount),
                    OrderCount = c.Orders.Count,
                    LastOrderDate = c.Orders.Max(o => o.OrderDate),
                    AverageOrderValue = c.Orders.Average(o => o.TotalAmount)
                })
                .ToList();

            // Acquisition trend (last 12 months)
            var acquisitionTrend = Enumerable.Range(0, 12)
                .Select(i => thisMonthStart.AddMonths(-i))
                .Reverse()
                .Select(date => new CustomerAcquisitionData
                {
                    Date = date,
                    Label = date.ToString("MMM yyyy"),
                    NewCustomers = allCustomers.Count(c => c.CreatedAt.Year == date.Year && c.CreatedAt.Month == date.Month)
                })
                .ToList();

            // Geographic distribution (by city from address)
            var geoDistribution = allCustomers
                .Where(c => !string.IsNullOrEmpty(c.Address))
                .GroupBy(c => ExtractCity(c.Address ?? ""))
                .Where(g => !string.IsNullOrEmpty(g.Key))
                .Select(g => new GeographicDistributionData
                {
                    Region = g.Key,
                    CustomerCount = g.Count(),
                    TotalRevenue = g.Sum(c => c.Orders.Sum(o => o.TotalAmount))
                })
                .OrderByDescending(g => g.CustomerCount)
                .Take(10)
                .ToList();

            var totalGeoCustomers = geoDistribution.Sum(g => g.CustomerCount);
            foreach (var geo in geoDistribution)
            {
                geo.PercentageOfTotal = totalGeoCustomers > 0 ? ((decimal)geo.CustomerCount / totalGeoCustomers) * 100 : 0;
            }

            return Ok(new CustomerAnalyticsResponse
            {
                TotalCustomers = totalCustomers,
                NewCustomersThisMonth = newThisMonth,
                NewCustomersLastMonth = newLastMonth,
                CustomerGrowthPercentage = growthPercentage,
                AverageCustomerLifetimeValue = avgClv,
                RepeatPurchaseRate = repeatPurchaseRate,
                TopCustomers = topCustomers,
                AcquisitionTrend = acquisitionTrend,
                GeographicDistribution = geoDistribution,
                GeneratedAt = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating customer analytics");
            return StatusCode(500, new { message = "An error occurred while generating customer analytics" });
        }
    }

    /// <summary>
    /// Get inventory reports
    /// </summary>
    [HttpGet("inventory")]
    public async Task<ActionResult<InventoryReportResponse>> GetInventoryReport(
        [FromQuery] int deadStockDays = 30)
    {
        try
        {
            var currentUserId = GetCurrentUserId();
            var currentUserRoles = await GetCurrentUserRoles(currentUserId);
            
            if (!currentUserRoles.Contains("SuperAdmin"))
            {
                return StatusCode(403, new { Message = "You don't have permission to view reports" });
            }

            var now = DateTime.UtcNow;
            var deadStockCutoff = now.AddDays(-deadStockDays);

            // Get products with inventory
            var products = await _context.Products
                .Include(p => p.Variants)
                .Where(p => p.IsActive)
                .ToListAsync();

            var productInventories = await _context.ProductInventories
                .Include(pi => pi.Product)
                .Include(pi => pi.Warehouse)
                .ToListAsync();

            var variantInventories = await _context.VariantInventories
                .Include(vi => vi.ProductVariant)
                    .ThenInclude(pv => pv.Product)
                .ToListAsync();

            // Get sales data for turnover calculation
            var salesItems = await _context.SalesItems
                .Include(si => si.Product)
                .Include(si => si.SalesOrder)
                .Where(si => si.SalesOrder.OrderDate >= now.AddMonths(-3))
                .ToListAsync();

            // Calculate inventory value
            var totalInventoryValue = productInventories.Sum(pi => pi.Quantity * (pi.Product?.Price ?? 0))
                + variantInventories.Sum(vi => vi.Quantity * (vi.ProductVariant?.PriceOverride ?? vi.ProductVariant?.Product?.Price ?? 0));

            // Low stock and out of stock counts
            var lowStockCount = productInventories.Count(pi => pi.MinimumStockLevel.HasValue && pi.Quantity <= pi.MinimumStockLevel.Value && pi.Quantity > 0)
                + variantInventories.Count(vi => vi.MinimumStockLevel.HasValue && vi.Quantity <= vi.MinimumStockLevel.Value && vi.Quantity > 0);
            var outOfStockCount = productInventories.Count(pi => pi.Quantity == 0)
                + variantInventories.Count(vi => vi.Quantity == 0);

            // Inventory turnover by product
            var turnoverData = products
                .Select(p =>
                {
                    var productSales = salesItems.Where(si => si.ProductId == p.Id).Sum(si => si.Quantity);
                    var currentStock = productInventories.Where(pi => pi.ProductId == p.Id).Sum(pi => pi.Quantity);
                    var lastSale = salesItems.Where(si => si.ProductId == p.Id)
                        .OrderByDescending(si => si.SalesOrder.OrderDate)
                        .FirstOrDefault();
                    
                    return new InventoryTurnoverData
                    {
                        ProductId = p.Id,
                        ProductName = p.Name,
                        CurrentStock = currentStock,
                        SoldQuantity = productSales,
                        TurnoverRate = currentStock > 0 ? productSales / currentStock : 0,
                        DaysSinceLastSale = lastSale != null ? (int)(now - lastSale.SalesOrder.OrderDate).TotalDays : 999
                    };
                })
                .OrderByDescending(t => t.TurnoverRate)
                .Take(20)
                .ToList();

            // Dead stock
            var deadStock = products
                .Where(p =>
                {
                    var lastSale = salesItems.Where(si => si.ProductId == p.Id)
                        .OrderByDescending(si => si.SalesOrder.OrderDate)
                        .FirstOrDefault();
                    return lastSale == null || lastSale.SalesOrder.OrderDate < deadStockCutoff;
                })
                .Select(p =>
                {
                    var currentStock = productInventories.Where(pi => pi.ProductId == p.Id).Sum(pi => pi.Quantity);
                    var lastSale = salesItems.Where(si => si.ProductId == p.Id)
                        .OrderByDescending(si => si.SalesOrder.OrderDate)
                        .FirstOrDefault();
                    
                    return new DeadStockData
                    {
                        ProductId = p.Id,
                        ProductName = p.Name,
                        SKU = p.SKU,
                        CurrentStock = currentStock,
                        StockValue = currentStock * p.Price,
                        DaysWithoutSale = lastSale != null ? (int)(now - lastSale.SalesOrder.OrderDate).TotalDays : 999,
                        LastSaleDate = lastSale?.SalesOrder.OrderDate
                    };
                })
                .Where(d => d.CurrentStock > 0)
                .OrderByDescending(d => d.DaysWithoutSale)
                .Take(20)
                .ToList();

            // Variant performance - fetch data first, then order in memory (SQLite doesn't support decimal ORDER BY)
            var variantPerformanceQuery = await _context.SalesItems
                .Include(si => si.Product)
                .Include(si => si.ProductVariant)
                    .ThenInclude(pv => pv.Product)
                .Where(si => si.ProductVariantId != null && si.Product != null)
                .GroupBy(si => new { si.ProductVariantId, si.ProductVariant!.Attributes, si.ProductId, ProductName = si.Product!.Name })
                .Select(g => new VariantPerformanceData
                {
                    VariantId = g.Key.ProductVariantId ?? 0,
                    ProductId = g.Key.ProductId,
                    ProductName = g.Key.ProductName,
                    VariantAttributes = g.Key.Attributes ?? "",
                    TotalSold = g.Sum(si => si.Quantity),
                    Revenue = g.Sum(si => si.TotalPrice)
                })
                .ToListAsync();
            
            var variantPerformance = variantPerformanceQuery
                .OrderByDescending(v => v.TotalSold)
                .Take(20)
                .ToList();

            // Add current stock to variant performance
            foreach (var vp in variantPerformance)
            {
                vp.CurrentStock = variantInventories
                    .Where(vi => vi.ProductVariantId == vp.VariantId)
                    .Sum(vi => vi.Quantity);
            }

            // Stock forecast
            var stockForecast = products
                .Select(p =>
                {
                    var currentStock = productInventories.Where(pi => pi.ProductId == p.Id).Sum(pi => pi.Quantity);
                    var totalSold = salesItems.Where(si => si.ProductId == p.Id).Sum(si => si.Quantity);
                    var avgDailySales = totalSold / 90m; // 3 months
                    var daysUntilStockout = avgDailySales > 0 ? (int)(currentStock / avgDailySales) : 999;
                    
                    return new StockForecastData
                    {
                        ProductId = p.Id,
                        ProductName = p.Name,
                        CurrentStock = currentStock,
                        AverageDailySales = avgDailySales,
                        DaysUntilStockout = daysUntilStockout,
                        PredictedStockoutDate = avgDailySales > 0 ? now.AddDays(daysUntilStockout) : null,
                        RiskLevel = daysUntilStockout <= 7 ? "Critical" :
                                   daysUntilStockout <= 14 ? "High" :
                                   daysUntilStockout <= 30 ? "Medium" : "Low"
                    };
                })
                .Where(f => f.CurrentStock > 0 && f.AverageDailySales > 0)
                .OrderBy(f => f.DaysUntilStockout)
                .Take(20)
                .ToList();

            return Ok(new InventoryReportResponse
            {
                TotalProducts = products.Count,
                TotalVariants = products.Sum(p => p.Variants?.Count ?? 0),
                TotalInventoryValue = totalInventoryValue,
                LowStockCount = lowStockCount,
                OutOfStockCount = outOfStockCount,
                TurnoverByProduct = turnoverData,
                DeadStock = deadStock,
                VariantPerformance = variantPerformance,
                StockForecast = stockForecast,
                GeneratedAt = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating inventory report");
            return StatusCode(500, new { message = "An error occurred while generating the inventory report" });
        }
    }

    /// <summary>
    /// Get financial report with profit/loss analysis
    /// </summary>
    [HttpGet("financial")]
    public async Task<ActionResult<FinancialReportResponse>> GetFinancialReport(
        [FromQuery] int? year = null,
        [FromQuery] int? month = null)
    {
        try
        {
            var currentUserId = GetCurrentUserId();
            var currentUserRoles = await GetCurrentUserRoles(currentUserId);
            
            if (!currentUserRoles.Contains("SuperAdmin"))
            {
                return StatusCode(403, new { Message = "You don't have permission to view reports" });
            }

            var reportYear = year ?? DateTime.UtcNow.Year;

            // Get sales data
            var salesQuery = _context.SalesOrders
                .Include(so => so.SalesItems)
                    .ThenInclude(si => si.Product)
                .Where(so => so.OrderDate.Year == reportYear);

            if (month.HasValue)
            {
                salesQuery = salesQuery.Where(so => so.OrderDate.Month == month.Value);
            }

            var sales = await salesQuery.ToListAsync();

            // Get purchase costs
            var purchaseQuery = _context.PurchaseOrders
                .Where(po => po.OrderDate.Year == reportYear && po.Status == "Received");

            if (month.HasValue)
            {
                purchaseQuery = purchaseQuery.Where(po => po.OrderDate.Month == month.Value);
            }

            var purchases = await purchaseQuery.ToListAsync();

            // Get promo code discounts
            var discountsQuery = _context.PromoCodeUsages
                .Include(pcu => pcu.SalesOrder)
                .Where(pcu => pcu.SalesOrder.OrderDate.Year == reportYear);

            if (month.HasValue)
            {
                discountsQuery = discountsQuery.Where(pcu => pcu.SalesOrder.OrderDate.Month == month.Value);
            }

            var discounts = await discountsQuery.ToListAsync();

            var grossRevenue = sales.Sum(s => s.TotalAmount);
            var totalCosts = purchases.Sum(p => p.TotalAmount);
            var totalDiscounts = discounts.Sum(d => d.DiscountAmount);
            var grossProfit = grossRevenue - totalCosts;
            var grossMargin = grossRevenue > 0 ? (grossProfit / grossRevenue) * 100 : 0;
            var netRevenue = grossRevenue - totalDiscounts;

            // Product margins
            var productMargins = sales
                .SelectMany(s => s.SalesItems)
                .Where(si => si.Product != null)
                .GroupBy(si => new { si.ProductId, si.Product!.Name, si.Product.Price, si.Product.Cost })
                .Select(g =>
                {
                    var cost = g.Key.Cost ?? 0;
                    var margin = g.Key.Price - cost;
                    var marginPct = g.Key.Price > 0 ? (margin / g.Key.Price) * 100 : 0;
                    var totalRevenue = g.Sum(si => si.TotalPrice);
                    var totalProfit = g.Sum(si => (si.UnitPrice - cost) * si.Quantity);

                    return new ProductMarginData
                    {
                        ProductId = g.Key.ProductId,
                        ProductName = g.Key.Name,
                        Price = g.Key.Price,
                        Cost = cost,
                        GrossMargin = margin,
                        GrossMarginPercentage = marginPct,
                        TotalRevenue = totalRevenue,
                        TotalProfit = totalProfit,
                        QuantitySold = (int)g.Sum(si => si.Quantity)
                    };
                })
                .OrderByDescending(pm => pm.TotalProfit)
                .Take(20)
                .ToList();

            // Monthly trend
            var monthlyTrend = Enumerable.Range(1, 12)
                .Select(m =>
                {
                    var monthSales = sales.Where(s => s.OrderDate.Month == m);
                    var monthPurchases = purchases.Where(p => p.OrderDate.Month == m);
                    var revenue = monthSales.Sum(s => s.TotalAmount);
                    var costs = monthPurchases.Sum(p => p.TotalAmount);
                    var profit = revenue - costs;

                    return new MonthlyFinancialData
                    {
                        Month = m,
                        MonthName = new DateTime(reportYear, m, 1).ToString("MMM"),
                        Revenue = revenue,
                        Costs = costs,
                        Profit = profit,
                        MarginPercentage = revenue > 0 ? (profit / revenue) * 100 : 0
                    };
                })
                .ToList();

            // Payment status summary
            var paymentStatus = new PaymentStatusSummary
            {
                PaidAmount = sales.Where(s => s.PaymentStatus == "Paid").Sum(s => s.TotalAmount),
                PaidOrders = sales.Count(s => s.PaymentStatus == "Paid"),
                PendingAmount = sales.Where(s => s.PaymentStatus == "Pending").Sum(s => s.TotalAmount),
                PendingOrders = sales.Count(s => s.PaymentStatus == "Pending"),
                RefundedAmount = sales.Where(s => s.PaymentStatus == "Refunded").Sum(s => s.TotalAmount),
                RefundedOrders = sales.Count(s => s.PaymentStatus == "Refunded")
            };

            return Ok(new FinancialReportResponse
            {
                Year = reportYear,
                Month = month,
                GrossRevenue = grossRevenue,
                TotalCosts = totalCosts,
                GrossProfit = grossProfit,
                GrossMarginPercentage = grossMargin,
                TotalDiscounts = totalDiscounts,
                NetRevenue = netRevenue,
                ProductMargins = productMargins,
                MonthlyTrend = monthlyTrend,
                PaymentStatus = paymentStatus,
                GeneratedAt = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating financial report");
            return StatusCode(500, new { message = "An error occurred while generating the financial report" });
        }
    }

    /// <summary>
    /// Get promo code analytics
    /// </summary>
    [HttpGet("promo-analytics")]
    public async Task<ActionResult<PromoCodeAnalyticsResponse>> GetPromoCodeAnalytics()
    {
        try
        {
            var currentUserId = GetCurrentUserId();
            var currentUserRoles = await GetCurrentUserRoles(currentUserId);
            
            if (!currentUserRoles.Contains("SuperAdmin"))
            {
                return StatusCode(403, new { Message = "You don't have permission to view reports" });
            }

            var now = DateTime.UtcNow;

            var promoCodes = await _context.PromoCodes
                .Include(pc => pc.PromoCodeUsages)
                    .ThenInclude(pcu => pcu.SalesOrder)
                .ToListAsync();

            var totalPromoCodes = promoCodes.Count;
            var activePromoCodes = promoCodes.Count(pc => pc.IsActive && (!pc.EndDate.HasValue || pc.EndDate > now));
            
            var allUsages = promoCodes.SelectMany(pc => pc.PromoCodeUsages).ToList();
            var totalDiscountsGiven = allUsages.Sum(u => u.DiscountAmount);
            var totalUsageCount = allUsages.Count;
            var avgDiscountPerUse = totalUsageCount > 0 ? totalDiscountsGiven / totalUsageCount : 0;

            // Conversion rates
            var totalOrders = await _context.SalesOrders.CountAsync();
            var ordersWithPromo = allUsages.Select(u => u.SalesOrderId).Distinct().Count();
            var conversionWithPromo = ordersWithPromo > 0 && totalOrders > 0 
                ? ((decimal)ordersWithPromo / totalOrders) * 100 
                : 0;

            // Top promo codes
            var topPromoCodes = promoCodes
                .Select(pc =>
                {
                    var usages = pc.PromoCodeUsages.ToList();
                    var discountGiven = usages.Sum(u => u.DiscountAmount);
                    var revenueGenerated = usages.Sum(u => u.SalesOrder?.TotalAmount ?? 0);
                    var roi = discountGiven > 0 ? ((revenueGenerated - discountGiven) / discountGiven) * 100 : 0;

                    return new PromoCodePerformanceData
                    {
                        PromoCodeId = pc.Id,
                        Code = pc.Code,
                        DiscountType = pc.DiscountType,
                        DiscountValue = pc.DiscountValue,
                        UsageCount = pc.UsedCount,
                        UsageLimit = pc.UsageLimit ?? 0,
                        TotalDiscountGiven = discountGiven,
                        RevenueGenerated = revenueGenerated,
                        ROI = roi,
                        IsActive = pc.IsActive,
                        ExpiryDate = pc.EndDate
                    };
                })
                .OrderByDescending(p => p.UsageCount)
                .Take(10)
                .ToList();

            // Usage trend (last 30 days)
            var usageTrend = Enumerable.Range(0, 30)
                .Select(i => now.Date.AddDays(-i))
                .Reverse()
                .Select(date => new PromoCodeUsageTrendData
                {
                    Date = date,
                    Label = date.ToString("MMM dd"),
                    UsageCount = allUsages.Count(u => u.UsedAt.Date == date),
                    DiscountAmount = allUsages.Where(u => u.UsedAt.Date == date).Sum(u => u.DiscountAmount)
                })
                .ToList();

            return Ok(new PromoCodeAnalyticsResponse
            {
                TotalPromoCodes = totalPromoCodes,
                ActivePromoCodes = activePromoCodes,
                TotalDiscountsGiven = totalDiscountsGiven,
                TotalUsageCount = totalUsageCount,
                AverageDiscountPerUse = avgDiscountPerUse,
                ConversionRateWithPromo = conversionWithPromo,
                ConversionRateWithoutPromo = 100 - conversionWithPromo,
                TopPromoCodes = topPromoCodes,
                UsageTrend = usageTrend,
                GeneratedAt = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating promo code analytics");
            return StatusCode(500, new { message = "An error occurred while generating promo code analytics" });
        }
    }

    /// <summary>
    /// Get order status distribution
    /// </summary>
    [HttpGet("order-status")]
    public async Task<ActionResult<OrderStatusDistributionResponse>> GetOrderStatusDistribution(
        [FromQuery] int? year = null,
        [FromQuery] int? month = null)
    {
        try
        {
            var currentUserId = GetCurrentUserId();
            var currentUserRoles = await GetCurrentUserRoles(currentUserId);
            
            if (!currentUserRoles.Contains("SuperAdmin"))
            {
                return StatusCode(403, new { Message = "You don't have permission to view reports" });
            }

            var reportYear = year ?? DateTime.UtcNow.Year;

            var ordersQuery = _context.SalesOrders.Where(so => so.OrderDate.Year == reportYear);

            if (month.HasValue)
            {
                ordersQuery = ordersQuery.Where(so => so.OrderDate.Month == month.Value);
            }

            var orders = await ordersQuery.ToListAsync();

            var totalOrders = orders.Count;

            // Status distribution
            var statusDistribution = orders
                .GroupBy(o => o.Status)
                .Select(g => new OrderStatusData
                {
                    Status = g.Key,
                    Count = g.Count(),
                    Percentage = totalOrders > 0 ? ((decimal)g.Count() / totalOrders) * 100 : 0,
                    TotalValue = g.Sum(o => o.TotalAmount)
                })
                .OrderByDescending(s => s.Count)
                .ToList();

            // Status trend (last 30 days)
            var now = DateTime.UtcNow;
            var statusTrend = Enumerable.Range(0, 30)
                .Select(i => now.Date.AddDays(-i))
                .Reverse()
                .Select(date =>
                {
                    var dayOrders = orders.Where(o => o.OrderDate.Date == date);
                    return new OrderStatusTrendData
                    {
                        Date = date,
                        Label = date.ToString("MMM dd"),
                        Pending = dayOrders.Count(o => o.Status == "Pending"),
                        Confirmed = dayOrders.Count(o => o.Status == "Confirmed"),
                        Shipped = dayOrders.Count(o => o.Status == "Shipped"),
                        Delivered = dayOrders.Count(o => o.Status == "Delivered"),
                        Cancelled = dayOrders.Count(o => o.Status == "Cancelled")
                    };
                })
                .ToList();

            // Average fulfillment time (from created to delivered)
            // Use actual delivery time: DeliveryDate if set and not in the future, otherwise UpdatedAt when status is Delivered
            var deliveredOrders = orders.Where(o => o.Status == "Delivered").ToList();
            var avgFulfillmentTime = 0.0;
            
            if (deliveredOrders.Any())
            {
                var fulfillmentTimes = deliveredOrders.Select(o =>
                {
                    DateTime actualDeliveryTime;
                    
                    // Use DeliveryDate if it's set and not in the future (actual delivery)
                    // Otherwise use UpdatedAt as the time when status was changed to Delivered
                    if (o.DeliveryDate.HasValue && o.DeliveryDate.Value <= DateTime.UtcNow)
                    {
                        actualDeliveryTime = o.DeliveryDate.Value;
                    }
                    else if (o.UpdatedAt.HasValue)
                    {
                        actualDeliveryTime = o.UpdatedAt.Value;
                    }
                    else
                    {
                        // Fallback to CreatedAt if neither is available (shouldn't happen)
                        actualDeliveryTime = o.CreatedAt;
                    }
                    
                    return (actualDeliveryTime - o.CreatedAt).TotalHours;
                }).ToList();
                
                avgFulfillmentTime = fulfillmentTimes.Any() ? fulfillmentTimes.Average() : 0;
            }

            return Ok(new OrderStatusDistributionResponse
            {
                TotalOrders = totalOrders,
                StatusDistribution = statusDistribution,
                StatusTrend = statusTrend,
                AverageFulfillmentTimeHours = (decimal)avgFulfillmentTime,
                GeneratedAt = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating order status distribution");
            return StatusCode(500, new { message = "An error occurred while generating order status distribution" });
        }
    }

    // Helper methods
    private int GetWeekOfYear(DateTime date)
    {
        var cal = System.Globalization.CultureInfo.CurrentCulture.Calendar;
        return cal.GetWeekOfYear(date, System.Globalization.CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday);
    }

    private DateTime FirstDateOfWeek(int year, int weekOfYear)
    {
        var jan1 = new DateTime(year, 1, 1);
        var daysOffset = DayOfWeek.Monday - jan1.DayOfWeek;
        var firstMonday = jan1.AddDays(daysOffset);
        var firstWeek = System.Globalization.CultureInfo.CurrentCulture.Calendar.GetWeekOfYear(jan1, System.Globalization.CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday);
        if (firstWeek <= 1) weekOfYear -= 1;
        return firstMonday.AddDays(weekOfYear * 7);
    }

    private string ExtractCity(string address)
    {
        if (string.IsNullOrWhiteSpace(address)) return "";
        var parts = address.Split(new[] { ',', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length >= 2)
        {
            return parts[^2].Trim(); // Second to last part is often the city
        }
        return parts.Length > 0 ? parts[0].Trim() : "";
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
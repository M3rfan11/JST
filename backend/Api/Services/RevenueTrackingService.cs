using Api.Data;
using Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace Api.Services
{
    public class RevenueTrackingService : IRevenueTrackingService
    {
        private readonly ApplicationDbContext _context;
        private readonly IMemoryCache _cache;
        private readonly ILogger<RevenueTrackingService> _logger;
        private const string TOTAL_REVENUE_KEY = "total_revenue";
        private const string TOTAL_COSTS_KEY = "total_costs";
        private const string STORE_REVENUE_KEY = "store_revenue_{0}";
        private const string STORE_COSTS_KEY = "store_costs_{0}";

        public RevenueTrackingService(ApplicationDbContext context, IMemoryCache cache, ILogger<RevenueTrackingService> logger)
        {
            _context = context;
            _cache = cache;
            _logger = logger;
        }

        public async Task UpdateRevenueAfterSaleAsync(int salesOrderId, decimal amount)
        {
            try
            {
                // Update total revenue
                var currentTotal = await GetTotalRevenueAsync();
                var newTotal = currentTotal + amount;
                _cache.Set(TOTAL_REVENUE_KEY, newTotal, TimeSpan.FromHours(24));

                // Update store-specific revenue
                var salesOrder = await _context.SalesOrders
                    .Include(so => so.SalesItems)
                    .FirstOrDefaultAsync(so => so.Id == salesOrderId);

                if (salesOrder?.SalesItems?.Any() == true)
                {
                    var storeId = salesOrder.SalesItems.First().WarehouseId;
                    var currentStoreRevenue = await GetStoreRevenueAsync(storeId);
                    var newStoreRevenue = currentStoreRevenue + amount;
                    _cache.Set(string.Format(STORE_REVENUE_KEY, storeId), newStoreRevenue, TimeSpan.FromHours(24));
                }

                _logger.LogInformation("Updated revenue after sale {SalesOrderId}: +${Amount}", salesOrderId, amount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating revenue after sale {SalesOrderId}", salesOrderId);
            }
        }

        public async Task UpdateCostsAfterPurchaseAsync(int purchaseOrderId, decimal amount)
        {
            try
            {
                // Update total costs
                var currentTotal = await GetTotalCostsAsync();
                var newTotal = currentTotal + amount;
                _cache.Set(TOTAL_COSTS_KEY, newTotal, TimeSpan.FromHours(24));

                // Update store-specific costs
                var purchaseOrder = await _context.PurchaseOrders
                    .Include(po => po.PurchaseItems)
                    .FirstOrDefaultAsync(po => po.Id == purchaseOrderId);

                if (purchaseOrder?.PurchaseItems?.Any() == true)
                {
                    var storeId = purchaseOrder.PurchaseItems.First().WarehouseId;
                    var currentStoreCosts = await GetStoreCostsAsync(storeId);
                    var newStoreCosts = currentStoreCosts + amount;
                    _cache.Set(string.Format(STORE_COSTS_KEY, storeId), newStoreCosts, TimeSpan.FromHours(24));
                }

                _logger.LogInformation("Updated costs after purchase {PurchaseOrderId}: +${Amount}", purchaseOrderId, amount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating costs after purchase {PurchaseOrderId}", purchaseOrderId);
            }
        }

        public async Task<decimal> GetTotalRevenueAsync()
        {
            // Always calculate from database to ensure real-time accuracy
            // This ensures revenue updates immediately when orders are completed/delivered
            var totalRevenue = await _context.SalesOrders
                .Where(so => so.Status == "Completed" || so.Status == "Delivered")
                .SumAsync(so => so.TotalAmount);

            // Update cache for potential future use, but always recalculate from DB
            _cache.Set(TOTAL_REVENUE_KEY, totalRevenue, TimeSpan.FromMinutes(5));
            return totalRevenue;
        }

        public async Task<decimal> GetTotalCostsAsync()
        {
            // Always calculate from database to ensure real-time accuracy
            var totalCosts = await _context.PurchaseOrders
                .Where(po => po.Status == "Received")
                .SumAsync(po => po.TotalAmount);

            // Update cache for potential future use, but always recalculate from DB
            _cache.Set(TOTAL_COSTS_KEY, totalCosts, TimeSpan.FromMinutes(5));
            return totalCosts;
        }

        public async Task<decimal> GetStoreRevenueAsync(int storeId)
        {
            // Always calculate from database to ensure real-time accuracy
            var storeRevenue = await _context.SalesItems
                .Include(si => si.SalesOrder)
                .Where(si => si.WarehouseId == storeId && 
                            (si.SalesOrder.Status == "Completed" || si.SalesOrder.Status == "Delivered"))
                .SumAsync(si => si.TotalPrice);

            // Update cache for potential future use, but always recalculate from DB
            var cacheKey = string.Format(STORE_REVENUE_KEY, storeId);
            _cache.Set(cacheKey, storeRevenue, TimeSpan.FromMinutes(5));
            return storeRevenue;
        }

        public async Task<decimal> GetStoreCostsAsync(int storeId)
        {
            // Always calculate from database to ensure real-time accuracy
            var storeCosts = await _context.PurchaseItems
                .Include(pi => pi.PurchaseOrder)
                .Where(pi => pi.WarehouseId == storeId && pi.PurchaseOrder.Status == "Received")
                .SumAsync(pi => pi.TotalPrice);

            // Update cache for potential future use, but always recalculate from DB
            var cacheKey = string.Format(STORE_COSTS_KEY, storeId);
            _cache.Set(cacheKey, storeCosts, TimeSpan.FromMinutes(5));
            return storeCosts;
        }

        public async Task RefreshAllTotalsAsync()
        {
            try
            {
                _logger.LogInformation("Refreshing all revenue and cost totals...");

                // Clear cache
                _cache.Remove(TOTAL_REVENUE_KEY);
                _cache.Remove(TOTAL_COSTS_KEY);

                // Refresh all store totals
                var stores = await _context.Warehouses.Select(w => w.Id).ToListAsync();
                foreach (var storeId in stores)
                {
                    _cache.Remove(string.Format(STORE_REVENUE_KEY, storeId));
                    _cache.Remove(string.Format(STORE_COSTS_KEY, storeId));
                }

                // Pre-calculate totals
                await GetTotalRevenueAsync();
                await GetTotalCostsAsync();
                foreach (var storeId in stores)
                {
                    await GetStoreRevenueAsync(storeId);
                    await GetStoreCostsAsync(storeId);
                }

                _logger.LogInformation("Successfully refreshed all revenue and cost totals");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error refreshing revenue and cost totals");
            }
        }
    }
}

using Api.Models;

namespace Api.Services
{
    public interface IRevenueTrackingService
    {
        Task UpdateRevenueAfterSaleAsync(int salesOrderId, decimal amount);
        Task UpdateCostsAfterPurchaseAsync(int purchaseOrderId, decimal amount);
        Task<decimal> GetTotalRevenueAsync();
        Task<decimal> GetTotalCostsAsync();
        Task<decimal> GetStoreRevenueAsync(int storeId);
        Task<decimal> GetStoreCostsAsync(int storeId);
        Task RefreshAllTotalsAsync();
    }
}

namespace Api.Services
{
    public interface IEmailService
    {
        Task<bool> SendPromoCodeNotificationAsync(string toEmail, string toName, string promoCode, decimal discountValue, string discountType, DateTime? endDate);
        
        /// <summary>
        /// Sends promo code notifications to multiple recipients in parallel
        /// </summary>
        /// <param name="recipients">List of recipients with email and name</param>
        /// <param name="promoCode">Promo code</param>
        /// <param name="discountValue">Discount value</param>
        /// <param name="discountType">Discount type (Percentage or FixedAmount)</param>
        /// <param name="endDate">End date of promo code</param>
        /// <returns>Dictionary of email addresses and their send status</returns>
        Task<Dictionary<string, bool>> SendPromoCodeNotificationsBatchAsync(
            List<(string Email, string Name)> recipients,
            string promoCode,
            decimal discountValue,
            string discountType,
            DateTime? endDate);
    }
}





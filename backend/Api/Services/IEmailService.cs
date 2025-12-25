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
        
        /// <summary>
        /// Sends order confirmation email to customer
        /// </summary>
        /// <param name="toEmail">Customer email address</param>
        /// <param name="toName">Customer name</param>
        /// <param name="orderNumber">Order number</param>
        /// <param name="orderDate">Order date</param>
        /// <param name="items">Order items with product names, quantities, and prices</param>
        /// <param name="subtotal">Order subtotal</param>
        /// <param name="discount">Discount amount (if any)</param>
        /// <param name="shipping">Shipping cost</param>
        /// <param name="total">Total amount</param>
        /// <param name="paymentMethod">Payment method</param>
        /// <param name="shippingAddress">Shipping address</param>
        /// <returns>True if email sent successfully, false otherwise</returns>
        Task<bool> SendOrderConfirmationAsync(
            string toEmail,
            string toName,
            string orderNumber,
            DateTime orderDate,
            List<(string ProductName, string? Size, decimal Quantity, decimal UnitPrice, decimal TotalPrice)> items,
            decimal subtotal,
            decimal discount,
            decimal shipping,
            decimal total,
            string paymentMethod,
            string shippingAddress);
    }
}





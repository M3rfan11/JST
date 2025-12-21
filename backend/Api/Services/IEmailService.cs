namespace Api.Services
{
    public interface IEmailService
    {
        Task<bool> SendPromoCodeNotificationAsync(string toEmail, string toName, string promoCode, decimal discountValue, string discountType, DateTime? endDate);
    }
}





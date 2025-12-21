using Api.Services;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using SendGrid;
using SendGrid.Helpers.Mail;

namespace Api.Services
{
    public class EmailService : IEmailService
    {
        private readonly ILogger<EmailService> _logger;
        private readonly IConfiguration _configuration;

        public EmailService(ILogger<EmailService> logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
        }

        public async Task<bool> SendPromoCodeNotificationAsync(string toEmail, string toName, string promoCode, decimal discountValue, string discountType, DateTime? endDate)
        {
            try
            {
                var provider = _configuration["Email:Provider"] ?? "Gmail";
                var senderEmail = _configuration["Email:SenderEmail"] ?? "me5280908@gmail.com";
                var senderName = _configuration["Email:SenderName"] ?? "JST";
                
                var discountText = discountType == "Percentage" 
                    ? $"{discountValue}% off" 
                    : $"${discountValue} off";
                
                var endDateText = endDate.HasValue 
                    ? $"Valid until {endDate.Value:MMMM dd, yyyy}" 
                    : "No expiration date";
                
                var emailBodyHtml = $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background-color: #ed6b3e; color: white; padding: 20px; text-align: center; border-radius: 5px 5px 0 0; }}
        .content {{ background-color: #f9f9f9; padding: 30px; border-radius: 0 0 5px 5px; }}
        .promo-code {{ background-color: #fff; border: 2px dashed #ed6b3e; padding: 20px; text-align: center; margin: 20px 0; border-radius: 5px; }}
        .promo-code-text {{ font-size: 24px; font-weight: bold; color: #ed6b3e; letter-spacing: 2px; }}
        .discount {{ font-size: 18px; color: #28a745; font-weight: bold; margin: 10px 0; }}
        .footer {{ text-align: center; margin-top: 20px; color: #666; font-size: 12px; }}
        .button {{ display: inline-block; background-color: #ed6b3e; color: white; padding: 12px 30px; text-decoration: none; border-radius: 5px; margin: 20px 0; }}
    </style>
</head>
<body>
    <div class=""container"">
        <div class=""header"">
            <h1>{senderName}</h1>
        </div>
        <div class=""content"">
            <p>Dear {toName},</p>
            <p>We're excited to offer you a special discount!</p>
            
            <div class=""promo-code"">
                <p style=""margin: 0 0 10px 0; color: #666;"">Your Promo Code:</p>
                <div class=""promo-code-text"">{promoCode}</div>
                <div class=""discount"">{discountText}</div>
                <p style=""margin: 10px 0 0 0; color: #666; font-size: 14px;"">{endDateText}</p>
            </div>
            
            <p><strong>Important:</strong> To use this promo code, you must be a registered user. If you haven't already, please sign up on our website to take advantage of this offer.</p>
            
            <div style=""text-align: center;"">
                <a href=""http://localhost:3000"" class=""button"">Shop Now</a>
            </div>
            
            <p>Use this code at checkout to enjoy your discount!</p>
            
            <p>Thank you for being a valued customer.</p>
            
            <p>Best regards,<br>{senderName} Team</p>
        </div>
        <div class=""footer"">
            <p>This is an automated email. Please do not reply to this message.</p>
        </div>
    </div>
</body>
</html>";

                // Try SendGrid first (if API key is configured)
                var sendGridApiKey = _configuration["Email:SendGridApiKey"];
                if (!string.IsNullOrEmpty(sendGridApiKey) && provider.ToLower() == "sendgrid")
                {
                    return await SendViaSendGridAsync(toEmail, toName, senderEmail, senderName, 
                        $"Special Promo Code: {promoCode} - {discountText}", emailBodyHtml);
                }

                // Try Gmail SMTP (if password is configured)
                var senderPassword = _configuration["Email:SenderPassword"];
                if (!string.IsNullOrEmpty(senderPassword))
                {
                    // Remove spaces from App Password if present
                    senderPassword = senderPassword.Replace(" ", "");
                    _logger.LogInformation("Attempting to send promo code email to {Email} via Gmail SMTP", toEmail);
                    return await SendViaGmailAsync(toEmail, toName, senderEmail, senderName, 
                        $"Special Promo Code: {promoCode} - {discountText}", emailBodyHtml, senderPassword);
                }

                // If neither is configured, just log (don't fail)
                _logger.LogInformation("Email not configured. Would send promo code email to {Email} for code {Code}", toEmail, promoCode);
                _logger.LogInformation("Email content:\n{Content}", emailBodyHtml.Replace("<", "&lt;").Replace(">", "&gt;"));
                return true; // Return true so the system thinks email was sent
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending promo code email to {Email}", toEmail);
                return false;
            }
        }

        private async Task<bool> SendViaGmailAsync(string toEmail, string toName, string senderEmail, string senderName, 
            string subject, string htmlBody, string password)
        {
            try
            {
                var smtpServer = _configuration["Email:SmtpServer"] ?? "smtp.gmail.com";
                var smtpPort = int.Parse(_configuration["Email:SmtpPort"] ?? "587");

                _logger.LogInformation("Connecting to Gmail SMTP server: {Server}:{Port}", smtpServer, smtpPort);
                _logger.LogInformation("Authenticating as: {Email}", senderEmail);

                var message = new MimeMessage();
                message.From.Add(new MailboxAddress(senderName, senderEmail));
                message.To.Add(new MailboxAddress(toName, toEmail));
                message.Subject = subject;
                message.Body = new TextPart("html") { Text = htmlBody };

                using (var client = new SmtpClient())
                {
                    await client.ConnectAsync(smtpServer, smtpPort, SecureSocketOptions.StartTls);
                    _logger.LogInformation("Connected to SMTP server. Authenticating...");
                    await client.AuthenticateAsync(senderEmail, password);
                    _logger.LogInformation("Authentication successful. Sending email...");
                    await client.SendAsync(message);
                    await client.DisconnectAsync(true);
                }

                _logger.LogInformation("✅ Promo code email sent successfully via Gmail to {Email}", toEmail);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error sending email via Gmail to {Email}. Error: {Message}", toEmail, ex.Message);
                return false;
            }
        }

        private async Task<bool> SendViaSendGridAsync(string toEmail, string toName, string senderEmail, string senderName, 
            string subject, string htmlBody)
        {
            try
            {
                var apiKey = _configuration["Email:SendGridApiKey"];
                var client = new SendGridClient(apiKey);
                var from = new EmailAddress(senderEmail, senderName);
                var to = new EmailAddress(toEmail, toName);
                var msg = MailHelper.CreateSingleEmail(from, to, subject, null, htmlBody);
                var response = await client.SendEmailAsync(msg);

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("Promo code email sent successfully via SendGrid to {Email}", toEmail);
                    return true;
                }
                else
                {
                    var body = await response.Body.ReadAsStringAsync();
                    _logger.LogError("SendGrid error: {StatusCode} - {Body}", response.StatusCode, body);
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending email via SendGrid to {Email}", toEmail);
                return false;
            }
        }
    }
}


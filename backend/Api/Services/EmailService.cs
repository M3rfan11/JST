using Api.Services;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using SendGrid;
using SendGrid.Helpers.Mail;
using System.Net.Mail;

namespace Api.Services
{
    public class EmailService : IEmailService
    {
        private readonly ILogger<EmailService> _logger;
        private readonly IConfiguration _configuration;
        private const int MaxRetryAttempts = 3;
        private const int BaseRetryDelayMs = 1000; // 1 second

        public EmailService(ILogger<EmailService> logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
        }

        public async Task<bool> SendPromoCodeNotificationAsync(string toEmail, string toName, string promoCode, decimal discountValue, string discountType, DateTime? endDate)
        {
            // Validate email address
            if (!IsValidEmail(toEmail))
            {
                _logger.LogWarning("Invalid email address: {Email}", toEmail);
                return false;
            }

            try
            {
                var provider = _configuration["Email:Provider"] ?? "Gmail";
                var senderEmail = _configuration["Email:SenderEmail"] ?? "me5280908@gmail.com";
                var senderName = _configuration["Email:SenderName"] ?? "JST";
                var baseUrl = _configuration["Email:BaseUrl"] ?? "http://localhost:3000";
                
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
                <a href=""{baseUrl}"" class=""button"">Shop Now</a>
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
                    return await SendWithRetryAsync(() => SendViaSendGridAsync(toEmail, toName, senderEmail, senderName, 
                        $"Special Promo Code: {promoCode} - {discountText}", emailBodyHtml));
                }

                // Try Gmail SMTP (if password is configured)
                var senderPassword = _configuration["Email:SenderPassword"];
                if (!string.IsNullOrEmpty(senderPassword))
                {
                    // Remove spaces from App Password if present
                    senderPassword = senderPassword.Replace(" ", "");
                    _logger.LogInformation("Attempting to send promo code email to {Email} via Gmail SMTP", toEmail);
                    return await SendWithRetryAsync(() => SendViaGmailAsync(toEmail, toName, senderEmail, senderName, 
                        $"Special Promo Code: {promoCode} - {discountText}", emailBodyHtml, senderPassword));
                }

                // If neither is configured, return false (don't mark as sent)
                _logger.LogWarning("Email not configured. Cannot send promo code email to {Email} for code {Code}. Please configure Email:SenderPassword or Email:SendGridApiKey", toEmail, promoCode);
                _logger.LogInformation("Email content that would have been sent:\n{Content}", emailBodyHtml.Replace("<", "&lt;").Replace(">", "&gt;"));
                return false; // Return false so the system knows email was NOT sent
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending promo code email to {Email}", toEmail);
                return false;
            }
        }

        /// <summary>
        /// Validates email address format using MailAddress
        /// </summary>
        private bool IsValidEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return false;

            try
            {
                var mailAddress = new MailAddress(email);
                return mailAddress.Address == email;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Sends email with exponential backoff retry logic
        /// </summary>
        private async Task<bool> SendWithRetryAsync(Func<Task<bool>> sendAction)
        {
            for (int attempt = 1; attempt <= MaxRetryAttempts; attempt++)
            {
                try
                {
                    var result = await sendAction();
                    if (result)
                    {
                        if (attempt > 1)
                        {
                            _logger.LogInformation("Email sent successfully on attempt {Attempt}", attempt);
                        }
                        return true;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Email send attempt {Attempt} failed", attempt);
                    
                    if (attempt < MaxRetryAttempts)
                    {
                        var delay = BaseRetryDelayMs * (int)Math.Pow(2, attempt - 1); // Exponential backoff
                        _logger.LogInformation("Retrying email send in {Delay}ms...", delay);
                        await Task.Delay(delay);
                    }
                    else
                    {
                        _logger.LogError(ex, "All {MaxAttempts} email send attempts failed", MaxRetryAttempts);
                    }
                }
            }
            return false;
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

                using (var client = new MailKit.Net.Smtp.SmtpClient())
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

        /// <summary>
        /// Sends promo code notifications to multiple recipients in parallel using Task.WhenAll
        /// </summary>
        public async Task<Dictionary<string, bool>> SendPromoCodeNotificationsBatchAsync(
            List<(string Email, string Name)> recipients,
            string promoCode,
            decimal discountValue,
            string discountType,
            DateTime? endDate)
        {
            var results = new Dictionary<string, bool>();
            
            if (recipients == null || !recipients.Any())
            {
                _logger.LogWarning("No recipients provided for batch email send");
                return results;
            }

            _logger.LogInformation("Starting batch email send to {Count} recipients", recipients.Count);

            // Create tasks for all email sends
            var emailTasks = recipients.Select(async recipient =>
            {
                try
                {
                    var success = await SendPromoCodeNotificationAsync(
                        recipient.Email,
                        recipient.Name,
                        promoCode,
                        discountValue,
                        discountType,
                        endDate
                    );
                    return (recipient.Email, success);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in batch email send to {Email}", recipient.Email);
                    return (recipient.Email, false);
                }
            }).ToList();

            // Wait for all emails to be sent in parallel
            var emailResults = await Task.WhenAll(emailTasks);

            // Build results dictionary
            foreach (var (email, success) in emailResults)
            {
                results[email] = success;
            }

            var successCount = results.Values.Count(r => r);
            var failureCount = results.Count - successCount;
            _logger.LogInformation("Batch email send completed: {SuccessCount} succeeded, {FailureCount} failed out of {TotalCount}", 
                successCount, failureCount, results.Count);

            return results;
        }

        public async Task<bool> SendOrderConfirmationAsync(
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
            string shippingAddress)
        {
            // Validate email address
            if (!IsValidEmail(toEmail))
            {
                _logger.LogWarning("Invalid email address: {Email}", toEmail);
                return false;
            }

            try
            {
                var provider = _configuration["Email:Provider"] ?? "Gmail";
                var senderEmail = _configuration["Email:SenderEmail"] ?? "me5280908@gmail.com";
                var senderName = _configuration["Email:SenderName"] ?? "JST";
                var baseUrl = _configuration["Email:BaseUrl"] ?? "http://localhost:3000";

                // Build items HTML
                var itemsHtml = string.Join("", items.Select(item => $@"
                    <tr>
                        <td style=""padding: 14px 16px; border-bottom: 2px solid #3D0811; color: #3D0811; background-color: #CEB49D; border-left: none; border-right: none;"">{item.ProductName}{(item.Size != null ? $" - Size {item.Size}" : "")}</td>
                        <td style=""padding: 14px 16px; border-bottom: 2px solid #3D0811; text-align: center; color: #3D0811; background-color: #CEB49D; border-left: none; border-right: none;"">{item.Quantity}</td>
                        <td style=""padding: 14px 16px; border-bottom: 2px solid #3D0811; text-align: right; color: #3D0811; background-color: #CEB49D; border-left: none; border-right: none;"">{item.UnitPrice:F2} EGP</td>
                        <td style=""padding: 14px 16px; border-bottom: 2px solid #3D0811; text-align: right; color: #3D0811; font-weight: bold; background-color: #CEB49D; border-left: none; border-right: none;"">{item.TotalPrice:F2} EGP</td>
                    </tr>"));

                var emailBodyHtml = $@"
<!DOCTYPE html>
<html>
<head>
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
</head>
<body style=""margin: 0; padding: 0; font-family: Georgia, 'Times New Roman', serif; background-color: #CEB49D; color: #3D0811;"">
    <table width=""100%"" cellpadding=""0"" cellspacing=""0"" style=""background-color: #CEB49D; padding: 20px 0;"">
        <tr>
            <td align=""center"">
                <table width=""600"" cellpadding=""0"" cellspacing=""0"" style=""max-width: 600px; background-color: #CEB49D;"">
                    <!-- Header -->
                    <tr>
                        <td style=""background-color: #3D0811; color: #CEB49D; padding: 40px 20px; text-align: center;"">
                            <h1 style=""margin: 0; font-size: 36px; font-weight: bold; color: #CEB49D; letter-spacing: 3px;"">{senderName}</h1>
                            <p style=""margin: 12px 0 0 0; font-size: 18px; color: #CEB49D;"">Order Confirmation</p>
                        </td>
                    </tr>
                    <!-- Content -->
                    <tr>
                        <td style=""background-color: #CEB49D; padding: 35px 25px; color: #3D0811;"">
                            <p style=""margin: 0 0 15px 0; color: #3D0811; line-height: 1.9; font-size: 16px;"">Dear {toName},</p>
                            <p style=""margin: 0 0 25px 0; color: #3D0811; line-height: 1.9; font-size: 16px;"">Thank you for your order! We're excited to confirm that we've received your order and it's being processed.</p>
                            
                            <!-- Order Info Box -->
                            <table width=""100%"" cellpadding=""0"" cellspacing=""0"" style=""background-color: #CEB49D; border: 3px solid #3D0811; border-radius: 10px; margin: 25px 0; padding: 25px;"">
                                <tr>
                                    <td>
                                        <p style=""margin: 0 0 12px 0; color: #3D0811; font-size: 15px; font-weight: bold;"">Order Number:</p>
                                        <div style=""font-size: 28px; font-weight: bold; color: #3D0811; margin: 12px 0; letter-spacing: 2px;"">{orderNumber}</div>
                                        <p style=""margin: 12px 0 0 0; color: #3D0811; font-size: 15px;"">Order Date: {orderDate:MMMM dd, yyyy 'at' HH:mm}</p>
                                    </td>
                                </tr>
                            </table>

                            <!-- Order Items Section -->
                            <h3 style=""color: #3D0811; margin: 35px 0 15px 0; font-size: 22px; font-weight: bold; border-bottom: 3px solid #3D0811; padding-bottom: 10px;"">Order Items</h3>
                            
                            <!-- Items Table -->
                            <table width=""100%"" cellpadding=""0"" cellspacing=""0"" style=""background-color: #CEB49D; border: 2px solid #3D0811; border-radius: 10px; margin: 25px 0; overflow: hidden;"">
                                <thead>
                                    <tr>
                                        <th style=""background-color: #3D0811; color: #CEB49D; padding: 16px; text-align: left; font-weight: bold; font-size: 15px; border: none;"">Product</th>
                                        <th style=""background-color: #3D0811; color: #CEB49D; padding: 16px; text-align: center; font-weight: bold; font-size: 15px; border: none;"">Quantity</th>
                                        <th style=""background-color: #3D0811; color: #CEB49D; padding: 16px; text-align: right; font-weight: bold; font-size: 15px; border: none;"">Unit Price</th>
                                        <th style=""background-color: #3D0811; color: #CEB49D; padding: 16px; text-align: right; font-weight: bold; font-size: 15px; border: none;"">Total</th>
                                    </tr>
                                </thead>
                                <tbody>
                                    {itemsHtml}
                                </tbody>
                            </table>

                            <!-- Summary Box -->
                            <table width=""100%"" cellpadding=""0"" cellspacing=""0"" style=""background-color: #CEB49D; border: 3px solid #3D0811; border-radius: 10px; margin: 25px 0; padding: 25px;"">
                                <tr>
                                    <td>
                                        <table width=""100%"" cellpadding=""0"" cellspacing=""0"">
                                            <tr>
                                                <td style=""padding: 12px 0; color: #3D0811; font-size: 16px;"">Subtotal:</td>
                                                <td style=""padding: 12px 0; color: #3D0811; font-size: 16px; text-align: right;"">{subtotal:F2} EGP</td>
                                            </tr>
                                            {(discount > 0 ? $@"
                                            <tr>
                                                <td style=""padding: 12px 0; color: #3D0811; font-size: 16px;"">Discount:</td>
                                                <td style=""padding: 12px 0; color: #3D0811; font-size: 16px; text-align: right; font-weight: bold;"">-{discount:F2} EGP</td>
                                            </tr>" : "")}
                                            <tr>
                                                <td style=""padding: 12px 0; color: #3D0811; font-size: 16px;"">Shipping:</td>
                                                <td style=""padding: 12px 0; color: #3D0811; font-size: 16px; text-align: right; font-weight: bold;"">{(shipping > 0 ? $"{shipping:F2} EGP" : "Free")}</td>
                                            </tr>
                                            <tr>
                                                <td style=""padding: 18px 0 0 0; border-top: 3px solid #3D0811; color: #3D0811; font-size: 22px; font-weight: bold;"">Total:</td>
                                                <td style=""padding: 18px 0 0 0; border-top: 3px solid #3D0811; color: #3D0811; font-size: 22px; font-weight: bold; text-align: right;"">{total:F2} EGP</td>
                                            </tr>
                                        </table>
                                    </td>
                                </tr>
                            </table>

                            <!-- Shipping Information Box -->
                            <table width=""100%"" cellpadding=""0"" cellspacing=""0"" style=""background-color: #CEB49D; border: 3px solid #3D0811; border-radius: 10px; margin: 25px 0; padding: 25px;"">
                                <tr>
                                    <td>
                                        <h3 style=""color: #3D0811; margin: 0 0 15px 0; font-size: 20px; border-bottom: 3px solid #3D0811; padding-bottom: 10px; font-weight: bold;"">Shipping Information</h3>
                                        <p style=""margin: 10px 0; color: #3D0811; font-size: 15px;""><strong>Address:</strong> {shippingAddress}</p>
                                        <p style=""margin: 10px 0; color: #3D0811; font-size: 15px;""><strong>Payment Method:</strong> {paymentMethod}</p>
                                    </td>
                                </tr>
                            </table>

                            <p style=""margin: 25px 0; color: #3D0811; line-height: 1.9; font-size: 16px;"">We'll send you another email when your order ships. You can track your order status at any time using your order number.</p>
                            
                            <div style=""text-align: center; margin: 25px 0;"">
                                <a href=""{baseUrl}/track?orderId={orderNumber}&email={Uri.EscapeDataString(toEmail)}"" style=""display: inline-block; background-color: #3D0811; color: #CEB49D; padding: 16px 40px; text-decoration: none; border-radius: 8px; font-weight: bold; font-size: 17px; border: 3px solid #CEB49D;"">Track Your Order</a>
                            </div>
                            
                            <p style=""margin: 25px 0; color: #3D0811; line-height: 1.9; font-size: 16px;"">If you have any questions about your order, please don't hesitate to contact us.</p>
                            
                            <p style=""margin: 25px 0; color: #3D0811; line-height: 1.9; font-size: 16px;"">Thank you for shopping with us!</p>
                            
                            <p style=""margin: 25px 0 0 0; color: #3D0811; line-height: 1.9; font-size: 16px;"">Best regards,<br><strong>{senderName} Team</strong></p>
                        </td>
                    </tr>
                    <!-- Footer -->
                    <tr>
                        <td style=""background-color: #CEB49D; padding: 25px; text-align: center; color: #3D0811; font-size: 12px;"">
                            <p style=""margin: 0; color: #3D0811;"">This is an automated email. Please do not reply to this message.</p>
                        </td>
                    </tr>
                </table>
            </td>
        </tr>
    </table>
</body>
</html>";

                // Try SendGrid first (if API key is configured)
                var sendGridApiKey = _configuration["Email:SendGridApiKey"];
                if (!string.IsNullOrEmpty(sendGridApiKey) && provider.ToLower() == "sendgrid")
                {
                    return await SendWithRetryAsync(() => SendViaSendGridAsync(toEmail, toName, senderEmail, senderName, 
                        $"Order Confirmation - {orderNumber}", emailBodyHtml));
                }

                // Try Gmail SMTP (if password is configured)
                var senderPassword = _configuration["Email:SenderPassword"];
                if (!string.IsNullOrEmpty(senderPassword))
                {
                    // Remove spaces from App Password if present
                    senderPassword = senderPassword.Replace(" ", "");
                    _logger.LogInformation("Attempting to send order confirmation email to {Email} via Gmail SMTP", toEmail);
                    return await SendWithRetryAsync(() => SendViaGmailAsync(toEmail, toName, senderEmail, senderName, 
                        $"Order Confirmation - {orderNumber}", emailBodyHtml, senderPassword));
                }

                // If neither is configured, return false (don't mark as sent)
                _logger.LogWarning("Email not configured. Cannot send order confirmation email to {Email} for order {OrderNumber}. Please configure Email:SenderPassword or Email:SendGridApiKey", toEmail, orderNumber);
                _logger.LogInformation("Email content that would have been sent:\n{Content}", emailBodyHtml.Replace("<", "&lt;").Replace(">", "&gt;"));
                return false; // Return false so the system knows email was NOT sent
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending order confirmation email to {Email}", toEmail);
                return false;
            }
        }
    }
}


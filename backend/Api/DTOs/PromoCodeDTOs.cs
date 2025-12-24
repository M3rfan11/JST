namespace Api.DTOs
{
    public class CreatePromoCodeRequest
    {
        public string Code { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string DiscountType { get; set; } = "Percentage"; // "Percentage" or "Fixed"
        public decimal DiscountValue { get; set; } // Percentage (0-100) or fixed amount
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public int? UsageLimit { get; set; } // Total usage limit (null = unlimited)
        public int? UsageLimitPerUser { get; set; } // Usage limit per user (null = unlimited)
        public decimal? MinimumOrderAmount { get; set; }
        public decimal? MaximumDiscountAmount { get; set; }
        public List<int>? UserIds { get; set; } // Users who can use this promo code
        public List<string>? EmailAddresses { get; set; } // Email addresses to send promo code to (can be non-registered)
        public List<int>? ProductIds { get; set; } // Products this promo code applies to (empty = all products)
        public bool? SendEmailNotification { get; set; } = true; // Whether to send email to users
    }

    public class UpdatePromoCodeRequest
    {
        public string? Code { get; set; }
        public string? Description { get; set; }
        public string? DiscountType { get; set; }
        public decimal? DiscountValue { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public int? UsageLimit { get; set; }
        public int? UsageLimitPerUser { get; set; }
        public decimal? MinimumOrderAmount { get; set; }
        public decimal? MaximumDiscountAmount { get; set; }
        public bool? IsActive { get; set; }
        public List<int>? UserIds { get; set; } // Update user list
        public List<string>? EmailAddresses { get; set; } // Email addresses to send promo code to (can be non-registered)
        public List<int>? ProductIds { get; set; } // Update product list
        public bool? SendEmailNotification { get; set; } // Whether to send email to non-registered email addresses
    }

    public class PromoCodeResponse
    {
        public int Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string DiscountType { get; set; } = string.Empty;
        public decimal DiscountValue { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public int? UsageLimit { get; set; }
        public int UsedCount { get; set; }
        public int? UsageLimitPerUser { get; set; }
        public decimal? MinimumOrderAmount { get; set; }
        public decimal? MaximumDiscountAmount { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public int? CreatedByUserId { get; set; }
        public string? CreatedByUserName { get; set; }
        public List<int> UserIds { get; set; } = new List<int>();
        public List<string> UserNames { get; set; } = new List<string>();
        public List<int> ProductIds { get; set; } = new List<int>();
        public List<string> ProductNames { get; set; } = new List<string>();
        public bool IsExpired { get; set; }
        public bool IsValid { get; set; }
        public List<UserUsageInfo> UserUsages { get; set; } = new List<UserUsageInfo>(); // Per-user usage tracking
    }

    public class PromoCodeListResponse
    {
        public int Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string DiscountType { get; set; } = string.Empty;
        public decimal DiscountValue { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public int? UsageLimit { get; set; }
        public int UsedCount { get; set; }
        public int UserCount { get; set; } // Number of users assigned
        public int ProductCount { get; set; } // Number of products assigned (0 = all products)
        public bool IsActive { get; set; }
        public bool IsExpired { get; set; }
        public bool IsValid { get; set; }
    }

    public class PromoCodeUsageDetailResponse
    {
        public int PromoCodeId { get; set; }
        public string Code { get; set; } = string.Empty;
        public List<UserUsageInfo> UserUsages { get; set; } = new List<UserUsageInfo>();
    }

    public class UserUsageInfo
    {
        public int UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string? UserEmail { get; set; }
        public int UsageCount { get; set; }
        public int? UsageLimit { get; set; }
        public bool HasExceededLimit { get; set; }
        public List<UsageRecord> UsageRecords { get; set; } = new List<UsageRecord>();
    }

    public class UsageRecord
    {
        public int OrderId { get; set; }
        public string OrderNumber { get; set; } = string.Empty;
        public decimal DiscountAmount { get; set; }
        public DateTime UsedAt { get; set; }
    }
}


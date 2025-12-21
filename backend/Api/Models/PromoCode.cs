using System.ComponentModel.DataAnnotations;

namespace Api.Models
{
    public class PromoCode
    {
        public int Id { get; set; }
        
        [Required]
        [MaxLength(50)]
        public string Code { get; set; } = string.Empty;
        
        [MaxLength(200)]
        public string? Description { get; set; }
        
        [Required]
        [MaxLength(20)]
        public string DiscountType { get; set; } = "Percentage"; // "Percentage" or "Fixed"
        
        [Required]
        public decimal DiscountValue { get; set; } // Percentage (0-100) or fixed amount
        
        [Required]
        public DateTime StartDate { get; set; }
        
        public DateTime? EndDate { get; set; }
        
        public int? UsageLimit { get; set; } // Total usage limit (null = unlimited)
        
        public int UsedCount { get; set; } = 0;
        
        public int? UsageLimitPerUser { get; set; } // Usage limit per user (null = unlimited)
        
        public decimal? MinimumOrderAmount { get; set; }
        
        public decimal? MaximumDiscountAmount { get; set; }
        
        public bool IsActive { get; set; } = true;
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        public DateTime? UpdatedAt { get; set; }
        
        public int? CreatedByUserId { get; set; }
        
        // Navigation properties
        public User? CreatedByUser { get; set; }
        
        public ICollection<PromoCodeUser> PromoCodeUsers { get; set; } = new List<PromoCodeUser>();
        
        public ICollection<PromoCodeProduct> PromoCodeProducts { get; set; } = new List<PromoCodeProduct>();
        
        public ICollection<PromoCodeUsage> PromoCodeUsages { get; set; } = new List<PromoCodeUsage>();
    }
    
    public class PromoCodeUser
    {
        public int Id { get; set; }
        
        public int PromoCodeId { get; set; }
        
        public int UserId { get; set; }
        
        public DateTime AssignedAt { get; set; } = DateTime.UtcNow;
        
        public bool IsNotified { get; set; } = false; // Whether email notification was sent
        
        public DateTime? NotifiedAt { get; set; }
        
        // Navigation properties
        public PromoCode PromoCode { get; set; } = null!;
        
        public User User { get; set; } = null!;
    }
    
    public class PromoCodeProduct
    {
        public int Id { get; set; }
        
        public int PromoCodeId { get; set; }
        
        public int ProductId { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        // Navigation properties
        public PromoCode PromoCode { get; set; } = null!;
        
        public Product Product { get; set; } = null!;
    }
    
    public class PromoCodeUsage
    {
        public int Id { get; set; }
        
        public int PromoCodeId { get; set; }
        
        public int SalesOrderId { get; set; }
        
        public int? UserId { get; set; }
        
        public decimal DiscountAmount { get; set; }
        
        public DateTime UsedAt { get; set; } = DateTime.UtcNow;
        
        // Navigation properties
        public PromoCode PromoCode { get; set; } = null!;
        
        public SalesOrder SalesOrder { get; set; } = null!;
        
        public User? User { get; set; }
    }
}





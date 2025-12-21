using System.ComponentModel.DataAnnotations;

namespace Api.Models
{
    public class ProductVariant
    {
        public int Id { get; set; }
        
        public int ProductId { get; set; }
        
        [Required]
        [MaxLength(50)]
        public string Color { get; set; } = string.Empty; // Legacy field - kept for backward compatibility (e.g., "Charcoal", "Cream", "Navy")
        
        [MaxLength(7)]
        public string? ColorHex { get; set; } // Legacy field - Hex color code (e.g., "#36454F" for Charcoal)
        
        [MaxLength(5000)]
        public string? Attributes { get; set; } // JSON object of variant attributes (e.g., {"Color": "Red", "Size": "Large", "Material": "Cotton"})
        
        [MaxLength(500)]
        public string? ImageUrl { get; set; } // Variant-specific image (for backward compatibility)
        
        [MaxLength(50000)]
        public string? MediaUrls { get; set; } // JSON array of variant image URLs (supports base64 data URLs)
        
        public decimal? PriceOverride { get; set; } // Optional price override for this variant
        
        [MaxLength(50)]
        public string? SKU { get; set; } // Variant-specific SKU
        
        public bool IsActive { get; set; } = true;
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        public DateTime? UpdatedAt { get; set; }
        
        // Navigation properties
        public virtual Product Product { get; set; } = null!;
    }
}


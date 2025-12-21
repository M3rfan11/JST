using System.ComponentModel.DataAnnotations;

namespace Api.Models
{
    public enum ProductStatus
    {
        Draft = 0,
        Active = 1
    }

    public class Product
    {
        public int Id { get; set; }
        
        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;
        
        [MaxLength(2000)]
        public string? Description { get; set; } // Rich text description
        
        // Pricing
        [Required]
        public decimal Price { get; set; }
        
        public decimal? CompareAtPrice { get; set; } // Compare-at price (for showing discounts)
        
        public decimal? Cost { get; set; } // Cost price (for profit calculations)
        
        public bool Taxable { get; set; } = true; // Charge tax on this product
        
        [MaxLength(50)]
        public string? Unit { get; set; } // e.g., "piece", "box", "kg", "liter"
        
        // Inventory
        [MaxLength(50)]
        public string? SKU { get; set; } // Stock Keeping Unit
        
        [MaxLength(100)]
        public string? Barcode { get; set; } // Barcode (EAN, UPC, etc.)
        
        public bool InventoryTracked { get; set; } = true; // Inventory tracked toggle
        
        public bool SellWhenOutOfStock { get; set; } = false; // Sell when out of stock
        
        public bool AlwaysAvailable { get; set; } = false; // Always available regardless of stock level (even if negative)
        
        // Shipping
        public bool IsPhysicalProduct { get; set; } = true; // Physical product toggle
        
        public decimal? Weight { get; set; } // in kg
        
        [MaxLength(50)]
        public string? Dimensions { get; set; } // e.g., "10x20x30 cm"
        
        // Package dimensions
        public decimal? PackageLength { get; set; } // in cm
        public decimal? PackageWidth { get; set; } // in cm
        public decimal? PackageHeight { get; set; } // in cm
        
        [MaxLength(100)]
        public string? CountryOfOrigin { get; set; } // Country of origin
        
        // Product Organization
        [MaxLength(100)]
        public string? Brand { get; set; }
        
        [MaxLength(100)]
        public string? Vendor { get; set; } // Vendor/manufacturer
        
        [MaxLength(100)]
        public string? ProductType { get; set; } // Product type/category
        
        [MaxLength(500)]
        public string? Tags { get; set; } // Comma-separated tags
        
        public ProductStatus Status { get; set; } = ProductStatus.Active; // Draft or Active
        
        public bool IsActive { get; set; } = true; // Legacy field for backward compatibility
        
        // Publishing Channels (stored as comma-separated string: "Online Store,Point of Sale")
        [MaxLength(200)]
        public string? PublishingChannels { get; set; } = "Online Store";
        
        // Media
        [MaxLength(500)]
        public string? ImageUrl { get; set; } // Primary image URL (for backward compatibility)
        
        [MaxLength(50000)]
        public string? MediaUrls { get; set; } // JSON array of image/video URLs (supports base64 data URLs)
        
        [MaxLength(2000)]
        public string? VariantAttributes { get; set; } // JSON array of variant attribute definitions (e.g., ["Color", "Size", "Material"])
        
        [MaxLength(1000)]
        public string? WashingInstructions { get; set; } // Optional washing/care instructions
        
        public int CategoryId { get; set; } // Legacy field - kept for backward compatibility (primary category)
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        public DateTime? UpdatedAt { get; set; }
        
        // Navigation properties
        public virtual Category Category { get; set; } = null!; // Legacy - primary category
        public virtual ICollection<ProductCategory> ProductCategories { get; set; } = new List<ProductCategory>(); // Many-to-many relationship
        public virtual ICollection<ProductInventory> ProductInventories { get; set; } = new List<ProductInventory>();
        public virtual ICollection<ProductVariant> Variants { get; set; } = new List<ProductVariant>();
    }
}

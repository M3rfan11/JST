using Api.Models;

namespace Api.DTOs
{
    public class CreateProductRequest
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        
        // Pricing
        public decimal Price { get; set; }
        public decimal? CompareAtPrice { get; set; }
        public decimal? Cost { get; set; }
        public bool Taxable { get; set; } = true;
        
        public string? Unit { get; set; }
        
        // Inventory
        public string? SKU { get; set; }
        public string? Barcode { get; set; }
        public bool InventoryTracked { get; set; } = true;
        public bool SellWhenOutOfStock { get; set; } = false;
        public bool AlwaysAvailable { get; set; } = false;
        
        // Shipping
        public bool IsPhysicalProduct { get; set; } = true;
        public decimal? Weight { get; set; }
        public string? Dimensions { get; set; }
        public decimal? PackageLength { get; set; }
        public decimal? PackageWidth { get; set; }
        public decimal? PackageHeight { get; set; }
        public string? CountryOfOrigin { get; set; }
        
        // Product Organization
        public string? Brand { get; set; }
        public string? Vendor { get; set; }
        public string? ProductType { get; set; }
        public string? Tags { get; set; }
        public ProductStatus Status { get; set; } = ProductStatus.Active;
        public string? PublishingChannels { get; set; } = "Online Store";
        
        // Media
        public string? ImageUrl { get; set; }
        public string? MediaUrls { get; set; } // JSON array of URLs
        
        public string? VariantAttributes { get; set; } // JSON array of variant attribute definitions (e.g., ["Color", "Size", "Material"])
        
        public string? WashingInstructions { get; set; } // Optional washing/care instructions
        
        public int CategoryId { get; set; } // Legacy - primary category
        public List<int>? CategoryIds { get; set; } // Multiple categories
        public bool IsActive { get; set; } = true; // Legacy field
        public List<CreateProductVariantRequest>? Variants { get; set; }
    }

    public class UpdateProductRequest
    {
        public string? Name { get; set; }
        public string? Description { get; set; }
        
        // Pricing
        public decimal? Price { get; set; }
        public decimal? CompareAtPrice { get; set; }
        public decimal? Cost { get; set; }
        public bool? Taxable { get; set; }
        
        public string? Unit { get; set; }
        
        // Inventory
        public string? SKU { get; set; }
        public string? Barcode { get; set; }
        public bool? InventoryTracked { get; set; }
        public bool? SellWhenOutOfStock { get; set; }
        public bool? AlwaysAvailable { get; set; }
        
        // Shipping
        public bool? IsPhysicalProduct { get; set; }
        public decimal? Weight { get; set; }
        public string? Dimensions { get; set; }
        public decimal? PackageLength { get; set; }
        public decimal? PackageWidth { get; set; }
        public decimal? PackageHeight { get; set; }
        public string? CountryOfOrigin { get; set; }
        
        // Product Organization
        public string? Brand { get; set; }
        public string? Vendor { get; set; }
        public string? ProductType { get; set; }
        public string? Tags { get; set; }
        public ProductStatus? Status { get; set; }
        public string? PublishingChannels { get; set; }
        
        // Media
        public string? ImageUrl { get; set; }
        public string? MediaUrls { get; set; }
        
        public string? VariantAttributes { get; set; } // JSON array of variant attribute definitions
        
        public string? WashingInstructions { get; set; } // Optional washing/care instructions
        
        public int? CategoryId { get; set; } // Legacy - primary category
        public List<int>? CategoryIds { get; set; } // Multiple categories
        public bool? IsActive { get; set; } // Legacy field
    }

    public class ProductResponse
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        
        // Pricing
        public decimal Price { get; set; }
        public decimal? CompareAtPrice { get; set; }
        public decimal? Cost { get; set; }
        public bool Taxable { get; set; }
        
        public string? Unit { get; set; }
        
        // Inventory
        public string? SKU { get; set; }
        public string? Barcode { get; set; }
        public bool InventoryTracked { get; set; }
        public bool SellWhenOutOfStock { get; set; }
        public bool AlwaysAvailable { get; set; }
        
        // Shipping
        public bool IsPhysicalProduct { get; set; }
        public decimal? Weight { get; set; }
        public string? Dimensions { get; set; }
        public decimal? PackageLength { get; set; }
        public decimal? PackageWidth { get; set; }
        public decimal? PackageHeight { get; set; }
        public string? CountryOfOrigin { get; set; }
        
        // Product Organization
        public string? Brand { get; set; }
        public string? Vendor { get; set; }
        public string? ProductType { get; set; }
        public string? Tags { get; set; }
        public ProductStatus Status { get; set; }
        public string? PublishingChannels { get; set; }
        
        // Media
        public string? ImageUrl { get; set; }
        public string? MediaUrls { get; set; }
        
        public string? VariantAttributes { get; set; } // JSON array of variant attribute definitions
        
        public string? WashingInstructions { get; set; } // Optional washing/care instructions
        
        public int CategoryId { get; set; } // Legacy - primary category
        public string CategoryName { get; set; } = string.Empty; // Legacy - primary category name
        public List<int> CategoryIds { get; set; } = new List<int>(); // All category IDs
        public List<string> CategoryNames { get; set; } = new List<string>(); // All category names
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public decimal TotalQuantity { get; set; }
        public List<ProductInventoryResponse> Inventories { get; set; } = new List<ProductInventoryResponse>();
        public List<ProductVariantResponse> Variants { get; set; } = new List<ProductVariantResponse>();
        
        // Calculated fields
        public decimal? Profit => Cost.HasValue && Price > 0 ? Price - Cost.Value : null;
        public decimal? ProfitMargin => Cost.HasValue && Price > 0 ? ((Price - Cost.Value) / Price) * 100 : null;
    }

    public class ProductListResponse
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public decimal Price { get; set; }
        public decimal? CompareAtPrice { get; set; }
        public string? Unit { get; set; }
        public string? SKU { get; set; }
        public string? Brand { get; set; }
        public string? ImageUrl { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public decimal TotalQuantity { get; set; }
        public List<string> Colors { get; set; } = new List<string>(); // Available colors from variants (legacy)
        public List<ProductVariantResponse> Variants { get; set; } = new List<ProductVariantResponse>(); // Full variant information
        public string? VariantAttributes { get; set; } // JSON array of variant attribute definitions
    }

    public class ProductInventoryResponse
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string ProductSKU { get; set; } = string.Empty;
        public int WarehouseId { get; set; }
        public string WarehouseName { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
        public string? Unit { get; set; }
        public decimal? MinimumStockLevel { get; set; }
        public decimal? MaximumStockLevel { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    public class CreateProductInventoryRequest
    {
        public int ProductId { get; set; }
        public int WarehouseId { get; set; }
        public decimal Quantity { get; set; }
        public string? Unit { get; set; }
        public decimal? MinimumStockLevel { get; set; }
        public decimal? MaximumStockLevel { get; set; }
    }

    public class UpdateInventoryRequest
    {
        public decimal Quantity { get; set; }
        public string? Unit { get; set; }
        public decimal? MinimumStockLevel { get; set; }
        public decimal? MaximumStockLevel { get; set; }
    }

    // Product Variant DTOs
    public class CreateProductVariantRequest
    {
        public string Color { get; set; } = string.Empty; // Legacy - kept for backward compatibility
        public string? ColorHex { get; set; } // Legacy - Hex color code (e.g., "#36454F")
        public string? Attributes { get; set; } // JSON object of variant attributes (e.g., {"Color": "Red", "Size": "Large"})
        public string? ImageUrl { get; set; } // For backward compatibility
        public string? MediaUrls { get; set; } // JSON array of image URLs
        public decimal? PriceOverride { get; set; }
        public string? SKU { get; set; }
        public bool IsActive { get; set; } = true;
    }

    public class UpdateProductVariantRequest
    {
        public string? Color { get; set; } // Legacy
        public string? ColorHex { get; set; } // Legacy - Hex color code
        public string? Attributes { get; set; } // JSON object of variant attributes
        public string? ImageUrl { get; set; } // For backward compatibility
        public string? MediaUrls { get; set; } // JSON array of image URLs
        public decimal? PriceOverride { get; set; }
        public string? SKU { get; set; }
        public bool? IsActive { get; set; }
    }

    public class ProductVariantResponse
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public string Color { get; set; } = string.Empty; // Legacy
        public string? ColorHex { get; set; } // Legacy - Hex color code
        public string? Attributes { get; set; } // JSON object of variant attributes
        public string? ImageUrl { get; set; } // For backward compatibility
        public string? MediaUrls { get; set; } // JSON array of image URLs
        public decimal? PriceOverride { get; set; }
        public string? SKU { get; set; }
        public bool IsActive { get; set; }
        public decimal Quantity { get; set; } // Stock quantity for this variant
        public bool IsOutOfStock { get; set; } // True if quantity is 0
        public bool IsAvailable { get; set; } // True if in stock or product allows selling when out of stock
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    // Variant Inventory DTOs
    public class CreateVariantInventoryRequest
    {
        public int ProductVariantId { get; set; }
        public int WarehouseId { get; set; }
        public decimal Quantity { get; set; }
        public string? Unit { get; set; }
        public decimal? MinimumStockLevel { get; set; }
        public decimal? MaximumStockLevel { get; set; }
    }

    public class UpdateVariantInventoryRequest
    {
        public decimal Quantity { get; set; }
        public string? Unit { get; set; }
        public decimal? MinimumStockLevel { get; set; }
        public decimal? MaximumStockLevel { get; set; }
    }

    public class VariantInventoryResponse
    {
        public int Id { get; set; }
        public int ProductVariantId { get; set; }
        public int WarehouseId { get; set; }
        public string WarehouseName { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
        public decimal POSQuantity { get; set; }
        public string? Unit { get; set; }
        public decimal? MinimumStockLevel { get; set; }
        public decimal? MaximumStockLevel { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}

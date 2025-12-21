using System.ComponentModel.DataAnnotations;

namespace Api.Models
{
    public class VariantInventory
    {
        public int Id { get; set; }
        
        public int ProductVariantId { get; set; }
        
        public int WarehouseId { get; set; }
        
        [Required]
        public decimal Quantity { get; set; }
        
        [Required]
        public decimal POSQuantity { get; set; } = 0m; // Quantity available in POS system
        
        [MaxLength(50)]
        public string? Unit { get; set; } // e.g., "piece", "box", "kg", "liter"
        
        public decimal? MinimumStockLevel { get; set; }
        
        public decimal? MaximumStockLevel { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        public DateTime? UpdatedAt { get; set; }
        
        // Navigation properties
        public virtual ProductVariant ProductVariant { get; set; } = null!;
        public virtual Warehouse Warehouse { get; set; } = null!;
    }
}


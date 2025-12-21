using System.ComponentModel.DataAnnotations;

namespace Api.Models
{
    public class BillOfMaterial
    {
        public int Id { get; set; }
        
        public int ProductAssemblyId { get; set; }
        
        public int RawProductId { get; set; } // The raw material/product used
        
        public int WarehouseId { get; set; } // Warehouse where raw material is stored
        
        [Required]
        public decimal RequiredQuantity { get; set; } // Quantity needed for assembly
        
        [Required]
        public decimal AvailableQuantity { get; set; } // Available quantity in stock
        
        [MaxLength(50)]
        public string? Unit { get; set; } // e.g., "piece", "box", "kg", "liter"
        
        [MaxLength(200)]
        public string? Notes { get; set; }
        
        public DateTime CreatedAt { get; set; }
        
        public DateTime? UpdatedAt { get; set; }
        
        // Navigation properties
        public virtual ProductAssembly ProductAssembly { get; set; } = null!;
        public virtual Product RawProduct { get; set; } = null!;
        public virtual Warehouse Warehouse { get; set; } = null!;
    }
}

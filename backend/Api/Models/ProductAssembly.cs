using System.ComponentModel.DataAnnotations;

namespace Api.Models
{
    public class ProductAssembly
    {
        public int Id { get; set; }
        
        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;
        
        [MaxLength(500)]
        public string? Description { get; set; }
        
        [Required]
        public decimal Quantity { get; set; } // Quantity of final product to produce
        
        [MaxLength(50)]
        public string? Unit { get; set; } // e.g., "piece", "box", "kg", "liter"
        
        [MaxLength(500)]
        public string? Instructions { get; set; } // Assembly instructions
        
        [MaxLength(20)]
        public string Status { get; set; } = "Pending"; // Pending, InProgress, Completed, Cancelled
        
        [MaxLength(500)]
        public string? Notes { get; set; }
        
        public int CreatedByUserId { get; set; }
        
        public int? StoreId { get; set; } // Store-specific assembly
        
        public bool IsActive { get; set; } = true; // Whether this assembly is active for POS
        
        public decimal? SalePrice { get; set; } // Price when sold as assembly
        
        public int? CompletedByUserId { get; set; }
        
        public DateTime CreatedAt { get; set; }
        
        public DateTime? UpdatedAt { get; set; }
        
        public DateTime? StartedAt { get; set; }
        
        public DateTime? CompletedAt { get; set; }
        
        // Navigation properties
        public virtual User CreatedByUser { get; set; } = null!;
        public virtual User? CompletedByUser { get; set; }
        public virtual Warehouse? Store { get; set; }
        public virtual ICollection<BillOfMaterial> BillOfMaterials { get; set; } = new List<BillOfMaterial>();
    }
}

using System.ComponentModel.DataAnnotations;

namespace Api.Models
{
    public class PurchaseItem
    {
        public int Id { get; set; }
        
        public int PurchaseOrderId { get; set; }
        
        public int ProductId { get; set; }
        
        public int WarehouseId { get; set; }
        
        [Required]
        public decimal Quantity { get; set; }
        
        [Required]
        public decimal UnitPrice { get; set; }
        
        [Required]
        public decimal TotalPrice { get; set; }
        
        [MaxLength(50)]
        public string? Unit { get; set; } // e.g., "piece", "box", "kg", "liter"
        
        [MaxLength(200)]
        public string? Notes { get; set; }
        
        public DateTime CreatedAt { get; set; }
        
        public DateTime? UpdatedAt { get; set; }
        
        // Navigation properties
        public virtual PurchaseOrder PurchaseOrder { get; set; } = null!;
        public virtual Product Product { get; set; } = null!;
        public virtual Warehouse Warehouse { get; set; } = null!;
    }
}

using System.ComponentModel.DataAnnotations;

namespace Api.Models
{
    public class ProductMovement
    {
        public int Id { get; set; }
        
        [Required]
        public int ProductId { get; set; }
        
        [Required]
        public int WarehouseId { get; set; }
        
        [Required]
        [MaxLength(20)]
        public string MovementType { get; set; } = string.Empty; // Purchase, Sale, Assembly, Transfer, Adjustment
        
        [Required]
        public decimal Quantity { get; set; }
        
        [MaxLength(50)]
        public string? Unit { get; set; }
        
        [MaxLength(20)]
        public string Direction { get; set; } = string.Empty; // In, Out
        
        [MaxLength(500)]
        public string? Description { get; set; }
        
        [MaxLength(100)]
        public string? ReferenceNumber { get; set; } // Order number, assembly ID, etc.
        
        public int? ReferenceId { get; set; } // ID of the related order/assembly
        
        [MaxLength(20)]
        public string? ReferenceType { get; set; } // PurchaseOrder, SalesOrder, ProductAssembly, etc.
        
        public int? CreatedByUserId { get; set; }
        
        public DateTime MovementDate { get; set; }
        
        public DateTime CreatedAt { get; set; }
        
        [MaxLength(500)]
        public string? Notes { get; set; }
        
        // Navigation properties
        public virtual Product Product { get; set; } = null!;
        public virtual Warehouse Warehouse { get; set; } = null!;
        public virtual User? CreatedByUser { get; set; }
    }
}

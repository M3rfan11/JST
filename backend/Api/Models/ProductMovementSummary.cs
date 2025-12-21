using System.ComponentModel.DataAnnotations;

namespace Api.Models
{
    public class ProductMovementSummary
    {
        public int Id { get; set; }
        
        [Required]
        public int ProductId { get; set; }
        
        [Required]
        public int WarehouseId { get; set; }
        
        [Required]
        public DateTime SummaryDate { get; set; } // Daily summary date
        
        [Required]
        public decimal OpeningBalance { get; set; }
        
        [Required]
        public decimal TotalIn { get; set; }
        
        [Required]
        public decimal TotalOut { get; set; }
        
        [Required]
        public decimal ClosingBalance { get; set; }
        
        public int PurchaseCount { get; set; }
        
        public int SaleCount { get; set; }
        
        public int AssemblyCount { get; set; }
        
        public int TransferCount { get; set; }
        
        public int AdjustmentCount { get; set; }
        
        public DateTime CreatedAt { get; set; }
        
        public DateTime? UpdatedAt { get; set; }
        
        // Navigation properties
        public virtual Product Product { get; set; } = null!;
        public virtual Warehouse Warehouse { get; set; } = null!;
    }
}

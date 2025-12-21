using System.ComponentModel.DataAnnotations;

namespace Api.Models
{
    public class PurchaseOrder
    {
        public int Id { get; set; }
        
        [Required]
        [MaxLength(50)]
        public string OrderNumber { get; set; } = string.Empty;
        
        [MaxLength(100)]
        public string? SupplierName { get; set; }
        
        [MaxLength(500)]
        public string? SupplierAddress { get; set; }
        
        [MaxLength(50)]
        public string? SupplierPhone { get; set; }
        
        [MaxLength(100)]
        public string? SupplierEmail { get; set; }
        
        public DateTime OrderDate { get; set; }
        
        public DateTime? ExpectedDeliveryDate { get; set; }
        
        public DateTime? ActualDeliveryDate { get; set; }
        
        [Required]
        public decimal TotalAmount { get; set; }
        
        [MaxLength(20)]
        public string Status { get; set; } = "Pending"; // Pending, Approved, Received, Cancelled
        
        [MaxLength(500)]
        public string? Notes { get; set; }
        
        public int CreatedByUserId { get; set; }
        
        public int? ApprovedByUserId { get; set; }
        
        public DateTime CreatedAt { get; set; }
        
        public DateTime? UpdatedAt { get; set; }
        
        // Navigation properties
        public virtual User CreatedByUser { get; set; } = null!;
        public virtual User? ApprovedByUser { get; set; }
        public virtual ICollection<PurchaseItem> PurchaseItems { get; set; } = new List<PurchaseItem>();
    }
}

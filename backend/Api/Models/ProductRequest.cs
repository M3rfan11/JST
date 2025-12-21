using System.ComponentModel.DataAnnotations;

namespace Api.Models
{
    public class ProductRequest
    {
        public int Id { get; set; }
        
        [Required]
        public int RequestedByUserId { get; set; }
        
        [Required]
        public DateTime RequestDate { get; set; }
        
        [Required]
        [MaxLength(20)]
        public string Status { get; set; } = "Pending"; // Pending, Approved, Rejected
        
        [Required]
        public int WarehouseId { get; set; }
        
        [MaxLength(500)]
        public string? Notes { get; set; }
        
        public int? ApprovedByUserId { get; set; }
        
        public DateTime? ApprovedAt { get; set; }
        
        public DateTime? RejectedAt { get; set; }
        
        [MaxLength(500)]
        public string? RejectionReason { get; set; }
        
        public DateTime CreatedAt { get; set; }
        
        public DateTime? UpdatedAt { get; set; }
        
        // Navigation properties
        public virtual User RequestedByUser { get; set; } = null!;
        public virtual User? ApprovedByUser { get; set; }
        public virtual Warehouse Warehouse { get; set; } = null!;
        public virtual ICollection<ProductRequestItem> ProductRequestItems { get; set; } = new List<ProductRequestItem>();
    }
}

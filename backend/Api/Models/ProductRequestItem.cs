using System.ComponentModel.DataAnnotations;

namespace Api.Models
{
    public class ProductRequestItem
    {
        public int Id { get; set; }
        
        public int ProductRequestId { get; set; }
        
        public int ProductId { get; set; }
        
        [Required]
        public decimal QuantityRequested { get; set; }
        
        [MaxLength(50)]
        public string? Unit { get; set; } // e.g., "pieces", "kg", "liters"
        
        [MaxLength(200)]
        public string? Notes { get; set; }
        
        public decimal? QuantityApproved { get; set; } // May be different from requested
        
        public decimal? QuantityReceived { get; set; } // Actual quantity received
        
        public DateTime CreatedAt { get; set; }
        
        public DateTime? UpdatedAt { get; set; }
        
        // Navigation properties
        public virtual ProductRequest ProductRequest { get; set; } = null!;
        public virtual Product Product { get; set; } = null!;
    }
}

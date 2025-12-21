using System.ComponentModel.DataAnnotations;

namespace Api.Models
{
    public class OrderTracking
    {
        public int Id { get; set; }
        
        [Required]
        public int OrderId { get; set; }
        
        [Required]
        [MaxLength(50)]
        public string Status { get; set; } = string.Empty; // Pending, Confirmed, Shipped, Delivered, Cancelled
        
        [MaxLength(500)]
        public string? Notes { get; set; }
        
        [MaxLength(100)]
        public string? Location { get; set; } // Current location for shipping
        
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        
        public int? UpdatedByUserId { get; set; }
        
        // Navigation properties
        public virtual SalesOrder Order { get; set; } = null!;
        public virtual User? UpdatedByUser { get; set; }
    }
}

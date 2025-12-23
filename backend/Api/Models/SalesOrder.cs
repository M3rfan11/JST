using System.ComponentModel.DataAnnotations;

namespace Api.Models
{
    public class SalesOrder
    {
        public int Id { get; set; }
        
        [Required]
        [MaxLength(50)]
        public string OrderNumber { get; set; } = string.Empty;
        
        [MaxLength(100)]
        public string? CustomerName { get; set; }
        
        [MaxLength(500)]
        public string? CustomerAddress { get; set; }
        
        [MaxLength(50)]
        public string? CustomerPhone { get; set; }
        
        [MaxLength(100)]
        public string? CustomerEmail { get; set; }
        
        public DateTime OrderDate { get; set; }
        
        public DateTime? DeliveryDate { get; set; }
        
        [Required]
        public decimal TotalAmount { get; set; }
        
        public decimal? DownPayment { get; set; } // Down payment amount for furniture orders
        
        [MaxLength(20)]
        public string Status { get; set; } = "Pending"; // Pending, PENDING_PAYMENT, PROOF_SUBMITTED, UNDER_REVIEW, ACCEPTED, REJECTED, Confirmed, Shipped, Delivered, Cancelled
        
        [MaxLength(20)]
        public string PaymentStatus { get; set; } = "Pending"; // Pending, Paid, PartiallyPaid, Refunded
        
        [MaxLength(50)]
        public string? PaymentMethod { get; set; } // Cash on Delivery, InstaPay, Credit Card, Bank Transfer
        
        [MaxLength(500)]
        public string? Notes { get; set; }
        
        [MaxLength(1000)]
        public string? AdminNote { get; set; } // Admin notes for payment review
        
        [MaxLength(500)]
        public string? RejectionReason { get; set; } // Reason for payment rejection
        
        public DateTime? ConfirmedAt { get; set; } // When payment was confirmed by admin
        
        public int CreatedByUserId { get; set; }
        
        public int? ConfirmedByUserId { get; set; }
        
        public DateTime CreatedAt { get; set; }
        
        public DateTime? UpdatedAt { get; set; }
        
        // Navigation properties
        public virtual User CreatedByUser { get; set; } = null!;
        public virtual User? ConfirmedByUser { get; set; }
        public virtual ICollection<SalesItem> SalesItems { get; set; } = new List<SalesItem>();
        public virtual ICollection<PaymentProof> PaymentProofs { get; set; } = new List<PaymentProof>();
    }
}

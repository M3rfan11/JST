using System.ComponentModel.DataAnnotations;

namespace Api.Models
{
    public class PaymentProof
    {
        public int Id { get; set; }
        
        [Required]
        public int OrderId { get; set; }
        
        [Required]
        [MaxLength(500)]
        public string FileUrl { get; set; } = string.Empty;
        
        [MaxLength(50)]
        public string FileType { get; set; } = string.Empty; // image/jpeg, image/png, application/pdf
        
        [MaxLength(100)]
        public string? FileName { get; set; }
        
        public long? FileSize { get; set; } // Size in bytes
        
        public DateTime UploadedAt { get; set; }
        
        // Navigation property
        public virtual SalesOrder Order { get; set; } = null!;
    }
}


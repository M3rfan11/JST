using System.ComponentModel.DataAnnotations;

namespace Api.Models
{
    public class Settings
    {
        public int Id { get; set; }
        
        [Required]
        [MaxLength(100)]
        public string Key { get; set; } = string.Empty; // e.g., "ShippingTickerMessages"
        
        public string? Value { get; set; } // JSON string for complex data, or simple string
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        public DateTime? UpdatedAt { get; set; }
    }
}


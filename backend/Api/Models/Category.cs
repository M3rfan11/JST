using System.ComponentModel.DataAnnotations;

namespace Api.Models
{
    public class Category
    {
        public int Id { get; set; }
        
        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;
        
        [MaxLength(500)]
        public string? Description { get; set; }
        
        // No MaxLength - SQLite TEXT can handle large strings, base64 images can be very large
        public string? ImageUrls { get; set; } // JSON array of image URLs for the collection
        
        public bool IsActive { get; set; } = true;
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        public DateTime? UpdatedAt { get; set; }
        
        // Navigation properties
        public virtual ICollection<Product> Products { get; set; } = new List<Product>(); // Legacy - products with this as primary category
        public virtual ICollection<ProductCategory> ProductCategories { get; set; } = new List<ProductCategory>(); // Many-to-many relationship
    }
}

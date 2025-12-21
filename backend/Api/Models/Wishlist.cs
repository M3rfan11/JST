using System.ComponentModel.DataAnnotations;

namespace Api.Models;

public class Wishlist
{
    public int Id { get; set; }

    [Required]
    public int UserId { get; set; }
    public User User { get; set; } = null!;

    [Required]
    public int ProductId { get; set; }
    public Product Product { get; set; } = null!;

    public int? ProductVariantId { get; set; } // Optional: for variant-specific wishlist items
    public ProductVariant? ProductVariant { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    // Ensure unique combination of User, Product, and Variant
    // This prevents duplicate wishlist items
}





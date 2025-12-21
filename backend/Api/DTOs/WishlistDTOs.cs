namespace Api.DTOs;

public class CreateWishlistItemRequest
{
    public int ProductId { get; set; }
    public int? ProductVariantId { get; set; }
}

public class WishlistItemResponse
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string? ProductSKU { get; set; }
    public decimal ProductPrice { get; set; }
    public string? ProductImageUrl { get; set; }
    public int? ProductVariantId { get; set; }
    public string? VariantAttributes { get; set; }
    public DateTime CreatedAt { get; set; }
}





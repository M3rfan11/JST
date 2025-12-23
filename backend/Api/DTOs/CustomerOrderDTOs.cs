using System.ComponentModel.DataAnnotations;

namespace Api.DTOs
{
    // Shopping Cart DTOs
    public class AddToCartRequest
    {
        [Required]
        public int ProductId { get; set; }

        public int? VariantId { get; set; } // Optional: for variant-specific cart items

        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "Quantity must be greater than 0")]
        public decimal Quantity { get; set; }
    }

    public class UpdateCartItemRequest
    {
        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "Quantity must be greater than 0")]
        public decimal Quantity { get; set; }
    }

    public class CartItemResponse
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public int? VariantId { get; set; } // Variant ID if item has a variant
        public string ProductName { get; set; } = string.Empty;
        public string? ProductSKU { get; set; }
        public string? ProductDescription { get; set; }
        public decimal Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal TotalPrice { get; set; }
        public string? Unit { get; set; }
        public string? CategoryName { get; set; }
        public decimal AvailableQuantity { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public class CartSummaryResponse
    {
        public List<CartItemResponse> Items { get; set; } = new List<CartItemResponse>();
        public decimal SubTotal { get; set; }
        public decimal Tax { get; set; }
        public decimal Total { get; set; }
        public int ItemCount { get; set; }
    }

    // Customer Order DTOs
    public class CreateCustomerOrderRequest
    {
        [Required]
        [MaxLength(100)]
        public string CustomerName { get; set; } = string.Empty;

        [EmailAddress]
        [MaxLength(255)]
        public string? CustomerEmail { get; set; }

        [MaxLength(20)]
        public string? CustomerPhone { get; set; }

        [Required]
        [MaxLength(500)]
        public string CustomerAddress { get; set; } = string.Empty;

        public DateTime? DeliveryDate { get; set; }

        [MaxLength(500)]
        public string? Notes { get; set; }

        [MaxLength(50)]
        public string? PaymentMethod { get; set; } // Cash on Delivery, InstaPay, Credit Card, Bank Transfer

        public string? PromoCode { get; set; } // Optional promo code

        public bool UseCartItems { get; set; } = true; // If true, use cart items; if false, use provided items
        public List<CreateOnlineOrderItemRequest>? Items { get; set; } // Only used if UseCartItems is false
    }

    public class CustomerOrderResponse
    {
        public int Id { get; set; }
        public string OrderNumber { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public string? CustomerEmail { get; set; }
        public string? CustomerPhone { get; set; }
        public string CustomerAddress { get; set; } = string.Empty;
        public DateTime OrderDate { get; set; }
        public DateTime? DeliveryDate { get; set; }
        public decimal TotalAmount { get; set; }
        public string Status { get; set; } = string.Empty;
        public string PaymentStatus { get; set; } = string.Empty;
        public string? Notes { get; set; }
        public List<OnlineOrderItemResponse> Items { get; set; } = new List<OnlineOrderItemResponse>();
        public List<OrderTrackingResponse> TrackingHistory { get; set; } = new List<OrderTrackingResponse>();
    }

    public class OrderTrackingResponse
    {
        public int Id { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? Notes { get; set; }
        public string? Location { get; set; }
        public DateTime Timestamp { get; set; }
        public string? UpdatedByUserName { get; set; }
    }

    // Customer Product Catalog DTOs
    public class CustomerProductResponse
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public decimal Price { get; set; }
        public string? SKU { get; set; }
        public string? Brand { get; set; }
        public string? Unit { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public decimal AvailableQuantity { get; set; }
        public bool IsAvailable { get; set; }
        public string? ImageUrl { get; set; }
    }

    public class CustomerCategoryResponse
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int ProductCount { get; set; }
    }

    // Guest Order DTOs
    public class CreateGuestOrderRequest
    {
        [Required]
        [MaxLength(100)]
        public string CustomerName { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [MaxLength(255)]
        public string CustomerEmail { get; set; } = string.Empty;

        [Required]
        [MaxLength(20)]
        public string CustomerPhone { get; set; } = string.Empty;

        [Required]
        [MaxLength(500)]
        public string CustomerAddress { get; set; } = string.Empty;

        public DateTime? DeliveryDate { get; set; }

        [MaxLength(500)]
        public string? Notes { get; set; }

        [Required]
        public List<CreateGuestOrderItemRequest> Items { get; set; } = new List<CreateGuestOrderItemRequest>();

        public decimal? ShippingCost { get; set; }

        public string? PaymentMethod { get; set; }

        [MaxLength(50)]
        public string? PromoCode { get; set; } // Optional promo code
    }

    public class CreateGuestOrderItemRequest
    {
        [Required]
        public int ProductId { get; set; }

        public int? VariantId { get; set; }

        [Required]
        [Range(0.01, double.MaxValue)]
        public decimal Quantity { get; set; }

        [Required]
        [Range(0.01, double.MaxValue)]
        public decimal UnitPrice { get; set; }

        public string? Unit { get; set; }
    }

    public class GuestOrderResponse
    {
        public int OrderId { get; set; }
        public string OrderNumber { get; set; } = string.Empty;
        public string CustomerEmail { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
        public string Status { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
    }

    public class TrackOrderRequest
    {
        [Required]
        public string OrderNumber { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;
    }
}

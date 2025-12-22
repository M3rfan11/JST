using System.ComponentModel.DataAnnotations;

namespace Api.DTOs
{
    public class CreateOnlineOrderRequest
    {
        [Required]
        [MaxLength(100)]
        public string CustomerName { get; set; } = string.Empty;

        [EmailAddress]
        [MaxLength(255)]
        public string? CustomerEmail { get; set; }

        [MaxLength(20)]
        public string? CustomerPhone { get; set; }

        [MaxLength(500)]
        public string? CustomerAddress { get; set; }

        public DateTime? DeliveryDate { get; set; }

        [MaxLength(500)]
        public string? Notes { get; set; }

        [Required]
        public List<CreateOnlineOrderItemRequest> Items { get; set; } = new List<CreateOnlineOrderItemRequest>();
    }

    public class CreateOnlineOrderItemRequest
    {
        [Required]
        public int ProductId { get; set; }

        public int? VariantId { get; set; } // Optional: for variant-specific orders

        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "Quantity must be greater than 0")]
        public decimal Quantity { get; set; }

        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "Unit price must be greater than 0")]
        public decimal UnitPrice { get; set; }

        [MaxLength(50)]
        public string? Unit { get; set; }

        [MaxLength(200)]
        public string? Notes { get; set; }
    }

    public class UpdateOnlineOrderStatusRequest
    {
        [Required]
        public string Status { get; set; } = string.Empty; // Pending, Accepted, Shipped, Delivered, Cancelled

        public DateTime? DeliveryDate { get; set; }

        [MaxLength(500)]
        public string? Notes { get; set; }
    }

    public class UpdateOnlineInventoryRequest
    {
        [Required]
        [Range(0, double.MaxValue, ErrorMessage = "Quantity must be 0 or greater")]
        public decimal Quantity { get; set; }
    }

    public class OnlineOrderResponse
    {
        public int Id { get; set; }
        public string OrderNumber { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public string? CustomerEmail { get; set; }
        public string? CustomerPhone { get; set; }
        public string? CustomerAddress { get; set; }
        public DateTime OrderDate { get; set; }
        public DateTime? DeliveryDate { get; set; }
        public decimal TotalAmount { get; set; }
        public string Status { get; set; } = string.Empty;
        public string PaymentStatus { get; set; } = string.Empty;
        public string? Notes { get; set; }
        public string CreatedByUserName { get; set; } = string.Empty;
        public List<OnlineOrderItemResponse> Items { get; set; } = new List<OnlineOrderItemResponse>();
    }

    public class OnlineOrderItemResponse
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public int? VariantId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string? ProductSKU { get; set; }
        public string? VariantName { get; set; } // e.g., "Red / Large"
        public decimal Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal TotalPrice { get; set; }
        public string? Unit { get; set; }
    }

    public class OnlineInventoryResponse
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string? ProductSKU { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public decimal AvailableQuantity { get; set; }
        public string? Unit { get; set; }
        public decimal MinimumStockLevel { get; set; }
        public decimal MaximumStockLevel { get; set; }
    }
}

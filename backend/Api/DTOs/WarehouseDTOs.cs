namespace Api.DTOs
{
    public class CreateWarehouseRequest
    {
        public string Name { get; set; } = string.Empty;
        public string? Address { get; set; }
        public string? City { get; set; }
        public string? PhoneNumber { get; set; }
        public string? ManagerName { get; set; }
        public int? ManagerUserId { get; set; } // Assign a user as manager
        public bool IsActive { get; set; } = true;
    }

    public class UpdateWarehouseRequest
    {
        public string? Name { get; set; }
        public string? Address { get; set; }
        public string? City { get; set; }
        public string? PhoneNumber { get; set; }
        public string? ManagerName { get; set; }
        public int? ManagerUserId { get; set; } // Assign a user as manager
        public bool? IsActive { get; set; }
    }

    public class WarehouseResponse
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Address { get; set; }
        public string? City { get; set; }
        public string? PhoneNumber { get; set; }
        public string? ManagerName { get; set; }
        public int? ManagerUserId { get; set; }
        public string? ManagerEmail { get; set; } // Manager's email for easy reference
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public int ProductCount { get; set; }
        public decimal TotalInventoryValue { get; set; }
    }

    public class WarehouseListResponse
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Address { get; set; }
        public string? City { get; set; }
        public string? ManagerName { get; set; }
        public bool IsActive { get; set; }
        public int ProductCount { get; set; }
        public decimal TotalInventoryValue { get; set; }
    }
}

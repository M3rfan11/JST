namespace Api.DTOs
{
    public class CreateCategoryRequest
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? ImageUrls { get; set; } // JSON array of image URLs
        public bool IsActive { get; set; } = true;
    }

    public class UpdateCategoryRequest
    {
        public string? Name { get; set; }
        public string? Description { get; set; }
        public string? ImageUrls { get; set; } // JSON array of image URLs
        public bool? IsActive { get; set; }
    }

    public class CategoryResponse
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? ImageUrls { get; set; } // JSON array of image URLs
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public int ProductCount { get; set; }
    }

    public class CategoryListResponse
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? ImageUrls { get; set; } // JSON array of image URLs
        public bool IsActive { get; set; }
        public int ProductCount { get; set; }
    }
}

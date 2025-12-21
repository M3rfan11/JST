using System.ComponentModel.DataAnnotations;

namespace Api.Models;

public class Permission
{
    public int Id { get; set; }
    
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty; // e.g., "Products.Create", "Products.Update"
    
    [MaxLength(200)]
    public string? Description { get; set; }
    
    [Required]
    [MaxLength(50)]
    public string Resource { get; set; } = string.Empty; // e.g., "Products", "Categories", "Users"
    
    [Required]
    [MaxLength(50)]
    public string Action { get; set; } = string.Empty; // e.g., "Create", "Update", "Delete", "Read"
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation properties
    public virtual ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
}








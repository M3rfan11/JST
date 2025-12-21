namespace Api.Models;

public class RolePermission
{
    public int RoleId { get; set; }
    public int PermissionId { get; set; }
    
    public DateTime AssignedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation properties
    public virtual Role Role { get; set; } = null!;
    public virtual Permission Permission { get; set; } = null!;
}


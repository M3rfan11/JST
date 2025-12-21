using System.ComponentModel.DataAnnotations;

namespace Api.DTOs;

public class PermissionResponse
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Resource { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class AssignPermissionToRoleRequest
{
    [Required]
    public int PermissionId { get; set; }
}

public class RoleWithPermissionsResponse
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<PermissionResponse> Permissions { get; set; } = new();
}








using System.ComponentModel.DataAnnotations;

namespace Api.DTOs;

public class CreateRoleRequest
{
    [Required]
    [MaxLength(50)]
    public string Name { get; set; } = string.Empty;
    
    [MaxLength(200)]
    public string? Description { get; set; }
}

public class UpdateRoleRequest
{
    [MaxLength(50)]
    public string? Name { get; set; }
    
    [MaxLength(200)]
    public string? Description { get; set; }
}

public class RoleResponse
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class AssignRoleRequest
{
    [Required]
    public int RoleId { get; set; }
}

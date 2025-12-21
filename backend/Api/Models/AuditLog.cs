using System.ComponentModel.DataAnnotations;

namespace Api.Models;

public class AuditLog
{
    public int Id { get; set; }
    
    public int? ActorUserId { get; set; }
    
    [Required]
    [MaxLength(100)]
    public string Entity { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(50)]
    public string EntityId { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(50)]
    public string Action { get; set; } = string.Empty;
    
    public string? Before { get; set; }
    
    public string? After { get; set; }
    
    public DateTime At { get; set; } = DateTime.UtcNow;
    
    [MaxLength(200)]
    public string? IpAddress { get; set; }
    
    [MaxLength(500)]
    public string? UserAgent { get; set; }
    
    // Navigation properties
    public virtual User? ActorUser { get; set; }
}

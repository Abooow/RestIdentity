using System.ComponentModel.DataAnnotations;

namespace RestIdentity.DataAccess.Models;

public class AuditLogRecord
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(450)]
    public string UserId { get; set; }

    [Required]
    [MaxLength(100)]
    public string Type { get; set; }

    [MaxLength(200)]
    public string? Description { get; set; }

    [MaxLength(100)]
    public string? IpAddress { get; set; }

    [MaxLength(100)]
    public string? OperatingSystem { get; set; }

    public DateTime Date { get; set; }
}

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RestIdentity.DataAccess.Models;

public class UserAvatarRecord
{
    [Key]
    [Required]
    [MaxLength(450)]
    public string UserId { get; set; }

    [Required]
    [MaxLength(40)]
    [Column(TypeName = "varchar(40)")]
    public string AvatarHash { get; set; }

    public bool UsesDefaultAvatar { get; set; }

    [MaxLength(10)]
    public string? ImageExtension { get; set; }

    [Required]
    public DateTime LastModifiedDate { get; set; }

    public virtual UserRecord User { get; set; }

    public UserAvatarRecord()
    {
        LastModifiedDate = DateTime.UtcNow;
        UsesDefaultAvatar = true;
    }

    public UserAvatarRecord(UserRecord user)
        : this()
    {
        User = user;
        UserId = user.Id;
    }
}

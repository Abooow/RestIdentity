using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RestIdentity.DataAccess.Models;

public class UserAvatarDao
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

    public virtual UserDao User { get; set; }

    public UserAvatarDao()
    {
        LastModifiedDate = DateTime.UtcNow;
        UsesDefaultAvatar = true;
    }

    public UserAvatarDao(UserDao user)
        : this()
    {
        User = user;
        UserId = user.Id;
    }
}

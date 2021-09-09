using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RestIdentity.Server.Models.DAO;

public class UserAvatarModel
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
    public string ImageExtension { get; set; }

    [Required]
    public DateTime LastModifiedDate { get; set; }

    public virtual ApplicationUser User { get; set; }

    public UserAvatarModel()
    {
        LastModifiedDate = DateTime.UtcNow;
        UsesDefaultAvatar = true;
    }

    public UserAvatarModel(ApplicationUser user)
        : this()
    {
        User = user;
        UserId = user.Id;
    }
}

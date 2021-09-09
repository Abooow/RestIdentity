using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace RestIdentity.Server.Models.DAO;

public sealed class ApplicationUser : IdentityUser
{
    [Required]
    [MaxLength(50)]
    public string FirstName { get; set; }

    [Required]
    [MaxLength(50)]
    public string LastName { get; set; }

    public DateTime DateCreated { get; set; }

    public UserAvatarModel UserAvatar { get; set; }
}
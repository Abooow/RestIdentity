using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore.Metadata.Internal;

namespace RestIdentity.Server.Models.DAO;

public sealed class ApplicationUser : IdentityUser
{
    [Required]
    [MaxLength(50)]
    public string FirstName { get; set; }

    [Required]
    [MaxLength(50)]
    public string LastName { get; set; }

    [Required]
    [MaxLength(40)]
    [Column(TypeName = "varchar(40)")]
    public string ProfilePicHash { get; set; }

    public DateTime DateCreated { get; set; }
}
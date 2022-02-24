using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace RestIdentity.DataAccess.Models;

public sealed class UserRecord : IdentityUser
{
    [Required]
    [MaxLength(50)]
    public string FirstName { get; set; }

    [Required]
    [MaxLength(50)]
    public string LastName { get; set; }

    public DateTime DateCreated { get; set; }
}
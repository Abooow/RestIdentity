using System.ComponentModel.DataAnnotations;

namespace RestIdentity.Shared.Models.Requests;

public sealed class UpdateProfileRequest
{
    [Required]
    public string? Password { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
}

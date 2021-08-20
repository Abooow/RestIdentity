using System.ComponentModel.DataAnnotations;

namespace RestIdentity.Shared.Models.Requests;

public sealed class ResetPasswordRequest
{
    [Required]
    [DataType(DataType.Text)]
    public string? Code { get; set; }

    [Required]
    [EmailAddress]
    [DataType(DataType.EmailAddress)]
    public string? Email { get; set; }

    [Required]
    [DataType(DataType.Password)]
    public string? Password { get; set; }

    [Required]
    [Compare(nameof(Password), ErrorMessage = "Passwords do not match!")]
    [DataType(DataType.Password)]
    public string? PasswordConfirm { get; set; }
}

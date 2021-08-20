using System.ComponentModel.DataAnnotations;

namespace RestIdentity.Shared.Models.Requests;

public sealed class RegisterRequest
{
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

    [Required]
    [Range(typeof(bool), "true", "true", ErrorMessage = "Please agree to Terms and Conditions")]
    public bool AgreeTerms { get; set; }
}

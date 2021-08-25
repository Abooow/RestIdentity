using System.ComponentModel.DataAnnotations;

namespace RestIdentity.Shared.Models.Requests;

public sealed class RegisterRequest
{
    [Required]
    [EmailAddress]
    [DataType(DataType.EmailAddress)]
    public string? Email { get; set; }

    [Required]
    [DataType(DataType.Text)]
    [StringLength(20, MinimumLength = 2)]
    [RegularExpression(@"^[a-zA-Z][a-zA-Z0-9]{1,19}$", ErrorMessage = "Please enter a valid UserName")]
    public string? UserName { get; set; }

    [Required]
    [DataType(DataType.Text)]
    [StringLength(50, MinimumLength = 2)]
    public string? FirstName { get; set; }

    [Required]
    [DataType(DataType.Text)]
    [StringLength(50, MinimumLength = 2)]
    public string? LastName { get; set; }

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

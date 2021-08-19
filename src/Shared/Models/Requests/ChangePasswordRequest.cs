using System.ComponentModel.DataAnnotations;

namespace RestIdentity.Shared.Models.Requests;

public sealed class ChangePasswordRequest
{
    [Required]
    [DataType(DataType.Password)]
    public string OldPassword { get; set; }

    [Required]
    [DataType(DataType.Password)]
    public string NewPassword { get; set; }

    [Required]
    [Compare(nameof(NewPassword), ErrorMessage = "Passwords do not match!")]
    [DataType(DataType.Password)]
    public string NewPasswordConfirm { get; set; }
}

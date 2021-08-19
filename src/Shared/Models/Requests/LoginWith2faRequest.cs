using System.ComponentModel.DataAnnotations;

namespace RestIdentity.Shared.Models.Requests;

public sealed class LoginWith2faRequest
{
    [Required]
    [StringLength(7, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 6)]
    [DataType(DataType.Text)]
    public string TwoFactorCode { get; set; }

    public bool RememberMe { get; set; }

    public bool RememberMachine { get; set; }
}

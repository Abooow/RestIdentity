using System.ComponentModel.DataAnnotations;

namespace RestIdentity.Shared.Models.Requests
{
    public sealed class LoginWithRecoveryCodeRequest
    {
        [Required]
        [DataType(DataType.Text)]
        public string RecoveryCode { get; set; }
    }
}

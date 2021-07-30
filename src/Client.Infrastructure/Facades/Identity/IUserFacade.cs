using RestIdentity.Shared.Models;
using RestIdentity.Shared.Models.Requests;
using RestIdentity.Shared.Models.Response;
using RestIdentity.Shared.Wrapper;
using System.Threading.Tasks;

namespace RestIdentity.Client.Infrastructure.Facades.Identity
{
    public interface IUserFacade
    {
        Task<IResult<CurrentUser>> GetMeAsync();

        Task<IResult> RegisterUserAsync(RegisterRequest registerRequest);

        Task<IResult> ConfirmEmailAsync(string userId, string code);

        Task<IResult> ResendEmailConfirmationAsync(EmailAddress emailAddress);

        Task<IResult> ChangePasswordAsync(ChangePasswordRequest changePasswordRequest);

        Task<IResult> ForgotPasswordAsync(EmailAddress emailAddress);

        Task<IResult> ResetPasswordAsync(ResetPasswordRequest resetPasswordRequest);

        Task<IResult<TwoFactorQRCode>> EnableTwoFactorAuthAsync();

        Task<IResult> DisableTwoFactorAuthAsync();

        Task<IResult<RecoveryCodes>> GenerateRecoveryCodesAsync();
            
        Task<IResult> ResetAuthenticatorAsync();
    }
}

namespace RestIdentity.Client.Infrastructure.Facades.Identity;

public interface IUserFacade
{
    Task<Result<CurrentUser>> GetMeAsync();

    Task<Result> RegisterUserAsync(RegisterRequest registerRequest);

    Task<Result> ConfirmEmailAsync(string userId, string code);

    Task<Result> ResendEmailConfirmationAsync(EmailAddress emailAddress);

    Task<Result> ChangePasswordAsync(ChangePasswordRequest changePasswordRequest);

    Task<Result> ForgotPasswordAsync(EmailAddress emailAddress);

    Task<Result> ResetPasswordAsync(ResetPasswordRequest resetPasswordRequest);

    Task<Result<TwoFactorQRCode>> EnableTwoFactorAuthAsync();

    Task<Result> DisableTwoFactorAuthAsync();

    Task<Result<RecoveryCodes>> GenerateRecoveryCodesAsync();

    Task<Result> ResetAuthenticatorAsync();
}

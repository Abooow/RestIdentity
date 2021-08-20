using System.Net.Http.Json;

namespace RestIdentity.Client.Infrastructure.Facades.Identity;

public sealed class UserFacade : IUserFacade
{
    private readonly HttpClient httpClient;

    public UserFacade(HttpClient httpClient)
    {
        this.httpClient = httpClient;
    }

    public async Task<Result<CurrentUser>> GetMeAsync()
    {
        HttpResponseMessage response = await httpClient.GetAsync(UserEndpoints.GetMe)!;
        return await response.ToResult<CurrentUser>();
    }

    public async Task<Result> RegisterUserAsync(RegisterRequest registerRequest)
    {
        HttpResponseMessage response = await httpClient.PostAsJsonAsync(UserEndpoints.Register, registerRequest)!;
        return await response.ToResult();
    }

    public async Task<Result> ConfirmEmailAsync(string userId, string code)
    {
        HttpResponseMessage response = await httpClient.PostAsync(UserEndpoints.ConfirmEmail(userId, code), null);
        return await response.ToResult();
    }

    public async Task<Result> ResendEmailConfirmationAsync(EmailAddress emailAddress)
    {
        HttpResponseMessage response = await httpClient.PostAsJsonAsync(UserEndpoints.ResendEmailConfirmation, emailAddress);
        return await response.ToResult();
    }

    public async Task<Result> ChangePasswordAsync(ChangePasswordRequest changePasswordRequest)
    {
        HttpResponseMessage response = await httpClient.PostAsJsonAsync(UserEndpoints.ChangePassword, changePasswordRequest);
        return await response.ToResult();
    }

    public async Task<Result> ForgotPasswordAsync(EmailAddress emailAddress)
    {
        HttpResponseMessage response = await httpClient.PostAsJsonAsync(UserEndpoints.ForgotPassword, emailAddress);
        return await response.ToResult();
    }

    public async Task<Result> ResetPasswordAsync(ResetPasswordRequest resetPasswordRequest)
    {
        HttpResponseMessage response = await httpClient.PostAsJsonAsync(UserEndpoints.ResetPassword, resetPasswordRequest);
        return await response.ToResult();
    }

    public async Task<Result<TwoFactorQRCode>> EnableTwoFactorAuthAsync()
    {
        HttpResponseMessage response = await httpClient.PostAsync(UserEndpoints.EnableTwoFactorAuth, null);
        return await response.ToResult<TwoFactorQRCode>();
    }

    public async Task<Result> DisableTwoFactorAuthAsync()
    {
        HttpResponseMessage response = await httpClient.PostAsync(UserEndpoints.DisableTwoFactorAuth, null);
        return await response.ToResult();
    }

    public async Task<Result<RecoveryCodes>> GenerateRecoveryCodesAsync()
    {
        HttpResponseMessage response = await httpClient.GetAsync(UserEndpoints.GenerateRecoveryCodes);
        return await response.ToResult<RecoveryCodes>();
    }

    public async Task<Result> ResetAuthenticatorAsync()
    {
        HttpResponseMessage response = await httpClient.PostAsync(UserEndpoints.ResetPassword, null);
        return await response.ToResult();
    }
}

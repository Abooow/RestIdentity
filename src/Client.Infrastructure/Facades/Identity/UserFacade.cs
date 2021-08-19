using System.Net.Http.Json;
using RestIdentity.Client.Infrastructure.Extensions;
using RestIdentity.Client.Infrastructure.Routes;
using RestIdentity.Shared.Models;
using RestIdentity.Shared.Models.Requests;
using RestIdentity.Shared.Models.Response;
using RestIdentity.Shared.Wrapper;

namespace RestIdentity.Client.Infrastructure.Facades.Identity;

public sealed class UserFacade : IUserFacade
{
    private readonly HttpClient httpClient;

    public UserFacade(HttpClient httpClient)
    {
        this.httpClient = httpClient;
    }

    public async Task<IResult<CurrentUser>> GetMeAsync()
    {
        HttpResponseMessage response = await httpClient.GetAsync(UserEndpoints.GetMe)!;
        return await response.ToResult<CurrentUser>();
    }

    public async Task<IResult> RegisterUserAsync(RegisterRequest registerRequest)
    {
        HttpResponseMessage response = await httpClient.PostAsJsonAsync(UserEndpoints.Register, registerRequest)!;
        return await response.ToResult();
    }

    public async Task<IResult> ConfirmEmailAsync(string userId, string code)
    {
        HttpResponseMessage response = await httpClient.PostAsync(UserEndpoints.ConfirmEmail(userId, code), null);
        return await response.ToResult();
    }

    public async Task<IResult> ResendEmailConfirmationAsync(EmailAddress emailAddress)
    {
        HttpResponseMessage response = await httpClient.PostAsJsonAsync(UserEndpoints.ResendEmailConfirmation, emailAddress);
        return await response.ToResult();
    }

    public async Task<IResult> ChangePasswordAsync(ChangePasswordRequest changePasswordRequest)
    {
        HttpResponseMessage response = await httpClient.PostAsJsonAsync(UserEndpoints.ChangePassword, changePasswordRequest);
        return await response.ToResult();
    }

    public async Task<IResult> ForgotPasswordAsync(EmailAddress emailAddress)
    {
        HttpResponseMessage response = await httpClient.PostAsJsonAsync(UserEndpoints.ForgotPassword, emailAddress);
        return await response.ToResult();
    }

    public async Task<IResult> ResetPasswordAsync(ResetPasswordRequest resetPasswordRequest)
    {
        HttpResponseMessage response = await httpClient.PostAsJsonAsync(UserEndpoints.ResetPassword, resetPasswordRequest);
        return await response.ToResult();
    }

    public async Task<IResult<TwoFactorQRCode>> EnableTwoFactorAuthAsync()
    {
        HttpResponseMessage response = await httpClient.PostAsync(UserEndpoints.EnableTwoFactorAuth, null);
        return await response.ToResult<TwoFactorQRCode>();
    }

    public async Task<IResult> DisableTwoFactorAuthAsync()
    {
        HttpResponseMessage response = await httpClient.PostAsync(UserEndpoints.DisableTwoFactorAuth, null);
        return await response.ToResult();
    }

    public async Task<IResult<RecoveryCodes>> GenerateRecoveryCodesAsync()
    {
        HttpResponseMessage response = await httpClient.GetAsync(UserEndpoints.GenerateRecoveryCodes);
        return await response.ToResult<RecoveryCodes>();
    }

    public async Task<IResult> ResetAuthenticatorAsync()
    {
        HttpResponseMessage response = await httpClient.PostAsync(UserEndpoints.ResetPassword, null);
        return await response.ToResult();
    }
}

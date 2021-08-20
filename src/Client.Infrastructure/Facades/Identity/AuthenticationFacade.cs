using System.Net.Http.Json;

namespace RestIdentity.Client.Infrastructure.Facades.Identity;

public sealed class AuthenticationFacade : IAuthenticationFacade
{
    private readonly HttpClient httpClient;

    public AuthenticationFacade(HttpClient httpClient)
    {
        this.httpClient = httpClient;
    }

    public async Task<Result> LoginAsync(LoginRequest loginRequest)
    {
        HttpResponseMessage response = await httpClient.PostAsJsonAsync(AuthenticationEndpoints.Login, loginRequest)!;
        return await response.ToResult();
    }

    public async Task<Result> LoginWith2faAsync(LoginWith2faRequest loginWith2faRequest)
    {
        HttpResponseMessage response = await httpClient.PostAsJsonAsync(AuthenticationEndpoints.LoginWith2fa, loginWith2faRequest)!;
        return await response.ToResult();
    }

    public async Task<Result> LoginWithRecoveryCodeAsync(LoginWithRecoveryCodeRequest loginWithRecoveryCodeRequest)
    {
        HttpResponseMessage response = await httpClient.PostAsJsonAsync(AuthenticationEndpoints.LoginWithRecoveryCode, loginWithRecoveryCodeRequest)!;
        return await response.ToResult();
    }

    public async Task<Result> LogoutAsync()
    {
        HttpResponseMessage response = await httpClient.PostAsync(AuthenticationEndpoints.Logout, null);
        return await response.ToResult();
    }
}

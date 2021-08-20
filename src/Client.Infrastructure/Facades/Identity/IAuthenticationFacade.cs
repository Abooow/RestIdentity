namespace RestIdentity.Client.Infrastructure.Facades.Identity;

public interface IAuthenticationFacade
{
    Task<Result> LoginAsync(LoginRequest loginRequest);

    Task<Result> LoginWith2faAsync(LoginWith2faRequest loginWith2faRequest);

    Task<Result> LoginWithRecoveryCodeAsync(LoginWithRecoveryCodeRequest loginWithRecoveryCodeRequest);

    Task<Result> LogoutAsync();
}

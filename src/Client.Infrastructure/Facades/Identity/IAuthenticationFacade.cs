using RestIdentity.Shared.Models.Requests;
using RestIdentity.Shared.Models.Response;
using RestIdentity.Shared.Wrapper;
using System.Threading.Tasks;

namespace RestIdentity.Client.Infrastructure.Facades.Identity
{
    public interface IAuthenticationFacade
    {
        Task<IResult<TokenResponse>> LoginAsync(LoginRequest loginRequest);

        Task<IResult> LoginWith2faAsync(LoginWith2faRequest loginWith2faRequest);

        Task<IResult> LoginWithRecoveryCodeAsync(LoginWithRecoveryCodeRequest loginWithRecoveryCodeRequest);

        Task<IResult<TokenResponse>> RefreshToken(RefreshTokenRequest refreshTokenRequest);

        Task<IResult> LogoutAsync();
    }
}

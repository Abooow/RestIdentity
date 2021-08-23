using RestIdentity.Shared.Models.Requests;
using RestIdentity.Shared.Models.Response;
using RestIdentity.Shared.Wrapper;

namespace RestIdentity.Server.Services.Authentication;

public interface IAuthService
{
    Task<Result<TokenResponse>> Authenticate(LoginRequest loginRequest);
}

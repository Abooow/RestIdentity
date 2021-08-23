using System.Security.Claims;

namespace RestIdentity.Shared.Models.Response;

public sealed class TokenResponse
{
    public string Token { get; init; }
    public string RefreshToken { get; init; }
    public DateTime ExpirationDate { get; init; }
    public DateTime RefreshTokenExpirationDate { get; init; }
    public IEnumerable<string> Roles { get; init; }
    public string Username { get; init; }
    public string UserId { get; init; }
    public bool TwoFactorLoginOn { get; init; }
    public ClaimsPrincipal Principal { get; init; }
}

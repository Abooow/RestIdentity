using System.Security.Claims;

namespace RestIdentity.Shared.Models.Response;

public sealed record TokenResponse
(
    string Token,
    string RefreshToken,
    DateTime ExpirationDate,
    DateTime RefreshTokenExpirationDate,
    IEnumerable<string> Roles,
    string Username,
    string UserId,
    bool TwoFactorLoginOn,
    ClaimsPrincipal Principal
);

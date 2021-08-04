using RestIdentity.Shared.Models.Response;
using System;
using System.Security.Claims;
using System.Collections.Immutable;
using System.IdentityModel.Tokens.Jwt;
using System.Collections.Generic;

namespace RestIdentity.Server.Services.Jwt
{
    public interface IJwtManager
    {
        IImmutableDictionary<string, RefreshTokenResponse> UsersRefreshTokensReadOnlyDictionary { get; }

        TokenResponse GenerateToken(string trackerIdentifier, IEnumerable<Claim> claims, DateTime startingDate);
        TokenResponse Refresh(string refreshToken, string accessToken, DateTime startingDate);
        void RemoveExpiredRefreshTokens(DateTime now);
        void RemoveRefreshTokenByTrackerIdentifier(string trackerIdentifier);
        (ClaimsPrincipal, JwtSecurityToken) DecodeJwtToken(string token);
    }
}

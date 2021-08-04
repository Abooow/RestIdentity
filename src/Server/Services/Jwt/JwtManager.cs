using RestIdentity.Shared.Models.Response;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Security.Claims;
using System.Security.Cryptography;
using System.IdentityModel.Tokens.Jwt;
using RestIdentity.Server.Models.Infrastructure;

namespace RestIdentity.Server.Services.Jwt
{
    public class JwtManager : IJwtManager
    {
        public IImmutableDictionary<string, RefreshTokenResponse> UsersRefreshTokensReadOnlyDictionary => usersRefreshTokens.ToImmutableDictionary();

        private readonly ConcurrentDictionary<string, RefreshTokenResponse> usersRefreshTokens;  // can store in a database or a distributed cache
        private readonly JwtTokenConfig _jwtTokenConfig;
        private readonly byte[] secret;

        public JwtManager(JwtTokenConfig jwtTokenConfig)
        {
            _jwtTokenConfig = jwtTokenConfig;

            usersRefreshTokens = new ConcurrentDictionary<string, RefreshTokenResponse>();
            secret = Encoding.ASCII.GetBytes(jwtTokenConfig.Secret);
        }

        public TokenResponse GenerateToken(string trackerIdentifier, IEnumerable<Claim> claims, DateTime startingDate)
        {
            bool shouldAddAudienceClaim = string.IsNullOrWhiteSpace(claims?.FirstOrDefault(x => x.Type == JwtRegisteredClaimNames.Aud)?.Value);
            JwtSecurityToken jwtToken = new JwtSecurityToken(
                _jwtTokenConfig.Issuer,
                shouldAddAudienceClaim ? _jwtTokenConfig.Audience : string.Empty,
                claims,
                expires: startingDate.AddMinutes(_jwtTokenConfig.AccessTokenExpirationMinutes),
                signingCredentials: new SigningCredentials(new SymmetricSecurityKey(secret), SecurityAlgorithms.HmacSha256Signature));
            string accessToken = new JwtSecurityTokenHandler().WriteToken(jwtToken);

            RefreshTokenResponse refreshToken = new RefreshTokenResponse
            {
                TrackerIdentifier = trackerIdentifier,
                Token = GenerateRefreshTokenString(),
                ExpiryTime = startingDate.AddMinutes(_jwtTokenConfig.RefreshTokenExpirationMinutes)
            };
            usersRefreshTokens.AddOrUpdate(refreshToken.Token, refreshToken, (_, _) => refreshToken);

            return new TokenResponse
            {
                Token = accessToken,
                RefreshToken = refreshToken
            };
        }

        public TokenResponse Refresh(string refreshToken, string accessToken, DateTime startingDate)
        {
            var (principal, jwtToken) = DecodeJwtToken(accessToken);
            if (jwtToken == null || !jwtToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256Signature))
                throw new SecurityTokenException("Invalid token");

            string userName = principal.Identity?.Name;
            if (!usersRefreshTokens.TryGetValue(refreshToken, out var existingRefreshToken))
                throw new SecurityTokenException("Invalid token");
            if (existingRefreshToken.TrackerIdentifier != userName || existingRefreshToken.ExpiryTime < startingDate)
                throw new SecurityTokenException("Invalid token");

            return GenerateToken(userName, principal.Claims, startingDate);
        }

        // optional: clean up expired refresh tokens
        public void RemoveExpiredRefreshTokens(DateTime now)
        {
            IEnumerable<KeyValuePair<string, RefreshTokenResponse>> expiredTokens = usersRefreshTokens.Where(x => x.Value.ExpiryTime < now);

            foreach (KeyValuePair<string, RefreshTokenResponse> expiredToken in expiredTokens)
            {
                usersRefreshTokens.TryRemove(expiredToken.Key, out _);
            }
        }

        public void RemoveRefreshTokenByTrackerIdentifier(string trackerIdentifier)
        {
            // TODO: Add jwt Token to a block-list in db.

            IEnumerable<KeyValuePair<string, RefreshTokenResponse>> refreshTokens = usersRefreshTokens.Where(x => x.Value.TrackerIdentifier == trackerIdentifier);

            foreach (var refreshToken in refreshTokens)
            {
                usersRefreshTokens.TryRemove(refreshToken.Key, out _);
            }
        }

        public (ClaimsPrincipal, JwtSecurityToken) DecodeJwtToken(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
                throw new SecurityTokenException("Invalid token");
            
            ClaimsPrincipal principal = new JwtSecurityTokenHandler()
                .ValidateToken(token, new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidIssuer = _jwtTokenConfig.Issuer,
                        ValidateIssuerSigningKey = true,
                        IssuerSigningKey = new SymmetricSecurityKey(secret),
                        ValidAudience = _jwtTokenConfig.Audience,
                        ValidateAudience = true,
                        ValidateLifetime = true,
                        ClockSkew = TimeSpan.FromMinutes(1)
                    },
                    out SecurityToken validatedToken);

            return (principal, validatedToken as JwtSecurityToken);
        }

        private static string GenerateRefreshTokenString()
        {
            byte[] randomNumber = new byte[32];

            using RandomNumberGenerator randomNumberGenerator = RandomNumberGenerator.Create();
            randomNumberGenerator.GetBytes(randomNumber);

            return Convert.ToBase64String(randomNumber);
        }
    }
}

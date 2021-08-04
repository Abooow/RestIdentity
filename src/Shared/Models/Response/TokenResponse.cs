using System;

namespace RestIdentity.Shared.Models.Response
{
    public sealed class TokenResponse
    {
        public string Token { get; set; }
        public RefreshTokenResponse RefreshToken { get; set; }
    }
}

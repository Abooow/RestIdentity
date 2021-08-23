namespace RestIdentity.Server.Models;

public sealed class JwtSettings
{
    public string Secret { get; set; }
    public string ClientId { get; set; }
    public string Issuer { get; set; }
    public string Audience { get; set; }
    public string AccessTokenExpirationInMinutes { get; set; }
    public string RefreshTokenExpirationInMinutes { get; set; }
    public bool ValidateIssuerSigningKey { get; set; }
    public bool ValidateIssuer { get; set; }
    public bool ValidateAudience { get; set; }
    public bool AllowSiteWideTokenRefresh { get; set; }
}

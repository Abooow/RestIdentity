namespace RestIdentity.Shared.Models.Response;

public sealed class RefreshTokenResponse
{
    public string TrackerIdentifier { get; set; }
    public string Token { get; set; }
    public DateTime ExpiryTime { get; set; }
}

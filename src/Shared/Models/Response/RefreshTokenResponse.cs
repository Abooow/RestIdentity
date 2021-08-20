namespace RestIdentity.Shared.Models.Response;

public sealed record RefreshTokenResponse(string TrackerIdentifier, string Token, DateTime ExpiryTime);
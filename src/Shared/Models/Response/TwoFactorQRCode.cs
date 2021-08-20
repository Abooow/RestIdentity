namespace RestIdentity.Shared.Models.Response;

public sealed record TwoFactorQRCode(string SharedKey, string AuthenticatorUri);

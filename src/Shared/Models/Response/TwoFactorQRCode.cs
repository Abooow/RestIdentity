namespace RestIdentity.Shared.Models.Response
{
    public sealed class TwoFactorQRCode
    {
        public string SharedKey { get; set; }

        public string AuthenticatorUri { get; set; }
    }
}

namespace RestIdentity.Shared.Models;

public sealed class RecoveryCodes
{
    public IEnumerable<string> Codes { get; set; }

    public RecoveryCodes()
    {
        Codes = Array.Empty<string>();
    }
}

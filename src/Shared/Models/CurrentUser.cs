namespace RestIdentity.Shared.Models;

public sealed class CurrentUser
{
    public string Email { get; set; }
    public Dictionary<string, string> Claims { get; set; }

    public CurrentUser()
    {
        Claims = new Dictionary<string, string>();
    }
}

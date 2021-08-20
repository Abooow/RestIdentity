namespace RestIdentity.Shared.Models;

public sealed record CurrentUser(string Email, IReadOnlyDictionary<string, string>? Claims);
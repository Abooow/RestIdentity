namespace RestIdentity.Shared.Models;

public sealed record UserProfile
(
    string AvatarUrl,
    string UserName,
    string FirstName,
    string LastName,
    DateTime CreatedOn,
    IEnumerable<string> Roles
);

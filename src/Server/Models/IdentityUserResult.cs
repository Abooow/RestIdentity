using Microsoft.AspNetCore.Identity;
using RestIdentity.DataAccess.Models;

namespace RestIdentity.Server.Models;

public sealed record IdentityUserResult(IdentityResult IdentityResult, UserDao? User)
{
    public IEnumerable<IdentityError> Errors => IdentityResult.Errors;
    public bool Succeeded => IdentityResult.Succeeded;
    public bool FailedToGetUser => User is null;

    public IdentityUserResult(IdentityResult identityResult)
        : this(identityResult, null)
    {
        IdentityResult = identityResult;
    }

    public static IdentityUserResult Failed()
    {
        return new IdentityUserResult(IdentityResult.Failed());
    }
}

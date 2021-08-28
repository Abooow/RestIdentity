using Microsoft.AspNetCore.Identity;
using RestIdentity.Server.Models.DAO;

namespace RestIdentity.Server.Models;

public sealed record IdentityUserResult(IdentityResult IdentityResult, ApplicationUser? User)
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

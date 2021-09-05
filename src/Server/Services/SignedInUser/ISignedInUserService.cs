using RestIdentity.Server.Models.DAO;

namespace RestIdentity.Server.Services.SignedInUser;

public interface ISignedInUserService
{
    string GetUserId();
    Task<ApplicationUser> GetUserAsync();
}
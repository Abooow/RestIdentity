using RestIdentity.DataAccess.Models;

namespace RestIdentity.Server.Services.SignedInUser;

public interface ISignedInUserService
{
    string GetUserId();
    Task<UserDao> GetUserAsync();
}
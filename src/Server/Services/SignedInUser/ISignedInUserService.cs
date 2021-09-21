using RestIdentity.DataAccess.Models;

namespace RestIdentity.Server.Services;

public interface ISignedInUserService
{
    string GetUserId();
    Task<UserRecord> GetUserAsync();
}
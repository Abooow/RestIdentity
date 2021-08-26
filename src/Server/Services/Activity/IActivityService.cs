using RestIdentity.Server.Models.DAO;

namespace RestIdentity.Server.Services.Activity;

public interface IActivityService
{
    Task AddUserActivityForSignInUser(string type);
    Task AddUserActivityForSignInUser(string type, string data);
    Task AddUserActivity(string userId, string type);
    Task AddUserActivity(string userId, string type, string data);
    Task<IEnumerable<ActivityModel>> GetPartialUserActivity(string userId);
    Task<IEnumerable<ActivityModel>> GetFullUserActivity(string userId);
}

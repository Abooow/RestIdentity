using RestIdentity.Server.Models.DAO;

namespace RestIdentity.Server.Services.Activity;

public interface IActivityService
{
    Task AddUserActivityForSignInUserAsync(string type);
    Task AddUserActivityForSignInUserAsync(string type, string data);
    Task AddUserActivityAsync(string userId, string type);
    Task AddUserActivityAsync(string userId, string type, string data);
    Task<IEnumerable<ActivityModel>> GetPartialUserActivityAsync(string userId);
    Task<IEnumerable<ActivityModel>> GetFullUserActivityAsync(string userId);
}

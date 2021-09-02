using RestIdentity.Server.Models.DAO;

namespace RestIdentity.Server.Services.Activity;

public interface IActivityService
{
    Task AddUserActivityForSignInUserAsync(string type);
    Task AddUserActivityForSignInUserAsync(string type, string data);
    Task AddUserActivityAsync(string userId, string type);
    Task AddUserActivityAsync(string userId, string type, string data);
    Task<(bool UserFound, IEnumerable<ActivityModel> UserActivities)> GetPartialUserActivitiesAsync(string userId);
    Task<(bool UserFound, IEnumerable<ActivityModel> UserActivities)> GetFullUserActivitiesAsync(string userId);
}

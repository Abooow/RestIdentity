using RestIdentity.Server.Models;

namespace RestIdentity.Server.Services.Activity;

public interface IActivityService
{
    Task AddUserActivity(ActivityModel activity);
    Task<IEnumerable<ActivityModel>> GetUserActivity(string userId);
}

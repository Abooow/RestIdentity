using RestIdentity.Server.Models.DAO;

namespace RestIdentity.Server.Services.Activity;

public interface IActivityService
{
    Task AddUserActivity(ActivityModel activity);
    Task<IEnumerable<ActivityModel>> GetPartialUserActivity(string userId);
    Task<IEnumerable<ActivityModel>> GetFullUserActivity(string userId);
}

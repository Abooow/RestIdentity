using RestIdentity.DataAccess.Models;

namespace RestIdentity.DataAccess.Repositories;

public interface IUserAvatarsRepository
{
    string CreateAvatarHashForUser(string userId);
    string CreateAvatarHashForUser(UserRecord user);

    Task<UserAvatarRecord?> FindByUserIdAsync(string userId);
    Task<UserAvatarRecord?> FindByUserNameAsync(string userName);
    Task<UserAvatarRecord?> FindByAvatarHashAsync(string avatarHash);

    Task<UserAvatarRecord> AddUserAvatarAsync(UserRecord user);
    Task<UserAvatarRecord> AddOrUpdateUserAvatarAsync(UserRecord user);
    Task<UserAvatarRecord?> UpdateUserAvatarAsync(UserRecord user);

    Task<UserAvatarRecord?> UseDefaultAvatarForUserAsync(string userId);
    Task<UserAvatarRecord?> UseAvatarForUserAsync(string userId, string imageExtension);
}

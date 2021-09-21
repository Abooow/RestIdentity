using RestIdentity.DataAccess.Models;

namespace RestIdentity.DataAccess.Repositories;

public interface IUserAvatarsRepository
{
    string CreateAvatarHashForUser(string userId);
    string CreateAvatarHashForUser(UserDao user);

    Task<UserAvatarDao?> FindByUserIdAsync(string userId);
    Task<UserAvatarDao?> FindByUserNameAsync(string userName);
    Task<UserAvatarDao?> FindByAvatarHashAsync(string avatarHash);

    Task<UserAvatarDao> AddUserAvatarAsync(UserDao user);
    Task<UserAvatarDao> AddOrUpdateUserAvatarAsync(UserDao user);
    Task<UserAvatarDao?> UpdateUserAvatarAsync(UserDao user);

    Task<UserAvatarDao?> UseDefaultAvatarForUserAsync(string userId);
    Task<UserAvatarDao?> UseAvatarForUserAsync(string userId, string imageExtension);
}

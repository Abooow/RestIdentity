using RestIdentity.DataAccess.Models;

namespace RestIdentity.DataAccess.Repositories;

public interface IUserAvatarRepository
{
    string CreateAvatarHashForUser(UserDao user);

    Task<UserAvatarDao?> FindByUserIdAsync(string userId);
    Task<UserAvatarDao?> FindByUserNameAsync(string userName);
    Task<UserAvatarDao?> FindByAvatarHashAsync(string avatarHash);

    Task AddUserAvatarAsync(UserDao user);
    Task AddOrUpdateUserAvatarAsync(UserDao user);
    Task UpdateUserAvatarAsync(UserDao user);

    Task UseDefaultAvatarForUserAsync(string userId);
    Task UseAvatarForUserAsync(string userId, string imageExtension);
}

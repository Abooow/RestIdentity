using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using RestIdentity.DataAccess.Data;
using RestIdentity.DataAccess.Models;
using RestIdentity.DataAccess.Repositories;

namespace RestIdentity.DataAccess.EntityFrameworkCore.Repositories;

internal class UserAvatarRepository : IUserAvatarRepository
{
    private readonly ApplicationDbContext _dbContext;

    public UserAvatarRepository(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public string CreateAvatarHashForUser(string userId)
    {
        using var sha1 = SHA1.Create();
        byte[] hash = sha1.ComputeHash(Encoding.UTF8.GetBytes(userId));
        string userIdHash = string.Concat(hash.Select(x => x.ToString("x2")));

        return userIdHash;
    }

    public string CreateAvatarHashForUser(UserDao user)
    {
        return CreateAvatarHashForUser(user.Id);
    }

    public Task<UserAvatarDao?> FindByUserIdAsync(string userId)
    {
        return _dbContext.UserAvatars
            .Where(x => x.UserId == userId)
            .Include(x => x.User)
            .FirstOrDefaultAsync()!;
    }

    public Task<UserAvatarDao?> FindByUserNameAsync(string userName)
    {
        userName = userName.ToUpperInvariant();
        var query = from u in _dbContext.Users
                    from ua in _dbContext.UserAvatars.Include(x => x.User)
                    where u.Id == ua.UserId && u.NormalizedUserName == userName
                    select ua;

        return query.FirstOrDefaultAsync();
    }

    public Task<UserAvatarDao?> FindByAvatarHashAsync(string avatarHash)
    {
        avatarHash = avatarHash.ToUpperInvariant();

        return _dbContext.UserAvatars
            .Where(x => x.AvatarHash == avatarHash)
            .Include(x => x.User)
            .FirstOrDefaultAsync()!;
    }

    public async Task<UserAvatarDao> AddUserAvatarAsync(UserDao user)
    {
        var userAvatar = new UserAvatarDao(user)
        {
            AvatarHash = CreateAvatarHashForUser(user)
        };

        await _dbContext.UserAvatars.AddAsync(userAvatar);
        await _dbContext.SaveChangesAsync();

        return userAvatar;
    }

    public async Task<UserAvatarDao> AddOrUpdateUserAvatarAsync(UserDao user)
    {
        return await UserHasAvatar(user.Id)
            ? (await UpdateUserAvatarAsync(user))!
            : await AddUserAvatarAsync(user);
    }

    public async Task<UserAvatarDao?> UpdateUserAvatarAsync(UserDao user)
    {
        UserAvatarDao? existingUserAvatar = await FindByUserIdAsync(user.Id);

        if (existingUserAvatar is null)
            return null;

        existingUserAvatar.LastModifiedDate = DateTime.UtcNow;

        _dbContext.Update(existingUserAvatar);
        await _dbContext.SaveChangesAsync();

        return existingUserAvatar;
    }

    public Task<UserAvatarDao?> UseDefaultAvatarForUserAsync(string userId)
    {
        return UpdateUserAvatarAsync(userId, null);
    }

    public Task<UserAvatarDao?> UseAvatarForUserAsync(string userId, string imageExtension)
    {
        return UpdateUserAvatarAsync(userId, imageExtension);
    }

    private Task<bool> UserHasAvatar(string userId)
    {
        return _dbContext.UserAvatars.Where(x => x.UserId == userId).AnyAsync();
    }

    private async Task<UserAvatarDao?> UpdateUserAvatarAsync(string userId, string? imageExtension)
    {
        UserAvatarDao? userAvatar = await FindByUserIdAsync(userId);
        if (userAvatar is null)
            return null;

        userAvatar.LastModifiedDate = DateTime.UtcNow;
        userAvatar.UsesDefaultAvatar = imageExtension is null;
        userAvatar.ImageExtension = imageExtension;

        _dbContext.Update(userAvatar);
        await _dbContext.SaveChangesAsync();

        return userAvatar;
    }
}

using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using RestIdentity.DataAccess.Models;
using RestIdentity.DataAccess.Repositories;
using RestIdentity.Server.Data;

namespace RestIdentity.DataAccess.EntityFrameworkCore.Repositories;

internal class UserAvatarRepository : IUserAvatarRepository
{
    private readonly ApplicationDbContext _dbContext;

    public UserAvatarRepository(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public string CreateAvatarHashForUser(UserDao user)
    {
        using var sha1 = SHA1.Create();
        byte[] hash = sha1.ComputeHash(Encoding.UTF8.GetBytes(user.Id));
        string userIdHash = string.Concat(hash.Select(x => x.ToString("x2")));

        return userIdHash;
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

    public async Task AddUserAvatarAsync(UserDao user)
    {
        var userAvatar = new UserAvatarDao(user)
        {
            AvatarHash = CreateAvatarHashForUser(user)
        };

        await _dbContext.UserAvatars.AddAsync(userAvatar);
        await _dbContext.SaveChangesAsync();
    }

    public async Task AddOrUpdateUserAvatarAsync(UserDao user)
    {
        if (await UserHasAvatar(user.Id))
            await UpdateUserAvatarAsync(user);
        else
            await AddUserAvatarAsync(user);
    }

    public async Task UpdateUserAvatarAsync(UserDao user)
    {
        UserAvatarDao? existingUserAvatar = await FindByUserIdAsync(user.Id);

        if (existingUserAvatar is null)
            return; // TODO: Throw Exception?

        existingUserAvatar.LastModifiedDate = DateTime.UtcNow;

        _dbContext.Update(existingUserAvatar);
        await _dbContext.SaveChangesAsync();
    }

    public Task UseDefaultAvatarForUserAsync(string userId)
    {
        return UpdateUserAvatarAsync(userId, null);
    }

    public Task UseAvatarForUserAsync(string userId, string imageExtension)
    {
        return UpdateUserAvatarAsync(userId, imageExtension);
    }

    private Task<bool> UserHasAvatar(string userId)
    {
        return _dbContext.UserAvatars.Where(x => x.UserId == userId).AnyAsync();
    }

    private async Task UpdateUserAvatarAsync(string userId, string? imageExtension)
    {
        UserAvatarDao? userAvatar = await FindByUserIdAsync(userId);
        if (userAvatar is null)
            return; // TODO: Throw Exception?

        userAvatar.LastModifiedDate = DateTime.UtcNow;
        userAvatar.UsesDefaultAvatar = imageExtension is null;
        userAvatar.ImageExtension = imageExtension;

        _dbContext.Update(userAvatar);
        await _dbContext.SaveChangesAsync();
    }
}

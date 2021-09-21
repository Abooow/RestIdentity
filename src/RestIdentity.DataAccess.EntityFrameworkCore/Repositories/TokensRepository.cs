using Microsoft.EntityFrameworkCore;
using RestIdentity.DataAccess.Data;
using RestIdentity.DataAccess.Models;
using RestIdentity.DataAccess.Repositories;

namespace RestIdentity.DataAccess.EntityFrameworkCore.Repositories;

internal class TokensRepository : ITokensRepository
{
    private readonly ApplicationDbContext _dbContext;
    private Dictionary<string, bool> _haveRemovedUserTokens;

    public TokensRepository(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
        _haveRemovedUserTokens = new Dictionary<string, bool>();
    }

    public async Task AddUserTokenAsync(TokenRecord token)
    {
        if (!TokensHasBeenRemovedForUser(token.UserId))
            await RemoveTokensAsync(token.UserId);

        _dbContext.Tokens.Add(token);
        SetUserTokensRemoved(token.UserId, false);

        await _dbContext.SaveChangesAsync();
    }

    public Task<TokenRecord?> GetUserTokenAsync(string userId)
    {
        return _dbContext.Tokens.Where(x => x.UserId == userId).FirstOrDefaultAsync()!;
    }

    public Task<TokenRecord?> GetUserTokenAsync(string userId, string userName)
    {
        userName = userName.ToUpperInvariant();

        return _dbContext.Tokens.Include(x => x.User)
            .Where(x => x.UserId == userId && x.User.NormalizedUserName == userName)
            .FirstOrDefaultAsync()!;
    }

    public async Task<bool> RemoveAllUserTokensAsync(string userId)
    {
        if (TokensHasBeenRemovedForUser(userId))
            return false;

        IEnumerable<TokenRecord> userTokensToRemove = await RemoveTokensAsync(userId);
        await _dbContext.SaveChangesAsync();

        return userTokensToRemove.Any();
    }

    public Task<bool> UserHasAnyTokensAsync(string userId)
    {
        return _dbContext.Tokens.Where(x => x.UserId == userId).AnyAsync();
    }

    private async Task<IEnumerable<TokenRecord>> RemoveTokensAsync(string userId)
    {
        TokenRecord[] userTokensToRemove = await _dbContext.Tokens.Where(x => x.UserId == userId).ToArrayAsync();
        _dbContext.Tokens.RemoveRange(userTokensToRemove);

        SetUserTokensRemoved(userId, true);

        return userTokensToRemove;
    }

    private void SetUserTokensRemoved(string userId, bool removed)
    {
        if (_haveRemovedUserTokens.ContainsKey(userId))
            _haveRemovedUserTokens[userId] = removed;
        else
            _haveRemovedUserTokens.Add(userId, removed);
    }

    private bool TokensHasBeenRemovedForUser(string userId)
    {
        return _haveRemovedUserTokens.ContainsKey(userId) && _haveRemovedUserTokens[userId];
    }
}

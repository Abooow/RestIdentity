using RestIdentity.DataAccess.Models;

namespace RestIdentity.DataAccess.Repositories;

public interface ITokensRepository
{
    /// <summary>
    /// Will add a Token for a user. If the user has any other tokens they will be removed.
    /// </summary>
    Task AddUserTokenAsync(TokenDao token);
    Task<TokenDao?> GetUserTokenAsync(string userId);
    Task<TokenDao?> GetUserTokenAsync(string userId, string userName);
    Task<bool> RemoveAllUserTokensAsync(string userId);
    Task<bool> UserHasAnyTokensAsync(string userId);
}

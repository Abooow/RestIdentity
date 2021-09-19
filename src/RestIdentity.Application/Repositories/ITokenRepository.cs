using RestIdentity.DataAccess.Models;

namespace RestIdentity.DataAccess.Repositories;

public interface ITokenRepository
{
    Task AddUserTokenAsync(TokenDao token);
    Task<TokenDao?> GetUserTokenAsync(string userId);
    Task<bool> RemoveAllUserTokensAsync(string userId);
    Task<bool> UserHasAnyTokensAsync(string userId);
}

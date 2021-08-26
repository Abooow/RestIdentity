using RestIdentity.Server.Models.DAO;
using RestIdentity.Shared.Models;
using RestIdentity.Shared.Models.Requests;
using RestIdentity.Shared.Wrapper;

namespace RestIdentity.Server.Services.User;

public interface IUserService
{
    Task<Result<ApplicationUser>> RegisterUserAsync(RegisterRequest registerRequest);
    Task<Result<ApplicationUser>> RegisterAdminUserAsync(RegisterRequest registerRequest);
    Task<bool> CheckLoggedInUserPasswordAsync(string userName, string password);
    Task<UserProfile> GetUserProfileByIdAsync(string userId);
    Task<UserProfile> GetLoggedInUserProfileAsync();
    string GetLoggedInUserId();
}

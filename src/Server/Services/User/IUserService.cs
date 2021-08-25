using RestIdentity.Shared.Models;

namespace RestIdentity.Server.Services.User;

public interface IUserService
{
    Task<UserProfile> GetUserProfileById(string userId);
    Task<UserProfile> GetLoggedInUserProfile();
    string GetLoggedInUserId();
}

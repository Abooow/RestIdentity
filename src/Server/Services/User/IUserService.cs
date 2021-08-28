using Microsoft.AspNetCore.Identity;
using RestIdentity.Server.Models;
using RestIdentity.Server.Models.DAO;
using RestIdentity.Shared.Models;
using RestIdentity.Shared.Models.Requests;
using RestIdentity.Shared.Wrapper;

namespace RestIdentity.Server.Services.User;

public interface IUserService
{
    Task<Result<ApplicationUser>> RegisterUserAsync(RegisterRequest registerRequest);
    Task<Result<ApplicationUser>> RegisterAdminUserAsync(RegisterRequest registerRequest);
    Task<(bool Success, ApplicationUser User)> CheckLoggedInUserPasswordAsync(string password);
    Task<UserProfile> GetUserProfileByIdAsync(string userId);
    Task<UserProfile> GetLoggedInUserProfileAsync();
    Task<IdentityUserResult> ChangeSignedInUserPasswordAsync(ChangePasswordRequest changePasswordRequest);
    Task<IdentityUserResult> ChangePasswordAsync(string userId, ChangePasswordRequest changePasswordRequest);
    Task<Result<ApplicationUser>> GetLoggedInUserAsync();
    string GetLoggedInUserId();
}

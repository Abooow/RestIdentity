using RestIdentity.DataAccess.Models;
using RestIdentity.Server.Models;
using RestIdentity.Shared.Models;
using RestIdentity.Shared.Models.Requests;
using RestIdentity.Shared.Wrapper;

namespace RestIdentity.Server.Services;

public interface IUserService
{
    Task<Result<UserRecord>> RegisterUserAsync(RegisterRequest registerRequest);
    Task<Result<UserRecord>> RegisterAdminUserAsync(RegisterRequest registerRequest);

    Task<(bool Success, UserRecord User)> CheckLoggedInUserPasswordAsync(string password);

    Task<PersonalUserProfile> GetLoggedInUserProfileAsync();
    Task<PersonalUserProfile> GetUserProfileByIdAsync(string userId);
    Task<UserProfile> GetUserProfileByNameAsync(string userName);

    Task<IdentityUserResult> UpdateSignedInUserProfileAsync(UpdateProfileRequest updateProfileRequest);

    Task<IdentityUserResult> ChangeSignedInUserPasswordAsync(ChangePasswordRequest changePasswordRequest);
    Task<IdentityUserResult> ChangePasswordAsync(string userId, ChangePasswordRequest changePasswordRequest);

    string GetSignedInUserId();
    Task<UserRecord> GetSignedInUserAsync();
}

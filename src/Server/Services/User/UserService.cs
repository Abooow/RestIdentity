using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.Routing;
using RestIdentity.Server.Constants;
using RestIdentity.Server.Extensions;
using RestIdentity.Server.Models;
using RestIdentity.Server.Models.DAO;
using RestIdentity.Server.Services.Activity;
using RestIdentity.Server.Services.ProfileImage;
using RestIdentity.Server.Services.SignedInUser;
using RestIdentity.Shared.Models;
using RestIdentity.Shared.Models.Requests;
using RestIdentity.Shared.Wrapper;
using Serilog;

namespace RestIdentity.Server.Services.User;

internal sealed class UserService : IUserService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ISignedInUserService _signedInUserInfoService;
    private readonly IActionContextAccessor _actionContextAccessor;
    private readonly IUrlHelperFactory _urlHelperFactory;
    private readonly IActivityService _activityService;
    private readonly IProfileImageService _profileImageService;

    public UserService(
        UserManager<ApplicationUser> userManager,
        ISignedInUserService signedInUserInfoService,
        IActionContextAccessor actionContextAccessor,
        IUrlHelperFactory urlHelperFactory,
        IActivityService activityService,
        IProfileImageService profileImageService)
    {
        _userManager = userManager;
        _signedInUserInfoService = signedInUserInfoService;
        _actionContextAccessor = actionContextAccessor;
        _urlHelperFactory = urlHelperFactory;
        _activityService = activityService;
        _profileImageService = profileImageService;
    }

    public async Task<Result<ApplicationUser>> RegisterUserAsync(RegisterRequest registerRequest)
    {
        var user = new ApplicationUser
        {
            UserName = registerRequest.UserName,
            Email = registerRequest.Email,
            FirstName = registerRequest.FirstName,
            LastName = registerRequest.LastName,
            ProfilePicHash = "",
            DateCreated = DateTime.UtcNow
        };

        IdentityResult result = await _userManager.CreateAsync(user, registerRequest.Password);
        if (!result.Succeeded)
            return Result<ApplicationUser>.Fail(result.Errors.Select(x => x.Description)).AsBadRequest();

        await _profileImageService.CreateDefaultProfileImageAsync(user);

        Log.Information("Customer User {Email} registered", user.Email);
        await _userManager.AddToRoleAsync(user, RolesConstants.Customer);

        await _activityService.AddUserActivityAsync(user.Id, ActivityConstants.AuthRegistered);

        return Result<ApplicationUser>.Success(user);
    }

    public async Task<Result<ApplicationUser>> RegisterAdminUserAsync(RegisterRequest registerRequest)
    {
        ApplicationUser signedInUser = await GetSignedInUserAsync();
        if (signedInUser is null)
            return Result<ApplicationUser>.Fail("Not Authorized.").AsUnauthorized();

        if (!await _userManager.IsInRoleAsync(signedInUser, RolesConstants.Admin))
            return Result<ApplicationUser>.Fail("Not Authorized.").AsUnauthorized();

        var user = new ApplicationUser
        {
            UserName = registerRequest.UserName,
            Email = registerRequest.Email,
            FirstName = registerRequest.FirstName,
            LastName = registerRequest.LastName,
            EmailConfirmed = true,
            ProfilePicHash = "",
            DateCreated = DateTime.UtcNow
        };

        IdentityResult result = await _userManager.CreateAsync(user, registerRequest.Password);
        if (!result.Succeeded)
            return Result<ApplicationUser>.Fail(result.Errors.Select(x => x.Description)).AsBadRequest();

        await _profileImageService.CreateDefaultProfileImageAsync(user);

        Log.Information("Admin User {Email} registered by {SignInUser}", user.Email, signedInUser.Email);
        await _userManager.AddToRoleAsync(user, RolesConstants.Admin);

        await _activityService.AddUserActivityForSignInUserAsync(ActivityConstants.CreateAdminUser, "USER_ID: " + user.Id);
        await _activityService.AddUserActivityAsync(user.Id, ActivityConstants.AuthRegistered);

        return Result<ApplicationUser>.Success(user);
    }

    public Task<(bool Success, ApplicationUser User)> CheckLoggedInUserPasswordAsync(string password)
    {
        return CheckUserPasswordAsync(GetSignedInUserId(), password);
    }

    public Task<PersonalUserProfile> GetLoggedInUserProfileAsync()
    {
        string id = GetSignedInUserId();
        return GetUserProfileByIdAsync(id);
    }

    public async Task<PersonalUserProfile> GetUserProfileByIdAsync(string userId)
    {
        ApplicationUser user = await _userManager.FindByIdAsync(userId);
        if (user is null)
            return null;

        IUrlHelper urlHelper = _urlHelperFactory.GetUrlHelper(_actionContextAccessor.ActionContext);
        string profilePicUrl = urlHelper.AbsoluteAction("avatars", "GetProfileImage", new { url = $"{user.ProfilePicHash}.png", size = 128 });

        IList<string> userRoles = await _userManager.GetRolesAsync(user);
        var userProfile = new PersonalUserProfile(
            profilePicUrl,
            user.Email,
            user.UserName,
            user.FirstName,
            user.LastName,
            user.PhoneNumber,
            user.TwoFactorEnabled,
            user.PhoneNumberConfirmed,
            user.EmailConfirmed,
            user.LockoutEnabled,
            user.DateCreated,
            userRoles);

        return userProfile;
    }

    public async Task<UserProfile> GetUserProfileByNameAsync(string userName)
    {
        ApplicationUser user = await _userManager.FindByNameAsync(userName);
        if (user is null)
            return null;

        IUrlHelper urlHelper = _urlHelperFactory.GetUrlHelper(_actionContextAccessor.ActionContext);
        string profilePicUrl = urlHelper.AbsoluteAction("avatars", "GetProfileImage", new { url = $"{user.ProfilePicHash}.png", size = 128 });

        IList<string> userRoles = await _userManager.GetRolesAsync(user);
        var userProfile = new UserProfile(
            profilePicUrl,
            user.UserName,
            user.FirstName,
            user.LastName,
            user.DateCreated,
            userRoles);

        return userProfile;
    }

    public async Task<IdentityUserResult> UpdateSignedInUserProfileAsync(UpdateProfileRequest updateProfileRequest)
    {
        (bool passwordCheckSucceeded, ApplicationUser user) = await CheckLoggedInUserPasswordAsync(updateProfileRequest.Password);
        if (!passwordCheckSucceeded)
        {
            Log.Information("Failed to Update user profile");
            return new IdentityUserResult(IdentityResult.Failed(new IdentityError() { Description = "Invalid Password." }));
        }

        if (updateProfileRequest.FirstName is null && updateProfileRequest.LastName is null)
        {
            Log.Information("User {Email} did not have anything to update", user.Email);
            return new IdentityUserResult(IdentityResult.Success, user);
        }

        string oldProfile = JsonSerializer.Serialize(new { user.FirstName, user.LastName });

        if (updateProfileRequest.FirstName is not null)
            user.FirstName = updateProfileRequest.FirstName;
        if (updateProfileRequest.LastName is not null)
            user.LastName = updateProfileRequest.LastName;
        IdentityResult updateProfileResult = await _userManager.UpdateAsync(user);

        Log.Information("User profile for user {Email} has been updated", user.Email);
        await _activityService.AddUserActivityAsync(user.Id, ActivityConstants.UserProfileUpdated, $"OLD_PROFILE: {oldProfile}");

        return new IdentityUserResult(updateProfileResult, user);
    }

    public async Task<IdentityUserResult> ChangeSignedInUserPasswordAsync(ChangePasswordRequest changePasswordRequest)
    {
        ApplicationUser loggedInUserResult = await GetSignedInUserAsync();
        if (loggedInUserResult is null)
        {
            Log.Warning("Failed to change password for logged in User, User not found");
            return IdentityUserResult.Failed();
        }

        IdentityResult changePasswordResult = await _userManager.ChangePasswordAsync(loggedInUserResult, changePasswordRequest.OldPassword, changePasswordRequest.NewPassword);
        if (changePasswordResult.Succeeded)
        {
            Log.Information("Changed Password for User {Email}", loggedInUserResult.Email);
            await _activityService.AddUserActivityAsync(loggedInUserResult.Id, ActivityConstants.AuthChangePassword);
        }

        return new IdentityUserResult(changePasswordResult, loggedInUserResult);
    }

    public async Task<IdentityUserResult> ChangePasswordAsync(string userId, ChangePasswordRequest changePasswordRequest)
    {
        ApplicationUser user = await _userManager.FindByIdAsync(userId);
        if (user is null)
        {
            Log.Warning("Failed to change password for {UserId}, User not found", userId);
            return IdentityUserResult.Failed();
        }

        IdentityResult changePasswordResult = await _userManager.ChangePasswordAsync(user, changePasswordRequest.OldPassword, changePasswordRequest.NewPassword);
        if (changePasswordResult.Succeeded)
        {
            Log.Information("Changed Password for another User {Email}", user.Email);
            await _activityService.AddUserActivityForSignInUserAsync(ActivityConstants.AuthChangeOtherPassword, "USER_ID: " + user.Id);
        }

        return new IdentityUserResult(changePasswordResult, user);
    }

    public string GetSignedInUserId()
    {
        return _signedInUserInfoService.GetUserId();
    }

    public Task<ApplicationUser> GetSignedInUserAsync()
    {
        return _signedInUserInfoService.GetUserAsync();
    }

    private async Task<(bool Success, ApplicationUser User)> CheckUserPasswordAsync(string id, string password)
    {
        ApplicationUser user = await _userManager.FindByIdAsync(id);
        if (user is null || !await _userManager.CheckPasswordAsync(user, password))
            return (false, null);

        return (true, user);
    }
}

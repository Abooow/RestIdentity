using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.Routing;
using RestIdentity.DataAccess;
using RestIdentity.DataAccess.Models;
using RestIdentity.Server.Constants;
using RestIdentity.Server.Extensions;
using RestIdentity.Server.Models;
using RestIdentity.Server.Services.AuditLog;
using RestIdentity.Server.Services.SignedInUser;
using RestIdentity.Server.Services.UserAvatars;
using RestIdentity.Shared.Models;
using RestIdentity.Shared.Models.Requests;
using RestIdentity.Shared.Wrapper;
using Serilog;

namespace RestIdentity.Server.Services.User;

internal sealed class UserService : IUserService
{
    private readonly UserManager<UserDao> _userManager;
    private readonly ISignedInUserService _signedInUserInfoService;
    private readonly IActionContextAccessor _actionContextAccessor;
    private readonly IUrlHelperFactory _urlHelperFactory;
    private readonly IAuditLogService _auditLogService;
    private readonly IUserAvatarService _userAvatarService;

    public UserService(
        UserManager<UserDao> userManager,
        ISignedInUserService signedInUserInfoService,
        IActionContextAccessor actionContextAccessor,
        IUrlHelperFactory urlHelperFactory,
        IAuditLogService auditLogService,
        IUserAvatarService userAvatarService)
    {
        _userManager = userManager;
        _signedInUserInfoService = signedInUserInfoService;
        _actionContextAccessor = actionContextAccessor;
        _urlHelperFactory = urlHelperFactory;
        _auditLogService = auditLogService;
        _userAvatarService = userAvatarService;
    }

    public async Task<Result<UserDao>> RegisterUserAsync(RegisterRequest registerRequest)
    {
        var user = new UserDao
        {
            UserName = registerRequest.UserName,
            Email = registerRequest.Email,
            FirstName = registerRequest.FirstName,
            LastName = registerRequest.LastName,
            DateCreated = DateTime.UtcNow
        };

        IdentityResult result = await _userManager.CreateAsync(user, registerRequest.Password);
        if (!result.Succeeded)
            return Result<UserDao>.Fail(result.Errors.Select(x => x.Description)).AsBadRequest();

        await _userAvatarService.CreateDefaultAvatarAsync(user);

        Log.Information("Customer User {Email} registered", user.Email);
        await _userManager.AddToRoleAsync(user, RolesConstants.Customer);

        await _auditLogService.AddAuditLogAsync(user.Id, AuditLogsConstants.AuthRegistered);

        return Result<UserDao>.Success(user);
    }

    public async Task<Result<UserDao>> RegisterAdminUserAsync(RegisterRequest registerRequest)
    {
        UserDao signedInUser = await GetSignedInUserAsync();
        if (signedInUser is null)
            return Result<UserDao>.Fail("Not Authorized.").AsUnauthorized();

        if (!await _userManager.IsInRoleAsync(signedInUser, RolesConstants.Admin))
            return Result<UserDao>.Fail("Not Authorized.").AsUnauthorized();

        var user = new UserDao
        {
            UserName = registerRequest.UserName,
            Email = registerRequest.Email,
            FirstName = registerRequest.FirstName,
            LastName = registerRequest.LastName,
            EmailConfirmed = true,
            DateCreated = DateTime.UtcNow
        };

        IdentityResult result = await _userManager.CreateAsync(user, registerRequest.Password);
        if (!result.Succeeded)
            return Result<UserDao>.Fail(result.Errors.Select(x => x.Description)).AsBadRequest();

        await _userAvatarService.CreateDefaultAvatarAsync(user);

        Log.Information("Admin User {Email} registered by {SignInUser}", user.Email, signedInUser.Email);
        await _userManager.AddToRoleAsync(user, RolesConstants.Admin);

        await _auditLogService.AddAuditLogForSignInUserAsync(AuditLogsConstants.CreateAdminUser, "USER_ID: " + user.Id);
        await _auditLogService.AddAuditLogAsync(user.Id, AuditLogsConstants.AuthRegistered);

        return Result<UserDao>.Success(user);
    }

    public Task<(bool Success, UserDao User)> CheckLoggedInUserPasswordAsync(string password)
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
        UserAvatarDao userAvatar = await _userAvatarService.FindByUserIdAsync(userId);
        if (userAvatar is null)
            return null;

        UserDao user = userAvatar.User;
        IList<string> userRoles = await _userManager.GetRolesAsync(user);
        string userAvatarUrl = GetUserAvatarUrl(userAvatar.AvatarHash);

        var userProfile = new PersonalUserProfile(
            userAvatarUrl,
            userAvatar.UsesDefaultAvatar,
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
        UserAvatarDao userAvatar = await _userAvatarService.FindByUserNameAsync(userName);
        if (userAvatar is null)
            return null;

        UserDao user = userAvatar.User;
        IList<string> userRoles = await _userManager.GetRolesAsync(user);
        string userAvatarUrl = GetUserAvatarUrl(userAvatar.AvatarHash);

        var userProfile = new UserProfile(
            userAvatarUrl,
            user.UserName,
            user.FirstName,
            user.LastName,
            user.DateCreated,
            userRoles);

        return userProfile;
    }

    public async Task<IdentityUserResult> UpdateSignedInUserProfileAsync(UpdateProfileRequest updateProfileRequest)
    {
        (bool passwordCheckSucceeded, UserDao user) = await CheckLoggedInUserPasswordAsync(updateProfileRequest.Password);
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
        await _auditLogService.AddAuditLogAsync(user.Id, AuditLogsConstants.UserProfileUpdated, $"OLD_PROFILE: {oldProfile}");

        return new IdentityUserResult(updateProfileResult, user);
    }

    public async Task<IdentityUserResult> ChangeSignedInUserPasswordAsync(ChangePasswordRequest changePasswordRequest)
    {
        UserDao loggedInUserResult = await GetSignedInUserAsync();
        if (loggedInUserResult is null)
        {
            Log.Warning("Failed to change password for logged in User, User not found");
            return IdentityUserResult.Failed();
        }

        IdentityResult changePasswordResult = await _userManager.ChangePasswordAsync(loggedInUserResult, changePasswordRequest.OldPassword, changePasswordRequest.NewPassword);
        if (changePasswordResult.Succeeded)
        {
            Log.Information("Changed Password for User {Email}", loggedInUserResult.Email);
            await _auditLogService.AddAuditLogAsync(loggedInUserResult.Id, AuditLogsConstants.AuthChangePassword);
        }

        return new IdentityUserResult(changePasswordResult, loggedInUserResult);
    }

    public async Task<IdentityUserResult> ChangePasswordAsync(string userId, ChangePasswordRequest changePasswordRequest)
    {
        UserDao user = await _userManager.FindByIdAsync(userId);
        if (user is null)
        {
            Log.Warning("Failed to change password for {UserId}, User not found", userId);
            return IdentityUserResult.Failed();
        }

        IdentityResult changePasswordResult = await _userManager.ChangePasswordAsync(user, changePasswordRequest.OldPassword, changePasswordRequest.NewPassword);
        if (changePasswordResult.Succeeded)
        {
            Log.Information("Changed Password for another User {Email}", user.Email);
            await _auditLogService.AddAuditLogForSignInUserAsync(AuditLogsConstants.AuthChangeOtherPassword, "USER_ID: " + user.Id);
        }

        return new IdentityUserResult(changePasswordResult, user);
    }

    public string GetSignedInUserId()
    {
        return _signedInUserInfoService.GetUserId();
    }

    public Task<UserDao> GetSignedInUserAsync()
    {
        return _signedInUserInfoService.GetUserAsync();
    }

    private async Task<(bool Success, UserDao User)> CheckUserPasswordAsync(string id, string password)
    {
        UserDao user = await _userManager.FindByIdAsync(id);
        if (user is null || !await _userManager.CheckPasswordAsync(user, password))
            return (false, null);

        return (true, user);
    }

    private string GetUserAvatarUrl(string userHash)
    {
        IUrlHelper urlHelper = _urlHelperFactory.GetUrlHelper(_actionContextAccessor.ActionContext);
        return urlHelper.AbsoluteAction("avatars", "GetUserAvatar", new { url = $"{userHash}.png", size = 128 });
    }
}

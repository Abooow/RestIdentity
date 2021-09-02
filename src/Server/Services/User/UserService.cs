using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using RestIdentity.Server.Constants;
using RestIdentity.Server.Data;
using RestIdentity.Server.Models;
using RestIdentity.Server.Models.DAO;
using RestIdentity.Server.Services.Activity;
using RestIdentity.Server.Services.Cookies;
using RestIdentity.Server.Services.ProfileImage;
using RestIdentity.Shared.Models;
using RestIdentity.Shared.Models.Requests;
using RestIdentity.Shared.Wrapper;
using Serilog;

namespace RestIdentity.Server.Services.User;

internal sealed class UserService : IUserService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ApplicationDbContext _context;
    private readonly DataProtectionKeys _dataProtectionKeys;
    private readonly IServiceProvider _serviceProvider;
    private readonly IWebHostEnvironment _hostEnvironment;
    private readonly ICookieService _cookieService;
    private readonly IActivityService _activityService;
    private readonly IProfileImageService _profileImageService;

    public UserService(UserManager<ApplicationUser> userManager,
        ApplicationDbContext context,
        IOptions<DataProtectionKeys> dataProtectionKeys,
        IServiceProvider serviceProvider,
        IWebHostEnvironment hostEnvironment,
        ICookieService cookieService,
        IActivityService activityService,
        IProfileImageService profileImageService)
    {
        _userManager = userManager;
        _context = context;
        _dataProtectionKeys = dataProtectionKeys.Value;
        _serviceProvider = serviceProvider;
        _hostEnvironment = hostEnvironment;
        _cookieService = cookieService;
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
            ProfilePictureUrl = "",
            DateCreated = DateTime.UtcNow
        };
        user.ProfilePictureUrl = await _profileImageService.CreateDefaultProfileImageAsync(user);
        IdentityResult result = await _userManager.CreateAsync(user, registerRequest.Password);

        if (!result.Succeeded)
            return Result<ApplicationUser>.Fail(result.Errors.Select(x => x.Description)).AsBadRequest();

        Log.Information("Customer User {Email} registered", user.Email);
        await _userManager.AddToRoleAsync(user, RolesConstants.Customer);

        await _activityService.AddUserActivityAsync(user.Id, ActivityConstants.AuthRegistered);

        return Result<ApplicationUser>.Success(user);
    }

    public async Task<Result<ApplicationUser>> RegisterAdminUserAsync(RegisterRequest registerRequest)
    {
        ApplicationUser signedInUser = await _userManager.FindByIdAsync(GetLoggedInUserId());
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
            ProfilePictureUrl = "",
            DateCreated = DateTime.UtcNow
        };
        user.ProfilePictureUrl = await _profileImageService.CreateDefaultProfileImageAsync(user);
        IdentityResult result = await _userManager.CreateAsync(user, registerRequest.Password);

        if (!result.Succeeded)
            return Result<ApplicationUser>.Fail(result.Errors.Select(x => x.Description)).AsBadRequest();

        Log.Information("Admin User {Email} registered by {SignInUser}", user.Email, signedInUser.Email);
        await _userManager.AddToRoleAsync(user, RolesConstants.Admin);

        await _activityService.AddUserActivityForSignInUserAsync(ActivityConstants.CreateAdminUser, "USER_ID: " + user.Id);
        await _activityService.AddUserActivityAsync(user.Id, ActivityConstants.AuthRegistered);

        return Result<ApplicationUser>.Success(user);
    }

    public async Task<(bool Success, ApplicationUser User)> CheckLoggedInUserPasswordAsync(string password)
    {
        ApplicationUser user = await _userManager.FindByIdAsync(GetLoggedInUserId());
        if (user is null || !await _userManager.CheckPasswordAsync(user, password))
            return (false, null);

        return (true, user);
    }

    public Task<PersonalUserProfile> GetLoggedInUserProfileAsync()
    {
        string id = GetLoggedInUserId();
        return GetUserProfileByIdAsync(id);
    }

    public async Task<PersonalUserProfile> GetUserProfileByIdAsync(string userId)
    {
        ApplicationUser user = await _userManager.FindByIdAsync(userId);
        if (user is null)
            return null;

        IList<string> userRoles = await _userManager.GetRolesAsync(user);
        var userProfile = new PersonalUserProfile(
            user.ProfilePictureUrl,
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

        IList<string> userRoles = await _userManager.GetRolesAsync(user);
        var userProfile = new UserProfile(
            user.ProfilePictureUrl,
            user.UserName,
            user.FirstName,
            user.LastName,
            user.DateCreated,
            userRoles);

        return userProfile;
    }

    public async Task<IdentityUserResult> ChangeSignedInUserPasswordAsync(ChangePasswordRequest changePasswordRequest)
    {
        Result<ApplicationUser> loggedInUserResult = await GetLoggedInUserAsync();
        if (!loggedInUserResult.Succeeded)
        {
            Log.Warning("Failed to change password for logged in User, User not found");
            return IdentityUserResult.Failed();
        }

        IdentityResult changePasswordResult = await _userManager.ChangePasswordAsync(loggedInUserResult.Data, changePasswordRequest.OldPassword, changePasswordRequest.NewPassword);
        if (changePasswordResult.Succeeded)
        {
            Log.Information("Changed Password for User {Email}", loggedInUserResult.Data.Email);
            await _activityService.AddUserActivityAsync(loggedInUserResult.Data.Id, ActivityConstants.AuthChangePassword);
        }

        return new IdentityUserResult(changePasswordResult, loggedInUserResult.Data);
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

    public async Task<Result<ApplicationUser>> GetLoggedInUserAsync()
    {
        string userId = GetLoggedInUserId();
        if (userId is null)
        {
            Log.Warning("Failed to get the logged in User");
            return Result<ApplicationUser>.Fail();
        }

        ApplicationUser user = await _userManager.FindByIdAsync(userId);
        return Result<ApplicationUser>.Success(user);
    }

    public string GetLoggedInUserId()
    {
        try
        {
            var protectorProvider = _serviceProvider.GetService<IDataProtectionProvider>();
            IDataProtector protector = protectorProvider.CreateProtector(_dataProtectionKeys.ApplicationUserKey);

            return protector.Unprotect(_cookieService.GetCookie(CookieConstants.UserId));
        }
        catch (Exception e)
        {
            Log.Error("An error occurred while trying to get user_id from cookies {Error} {StackTrace} {InnerExeption} {Source}",
                e.Message, e.StackTrace, e.InnerException, e.Source);
        }

        return null;
    }
}

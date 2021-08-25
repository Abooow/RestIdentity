using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using RestIdentity.Server.Constants;
using RestIdentity.Server.Data;
using RestIdentity.Server.Models;
using RestIdentity.Server.Models.DAO;
using RestIdentity.Server.Services.Activity;
using RestIdentity.Server.Services.Cookies;
using RestIdentity.Shared.Models;
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

    public UserService(UserManager<ApplicationUser> userManager,
        ApplicationDbContext context,
        IOptions<DataProtectionKeys> dataProtectionKeys,
        IServiceProvider serviceProvider,
        IWebHostEnvironment hostEnvironment,
        ICookieService cookieService,
        IActivityService activityService)
    {
        _userManager = userManager;
        _context = context;
        _dataProtectionKeys = dataProtectionKeys.Value;
        _serviceProvider = serviceProvider;
        _hostEnvironment = hostEnvironment;
        _cookieService = cookieService;
        _activityService = activityService;
    }

    public async Task<UserProfile> GetUserProfileById(string userId)
    {
        ApplicationUser user = await _userManager.FindByIdAsync(userId);
        if (user is null)
            return null;

        IList<string> userRoles = await _userManager.GetRolesAsync(user);
        var userProfile = new UserProfile(
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

    public Task<UserProfile> GetLoggedInUserProfile()
    {
        string id = GetLoggedInUserId();
        return GetUserProfileById(id);
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

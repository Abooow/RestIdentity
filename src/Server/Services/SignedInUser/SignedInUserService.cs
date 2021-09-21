using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using RestIdentity.DataAccess.Models;
using RestIdentity.Server.Constants;
using RestIdentity.Server.Models;
using Serilog;

namespace RestIdentity.Server.Services;

internal sealed class SignedInUserService : ISignedInUserService
{
    private readonly UserManager<UserRecord> _userManager;
    private readonly ICookieService _cookieService;
    private readonly IServiceProvider _serviceProvider;
    private readonly DataProtectionKeys _dataProtectionKeys;

    public SignedInUserService(
        UserManager<UserRecord> userManager,
        ICookieService cookieService,
        IServiceProvider serviceProvider,
        IOptions<DataProtectionKeys> dataProtectionKeys)
    {
        _userManager = userManager;
        _cookieService = cookieService;
        _serviceProvider = serviceProvider;
        _dataProtectionKeys = dataProtectionKeys.Value;
    }

    public string GetUserId()
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

    public async Task<UserRecord> GetUserAsync()
    {
        string userId = GetUserId();
        if (userId is null)
            return null;

        UserRecord user = await _userManager.FindByIdAsync(userId);
        return user;
    }
}

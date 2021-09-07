﻿using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using RestIdentity.Server.Constants;
using RestIdentity.Server.Models;
using RestIdentity.Server.Models.DAO;
using RestIdentity.Server.Services.Cookies;
using Serilog;

namespace RestIdentity.Server.Services.SignedInUser;

internal sealed class SignedInUserService : ISignedInUserService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ICookieService _cookieService;
    private readonly IServiceProvider _serviceProvider;
    private readonly DataProtectionKeys _dataProtectionKeys;

    public SignedInUserService(
        UserManager<ApplicationUser> userManager,
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

    public async Task<ApplicationUser> GetUserAsync()
    {
        string userId = GetUserId();
        if (userId is null)
            return null;

        ApplicationUser user = await _userManager.FindByIdAsync(userId);
        return user;
    }
}
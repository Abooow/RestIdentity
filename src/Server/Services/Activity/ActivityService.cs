using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using RestIdentity.Server.Constants;
using RestIdentity.Server.Data;
using RestIdentity.Server.Models;
using RestIdentity.Server.Models.DAO;
using RestIdentity.Server.Models.Ip;
using RestIdentity.Server.Services.Cookies;
using RestIdentity.Server.Services.IpInfo;
using RestIdentity.Server.Services.ProfileImage;
using RestIdentity.Server.Services.User;
using Serilog;

namespace RestIdentity.Server.Services.Activity;

public sealed class ActivityService : IActivityService
{
    private readonly ApplicationDbContext _context;
    private readonly DataProtectionKeys _dataProtectionKeys;
    private readonly IIpInfoService _ipInfoService;
    private readonly IServiceProvider _serviceProvider;
    private readonly ICookieService _cookieService;

    public ActivityService(ApplicationDbContext context,
        IOptions<DataProtectionKeys> dataProtectionKeys,
        IIpInfoService ipInfoService,
        IServiceProvider serviceProvider,
        ICookieService cookieService)
    {
        _context = context;
        _dataProtectionKeys = dataProtectionKeys.Value;
        _ipInfoService = ipInfoService;
        _serviceProvider = serviceProvider;
        _cookieService = cookieService;
    }

    public Task AddUserActivityForSignInUser(string type)
    {
        return AddUserActivityForSignInUser(type, null);
    }

    public Task AddUserActivityForSignInUser(string type, string data)
    {
        string userId = GetLoggedInUserId();
        if (userId is null)
        {
            Log.Error("Failed to add User Activity because userId was null");
            return Task.CompletedTask;
        }

        return AddUserActivity(userId, type, data);
    }

    public Task AddUserActivity(string userId, string type)
    {
        return AddUserActivity(userId, type, null);
    }

    public async Task AddUserActivity(string userId, string type, string data)
    {
        IIpInfo ipInfo = await _ipInfoService.GetIpInfo();
        var activity = new ActivityModel()
        {
            Type = type,
            Data = data,
            UserId = userId,
            IpAddress = _ipInfoService.GetRemoteIpAddress(),
            Location = ipInfo.Country is null ? "unknown" : $"{ipInfo.Country}, {ipInfo.City}",
            OperationgSystem = _ipInfoService.GetRemoteOperatingSystem(),
            Date = DateTime.UtcNow
        };

        await AddUserActivity(activity);
    }

    public async Task<IEnumerable<ActivityModel>> GetPartialUserActivity(string userId)

    {
        return await _context.Activities.Where(x => x.UserId == userId
            && x.Type == ActivityConstants.AuthSignedIn)
            .OrderBy(x => x.Date)
            .ToArrayAsync();
    }

    public async Task<IEnumerable<ActivityModel>> GetFullUserActivity(string userId)
    {
        return await _context.Activities.Where(x => x.UserId == userId)
            .OrderBy(x => x.Date)
            .ToArrayAsync();
    }

    private async Task AddUserActivity(ActivityModel activity)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            await _context.Activities.AddAsync(activity);
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
        }
        catch (Exception e)
        {
            Log.Error("An error occurred while committing a user Activity. {Error} {StackTrace} {InnerException} {Source}",
                e.Message, e.StackTrace, e.InnerException, e.Source);

            await transaction.RollbackAsync();
        }
    }

    private string GetLoggedInUserId()
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

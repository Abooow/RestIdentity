using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using RestIdentity.Server.Constants;
using RestIdentity.Server.Data;
using RestIdentity.Server.Models.DAO;
using RestIdentity.Server.Services.RemoteConnectionInfo;
using RestIdentity.Server.Services.SignedInUser;
using Serilog;

namespace RestIdentity.Server.Services.Activity;

public sealed class ActivityService : IActivityService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ISignedInUserService _signedInUserService;
    private readonly ApplicationDbContext _context;
    private readonly IRemoteConnectionInfoService _remoteConnectionService;

    public ActivityService(
        UserManager<ApplicationUser> userManager,
        ISignedInUserService signedInUserService,
        ApplicationDbContext context,
        IRemoteConnectionInfoService remoteConnectionService)
    {
        _userManager = userManager;
        _signedInUserService = signedInUserService;
        _context = context;
        _remoteConnectionService = remoteConnectionService;
    }

    public Task AddUserActivityForSignInUserAsync(string type)
    {
        return AddUserActivityForSignInUserAsync(type, null);
    }

    public Task AddUserActivityForSignInUserAsync(string type, string data)
    {
        string userId = _signedInUserService.GetUserId();
        if (userId is null)
        {
            Log.Error("Failed to add User Activity because userId was null");
            return Task.CompletedTask;
        }

        return AddUserActivityAsync(userId, type, data);
    }

    public Task AddUserActivityAsync(string userId, string type)
    {
        return AddUserActivityAsync(userId, type, null);
    }

    public async Task AddUserActivityAsync(string userId, string type, string data)
    {
        var activity = new ActivityModel()
        {
            Type = type,
            Data = data,
            UserId = userId,
            IpAddress = _remoteConnectionService.GetRemoteIpAddress(),
            OperationgSystem = _remoteConnectionService.GetRemoteOperatingSystem(),
            Date = DateTime.UtcNow
        };

        await AddUserActivityAsync(activity);
    }

    public async Task<(bool UserFound, IEnumerable<ActivityModel> UserActivities)> GetPartialUserActivitiesAsync(string userId)
    {
        ApplicationUser user = await _userManager.FindByIdAsync(userId);
        if (user is null)
            return (true, Array.Empty<ActivityModel>());

        ActivityModel[] activities = await _context.Activities
            .Where(x => x.UserId == userId && ActivityConstants.PartialActivityTypes.Contains(x.Type))
            .OrderBy(x => x.Date)
            .ToArrayAsync();

        return (true, activities);
    }

    public async Task<(bool UserFound, IEnumerable<ActivityModel> UserActivities)> GetFullUserActivitiesAsync(string userId)
    {
        ApplicationUser user = await _userManager.FindByIdAsync(userId);
        if (user is null)
            return (false, Array.Empty<ActivityModel>());

        ActivityModel[] activities = await _context.Activities
            .Where(x => x.UserId == userId)
            .OrderBy(x => x.Date)
            .ToArrayAsync();

        return (true, activities);
    }

    private async Task AddUserActivityAsync(ActivityModel activity)
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
}

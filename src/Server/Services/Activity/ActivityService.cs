using Microsoft.EntityFrameworkCore;
using RestIdentity.Server.Constants;
using RestIdentity.Server.Data;
using RestIdentity.Server.Models.DAO;
using Serilog;

namespace RestIdentity.Server.Services.Activity;

public sealed class ActivityService : IActivityService
{
    private readonly ApplicationDbContext _context;

    public ActivityService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task AddUserActivity(ActivityModel activity)
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
}

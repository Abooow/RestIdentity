using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using RestIdentity.Server.Constants;
using RestIdentity.Server.Data;
using RestIdentity.Server.Models.DAO;
using RestIdentity.Server.Services.Cookies;
using RestIdentity.Server.Services.SignedInUser;
using Serilog;

namespace RestIdentity.Server.Services.AuditLog;

public sealed class AuditLogService : IAuditLogService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ISignedInUserService _signedInUserService;
    private readonly ApplicationDbContext _context;
    private readonly ICookieService _cookieService;

    public AuditLogService(
        UserManager<ApplicationUser> userManager,
        ISignedInUserService signedInUserService,
        ApplicationDbContext context,
        ICookieService cookieService)
    {
        _userManager = userManager;
        _signedInUserService = signedInUserService;
        _context = context;
        _cookieService = cookieService;
    }

    public Task AddAuditLogForSignInUserAsync(string type)
    {
        return AddAuditLogForSignInUserAsync(type, null);
    }

    public Task AddAuditLogForSignInUserAsync(string type, string description)
    {
        string userId = _signedInUserService.GetUserId();
        if (userId is null)
        {
            Log.Error("Failed to add Audit Log for signed in user because userId was null");
            return Task.CompletedTask;
        }

        return AddAuditLogAsync(userId, type, description);
    }

    public Task AddAuditLogAsync(string userId, string type)
    {
        return AddAuditLogAsync(userId, type, null);
    }

    public async Task AddAuditLogAsync(string userId, string type, string description)
    {
        var auditLog = new AuditLogModel()
        {
            Type = type,
            Description = description,
            UserId = userId,
            IpAddress = _cookieService.GetRemoteIpAddress(),
            OperationgSystem = _cookieService.GetRemoteOperatingSystem(),
            Date = DateTime.UtcNow
        };

        await AddAuditLogAsync(auditLog);
    }

    public async Task<(bool UserFound, IEnumerable<AuditLogModel> AuditLogs)> GetPartialAuditLogsAsync(string userId)
    {
        ApplicationUser user = await _userManager.FindByIdAsync(userId);
        if (user is null)
            return (true, Array.Empty<AuditLogModel>());

        AuditLogModel[] auditLogs = await _context.AuditLogs
            .Where(x => x.UserId == userId && AuditLogsConstants.PartialAuditLogTypes.Contains(x.Type))
            .OrderBy(x => x.Date)
            .ToArrayAsync();

        return (true, auditLogs);
    }

    public async Task<(bool UserFound, IEnumerable<AuditLogModel> AuditLogs)> GetFullAuditLogsAsync(string userId)
    {
        ApplicationUser user = await _userManager.FindByIdAsync(userId);
        if (user is null)
            return (false, Array.Empty<AuditLogModel>());

        AuditLogModel[] auditLogs = await _context.AuditLogs
            .Where(x => x.UserId == userId)
            .OrderBy(x => x.Date)
            .ToArrayAsync();

        return (true, auditLogs);
    }

    private async Task AddAuditLogAsync(AuditLogModel auditLog)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            await _context.AuditLogs.AddAsync(auditLog);
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
        }
        catch (Exception e)
        {
            Log.Error("An error occurred while committing an Audit Log. {Error} {StackTrace} {InnerException} {Source}",
                e.Message, e.StackTrace, e.InnerException, e.Source);

            await transaction.RollbackAsync();
        }
    }
}

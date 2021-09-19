using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using RestIdentity.DataAccess.Models;
using RestIdentity.DataAccess.Repositories;
using RestIdentity.Server.Data;

namespace RestIdentity.DataAccess.EntityFrameworkCore.Repositories;

internal class AuditLogRepository : IAuditLogRepository
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<UserDao> _userManager;
    private readonly ILogger<AuditLogRepository> _logger;

    public AuditLogRepository(ApplicationDbContext context, UserManager<UserDao> userManager, ILogger<AuditLogRepository> logger)
    {
        _context = context;
        _userManager = userManager;
        _logger = logger;
    }

    public async Task AddAuditLogAsync(AuditLogDao auditLog)
    {
        auditLog.Date = DateTime.UtcNow;

        await using IDbContextTransaction transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            await _context.AuditLogs.AddAsync(auditLog);
            await _context.SaveChangesAsync();

            await transaction.CommitAsync();
        }
        catch (Exception e)
        {
            _logger.LogError(e, "An error occurred while committing an Audit Log.");
        }
    }

    public async Task<IEnumerable<AuditLogDao>?> GetAuditLogsAsync(string userId)
    {
        UserDao user = await _userManager.FindByIdAsync(userId);
        if (user is null)
            return null;

        AuditLogDao[] auditLogs = await _context.AuditLogs
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.Date)
            .ToArrayAsync();

        return auditLogs;
    }

    public async Task<IEnumerable<AuditLogDao>?> GetPartialAuditLogsAsync(string userId, IEnumerable<string> types)
    {
        UserDao user = await _userManager.FindByIdAsync(userId);
        if (user is null)
            return null;

        AuditLogDao[] auditLogs = await _context.AuditLogs
            .Where(x => x.UserId == userId && types.Contains(x.Type))
            .OrderByDescending(x => x.Date)
            .ToArrayAsync();

        return auditLogs;
    }
}

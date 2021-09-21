using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RestIdentity.DataAccess.Data;
using RestIdentity.DataAccess.Models;
using RestIdentity.DataAccess.Repositories;

namespace RestIdentity.DataAccess.EntityFrameworkCore.Repositories;

internal class AuditLogsRepository : IAuditLogsRepository
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<UserDao> _userManager;
    private readonly ILogger<AuditLogsRepository> _logger;

    public AuditLogsRepository(ApplicationDbContext context, IServiceProvider serviceProvider, ILogger<AuditLogsRepository> logger)
    {
        _context = context;
        _logger = logger;

        _userManager = serviceProvider.GetService<UserManager<UserDao>>()
            ?? throw new InvalidOperationException(
                "Type UserManager<UserDao> has not been registered. " +
                "Use the AddIdentityUserRepository() method to register the required UserManager, which can be found in the RestIdentity.DataAccess namespace.");
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

    public async Task AddAuditLogsAsync(IEnumerable<AuditLogDao> auditLogs)
    {
        foreach (var auditLog in auditLogs)
        {
            auditLog.Date = DateTime.UtcNow;
        }

        await using IDbContextTransaction transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            _context.AuditLogs.AddRange(auditLogs);
            await _context.SaveChangesAsync();

            await transaction.CommitAsync();
        }
        catch (Exception e)
        {
            _logger.LogError(e, "An error occurred while committing multiple Audit Logs.");
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

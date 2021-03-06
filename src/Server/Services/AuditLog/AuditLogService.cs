using RestIdentity.DataAccess.Models;
using RestIdentity.DataAccess.Repositories;
using RestIdentity.Server.Constants;
using Serilog;

namespace RestIdentity.Server.Services;

public sealed class AuditLogService : IAuditLogService
{
    private readonly IAuditLogsRepository _auditLogsRepository;
    private readonly ISignedInUserService _signedInUserService;
    private readonly ICookieService _cookieService;

    public AuditLogService(
        IAuditLogsRepository auditLogsRepository,
        ISignedInUserService signedInUserService,
        ICookieService cookieService)
    {
        _auditLogsRepository = auditLogsRepository;
        _signedInUserService = signedInUserService;
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

    public Task AddAuditLogAsync(string userId, string type, string description)
    {
        var auditLog = new AuditLogRecord()
        {
            Type = type,
            Description = description,
            UserId = userId,
            IpAddress = _cookieService.GetRemoteIpAddress(),
            OperatingSystem = _cookieService.GetRemoteOperatingSystem(),
        };

        return _auditLogsRepository.AddAuditLogAsync(auditLog);
    }

    public async Task<(bool UserFound, IEnumerable<AuditLogRecord> AuditLogs)> GetPartialAuditLogsAsync(string userId)
    {
        IEnumerable<AuditLogRecord> auditLogs = await _auditLogsRepository.GetPartialAuditLogsAsync(userId, AuditLogsConstants.PartialAuditLogTypes);

        return (auditLogs is not null, auditLogs ?? Enumerable.Empty<AuditLogRecord>());
    }

    public async Task<(bool UserFound, IEnumerable<AuditLogRecord> AuditLogs)> GetFullAuditLogsAsync(string userId)
    {
        IEnumerable<AuditLogRecord> auditLogs = await _auditLogsRepository.GetAuditLogsAsync(userId);

        return (auditLogs is not null, auditLogs ?? Enumerable.Empty<AuditLogRecord>());
    }
}

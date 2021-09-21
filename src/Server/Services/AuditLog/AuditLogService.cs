using RestIdentity.DataAccess.Models;
using RestIdentity.DataAccess.Repositories;
using RestIdentity.Server.Constants;
using RestIdentity.Server.Services.Cookies;
using RestIdentity.Server.Services.SignedInUser;
using Serilog;

namespace RestIdentity.Server.Services.AuditLog;

public sealed class AuditLogService : IAuditLogService
{
    private readonly IAuditLogRepository _auditLogRepository;
    private readonly ISignedInUserService _signedInUserService;
    private readonly ICookieService _cookieService;

    public AuditLogService(
        IAuditLogRepository auditLogRepository,
        ISignedInUserService signedInUserService,
        ICookieService cookieService)
    {
        _auditLogRepository = auditLogRepository;
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
        var auditLog = new AuditLogDao()
        {
            Type = type,
            Description = description,
            UserId = userId,
            IpAddress = _cookieService.GetRemoteIpAddress(),
            OperatingSystem = _cookieService.GetRemoteOperatingSystem(),
        };

        return _auditLogRepository.AddAuditLogAsync(auditLog);
    }

    public async Task<(bool UserFound, IEnumerable<AuditLogDao> AuditLogs)> GetPartialAuditLogsAsync(string userId)
    {
        IEnumerable<AuditLogDao> auditLogs = await _auditLogRepository.GetPartialAuditLogsAsync(userId, AuditLogsConstants.PartialAuditLogTypes);

        return (auditLogs is not null, auditLogs ?? Enumerable.Empty<AuditLogDao>());
    }

    public async Task<(bool UserFound, IEnumerable<AuditLogDao> AuditLogs)> GetFullAuditLogsAsync(string userId)
    {
        IEnumerable<AuditLogDao> auditLogs = await _auditLogRepository.GetAuditLogsAsync(userId);

        return (auditLogs is not null, auditLogs ?? Enumerable.Empty<AuditLogDao>());
    }
}

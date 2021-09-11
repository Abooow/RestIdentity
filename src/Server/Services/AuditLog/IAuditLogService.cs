using RestIdentity.Server.Models.DAO;

namespace RestIdentity.Server.Services.AuditLog;

public interface IAuditLogService
{
    Task AddAuditLogForSignInUserAsync(string type);
    Task AddAuditLogForSignInUserAsync(string type, string description);
    Task AddAuditLogAsync(string userId, string type);
    Task AddAuditLogAsync(string userId, string type, string description);
    Task<(bool UserFound, IEnumerable<AuditLogModel> AuditLogs)> GetPartialAuditLogsAsync(string userId);
    Task<(bool UserFound, IEnumerable<AuditLogModel> AuditLogs)> GetFullAuditLogsAsync(string userId);
}

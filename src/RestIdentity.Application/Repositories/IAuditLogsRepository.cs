using RestIdentity.DataAccess.Models;

namespace RestIdentity.DataAccess.Repositories;

public interface IAuditLogsRepository
{
    Task AddAuditLogAsync(AuditLogRecord auditLog);
    Task AddAuditLogsAsync(IEnumerable<AuditLogRecord> auditLogs);
    Task<IEnumerable<AuditLogRecord>?> GetAuditLogsAsync(string userId);
    Task<IEnumerable<AuditLogRecord>?> GetPartialAuditLogsAsync(string userId, IEnumerable<string> types);
}

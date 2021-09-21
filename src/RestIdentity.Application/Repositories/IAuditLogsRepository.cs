using RestIdentity.DataAccess.Models;

namespace RestIdentity.DataAccess.Repositories;

public interface IAuditLogsRepository
{
    Task AddAuditLogAsync(AuditLogDao auditLog);
    Task AddAuditLogsAsync(IEnumerable<AuditLogDao> auditLogs);
    Task<IEnumerable<AuditLogDao>?> GetAuditLogsAsync(string userId);
    Task<IEnumerable<AuditLogDao>?> GetPartialAuditLogsAsync(string userId, IEnumerable<string> types);
}

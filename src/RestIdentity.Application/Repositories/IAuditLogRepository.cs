using RestIdentity.DataAccess.Models;

namespace RestIdentity.DataAccess.Repositories;

public interface IAuditLogRepository
{
    Task AddAuditLogAsync(AuditLogDao auditLog);
    Task<IEnumerable<AuditLogDao>?> GetAuditLogsAsync(string userId);
    Task<IEnumerable<AuditLogDao>?> GetPartialAuditLogsAsync(string userId, IEnumerable<string> types);
}

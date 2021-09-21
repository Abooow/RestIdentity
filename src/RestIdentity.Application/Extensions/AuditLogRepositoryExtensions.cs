using RestIdentity.DataAccess.Models;

namespace RestIdentity.DataAccess.Repositories;

public static class AuditLogRepositoryExtensions
{
    public static Task AddRolesAsync(this IAuditLogsRepository auditLogsRepository, params AuditLogRecord[] auditLogs)
    {
        return auditLogsRepository.AddAuditLogsAsync(auditLogs);
    }
}

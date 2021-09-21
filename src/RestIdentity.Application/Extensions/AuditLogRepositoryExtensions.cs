using RestIdentity.DataAccess.Models;

namespace RestIdentity.DataAccess.Repositories;

public static class AuditLogRepositoryExtensions
{
    public static Task AddRolesAsync(this IAuditLogRepository auditLogRepository, params AuditLogDao[] auditLogs)
    {
        return auditLogRepository.AddAuditLogsAsync(auditLogs);
    }
}

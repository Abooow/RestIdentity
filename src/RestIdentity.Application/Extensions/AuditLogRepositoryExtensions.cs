using RestIdentity.DataAccess.Models;

namespace RestIdentity.DataAccess.Repositories;

public static class AuditLogRepositoryExtensions
{
    public static Task AddRolesAsync(this IAuditLogsRepository auditLogsRepository, params AuditLogDao[] auditLogs)
    {
        return auditLogsRepository.AddAuditLogsAsync(auditLogs);
    }
}

namespace RestIdentity.Shared.Models.Response;

public sealed record UserAuditLog(string Type, string IpAddress, DateTime Date);

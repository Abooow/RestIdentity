namespace RestIdentity.Shared.Models.Response;

public sealed record UserActivity(string Type, string IpAddress, string Location, DateTime Date);

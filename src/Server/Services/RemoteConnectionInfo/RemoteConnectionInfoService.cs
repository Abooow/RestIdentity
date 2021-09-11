namespace RestIdentity.Server.Services.RemoteConnectionInfo;

internal sealed class RemoteConnectionInfoService : IRemoteConnectionInfoService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public RemoteConnectionInfoService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public string GetRemoteIpAddress()
    {
        return _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString() ?? "::1";
    }

    public string GetRemoteOperatingSystem()
    {
        return _httpContextAccessor.HttpContext?.Request.Headers["User-Agent"] ?? "UNKNOWN";
    }
}

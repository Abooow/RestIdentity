/*
 * WARNING!!!
 * 
 * Use this service only for testing, and not in production!
 * Avoid calling the GetIpInfo() method too many times!
 * 
 * Buy an API key for a IpInfo service when publishing your site.
 * 
*/

using System.Globalization;
using RestIdentity.Server.Models.Ip;
using ModelsIp = RestIdentity.Server.Models.Ip;

namespace RestIdentity.Server.Services.IpInfo;

internal sealed class TestIpInfoService : IIpInfoService
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IHttpClientFactory _clientFactory;

    public TestIpInfoService(IHttpContextAccessor httpContextAccessor, IHttpClientFactory clientFactory)
    {
        _httpContextAccessor = httpContextAccessor;
        _clientFactory = clientFactory;
    }

    public string GetRemoteIpAddress()
    {
        return _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString() ?? "::1";
    }

    public string GetRemoteOperatingSystem()
    {
        return _httpContextAccessor.HttpContext?.Request.Headers["User-Agent"] ?? "UNKNOWN";
    }

    public Task<IIpInfo> GetIpInfoAsync(CancellationToken cancellationToken)
    {
        string userIp = GetRemoteIpAddress();
        return GetIpInfoAsync(userIp, cancellationToken);
    }

    public async Task<IIpInfo> GetIpInfoAsync(string ip, CancellationToken cancellationToken = default)
    {
        HttpClient client = _clientFactory.CreateClient();
        ModelsIp::IpInfo ipInfo = ip == "::1"
            ? new ModelsIp.IpInfo() { Ip = ip }
            : await client.GetFromJsonAsync<ModelsIp::IpInfo>($"http://ipinfo.io/{ip}", cancellationToken);

        if (ipInfo.Country is not null)
        {
            var regionalInfo = new RegionInfo(ipInfo.Country);
            ipInfo.Country = regionalInfo.EnglishName;
        }

        return ipInfo;
    }
}

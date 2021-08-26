using RestIdentity.Server.Models.Ip;

namespace RestIdentity.Server.Services.IpInfo;

public interface IIpInfoService
{
    string GetRemoteIpAddress();
    string GetRemoteOperatingSystem();
    Task<IIpInfo> GetIpInfoAsync(CancellationToken cancellationToken = default);
    Task<IIpInfo> GetIpInfoAsync(string ip, CancellationToken cancellationToken = default);
}

using RestIdentity.Server.Models.Ip;

namespace RestIdentity.Server.Services.IpInfo;

public interface IIpInfoService
{
    string GetRemoteIpAddress();
    string GetRemoteOperatingSystem();
    Task<IIpInfo> GetIpInfo(CancellationToken cancellationToken = default);
    Task<IIpInfo> GetIpInfo(string ip, CancellationToken cancellationToken = default);
}

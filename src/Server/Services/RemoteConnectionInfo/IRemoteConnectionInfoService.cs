namespace RestIdentity.Server.Services.RemoteConnectionInfo;

public interface IRemoteConnectionInfoService
{
    string GetRemoteIpAddress();
    string GetRemoteOperatingSystem();
}

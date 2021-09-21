namespace RestIdentity.Server.Services;

public interface ICookieService
{
    string GetCookie(string key);
    string GetRemoteIpAddress();
    string GetRemoteOperatingSystem();
    void SetCookie(string key, string value, DateTime? expireTime);
    void SetCookie(string key, string value, DateTime? expireTime, bool isSecure, bool isHttpOnly);
    void DeleteCookie(string key);
    void DeleteCookies(IEnumerable<string> keys);
}

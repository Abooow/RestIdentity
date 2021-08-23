namespace RestIdentity.Server.Services.Cookies;

public interface ICookieService
{
    string GetCookie(string key);
    void SetCookie(string key, string value, DateTime? expireTime);
    void SetCookie(string key, string value, DateTime? expireTime, bool isSecure, bool isHttpOnly);
    void DeleteCookie(string key);
    void DeleteCookies(IEnumerable<string> keys);
    string GetUserIP();
    string GetUserCountry();
    string GetUserOS();
}

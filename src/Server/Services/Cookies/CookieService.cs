﻿namespace RestIdentity.Server.Services.Cookies;

public class CookieService : ICookieService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CookieService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public string GetCookie(string key)
    {
        return _httpContextAccessor.HttpContext.Request.Cookies[key];
    }

    public void SetCookie(string key, string value, DateTime? expireTime)
    {
        var cookieOptions = new CookieOptions
        {
            Expires = expireTime ?? DateTime.UnixEpoch,
            Secure = true,
            HttpOnly = true,
            SameSite = SameSiteMode.Strict
        };

        _httpContextAccessor.HttpContext.Response.Cookies.Append(key, value, cookieOptions);
    }

    public void SetCookie(string key, string value, DateTime? expireTime, bool isSecure, bool isHttpOnly)
    {
        var cookieOptions = new CookieOptions
        {
            Expires = expireTime ?? DateTime.UnixEpoch,
            Secure = isSecure,
            HttpOnly = isHttpOnly
        };

        _httpContextAccessor.HttpContext.Response.Cookies.Append(key, value, cookieOptions);
    }

    public void DeleteCookie(string key)
    {
        _httpContextAccessor.HttpContext.Response.Cookies.Delete(key);
    }

    public void DeleteCookies(IEnumerable<string> keys)
    {
        foreach (string key in keys)
        {
            DeleteCookie(key);
        }
    }

    public string GetUserIP()
    {
        return string.Empty;
    }

    public string GetUserCountry()
    {
        return string.Empty;
    }

    public string GetUserOS()
    {
        return string.Empty;
    }
}
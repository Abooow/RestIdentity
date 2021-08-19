using Microsoft.JSInterop;

namespace RestIdentity.Client.Services.Storage;

internal sealed class CookieStorage : ICookieStorage
{
    private const string GetCookieJsFuncName = "getCookie";
    private const string RemoveCookieJsFuncName = "removeCookie";
    private const string SetSessionCookieJsFuncName = "setSessionCookie";
    private const string SetExpirebleCookieJsFuncName = "setExpirebleCookie";

    private readonly IJSInProcessRuntime _jsInProcessRuntime;

    public CookieStorage(IJSInProcessRuntime jsInProcessRuntime)
    {
        _jsInProcessRuntime = jsInProcessRuntime;
    }

    public string GetCookie(string name)
    {
        return _jsInProcessRuntime.Invoke<string>(GetCookieJsFuncName, name);
    }

    public void RemoveCookie(string name)
    {
        _jsInProcessRuntime.InvokeVoid(RemoveCookieJsFuncName, name);
    }

    public void SetSessionCookie(string name, string value)
    {
        _jsInProcessRuntime.InvokeVoid(SetSessionCookieJsFuncName, name, value);
    }

    public void SetExpirebleCookie(string name, string value, DateTime expiryDate)

    {
        _jsInProcessRuntime.InvokeVoid(SetExpirebleCookieJsFuncName, name, value, expiryDate);
    }

}

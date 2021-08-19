namespace RestIdentity.Client.Services.Storage;

internal interface ICookieStorage
{
    string GetCookie(string name);
    void RemoveCookie(string name);
    void SetSessionCookie(string name, string value);
    void SetExpirebleCookie(string name, string value, DateTime expiryDate);
}

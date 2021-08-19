namespace RestIdentity.Client.Services.Storage;

internal interface ILocalStorage
{
    void SetItem(string key, string value);
    string GetItem(string key);
    void RemoveItem(string key);
}

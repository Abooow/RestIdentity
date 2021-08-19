using Microsoft.JSInterop;

namespace RestIdentity.Client.Services.Storage;

internal sealed class LocalStorage : ILocalStorage
{
    private const string SetItemJsFuncName = "localStorage.setItem";
    private const string GetItemJsFuncName = "localStorage.getItem";
    private const string RemoveItemJsFuncName = "localStorage.removeItem";

    private readonly IJSInProcessRuntime _jsInProcessRuntime;

    public LocalStorage(IJSInProcessRuntime jsInProcessRuntime)
    {
        _jsInProcessRuntime = jsInProcessRuntime;
    }

    public void SetItem(string key, string value)
    {
        _jsInProcessRuntime.InvokeVoid(SetItemJsFuncName, key, value);
    }

    public string GetItem(string key)
    {
        return _jsInProcessRuntime.Invoke<string>(GetItemJsFuncName, key);
    }

    public void RemoveItem(string key)
    {
        _jsInProcessRuntime.InvokeVoid(RemoveItemJsFuncName, key);
    }
}

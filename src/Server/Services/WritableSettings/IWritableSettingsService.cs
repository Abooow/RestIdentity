using Microsoft.Extensions.Options;

namespace RestIdentity.Server.Services;

internal interface IWritableSettingsService<out T> : IOptionsSnapshot<T> where T : class
{
    Task<bool> UpdateAsync(Action<T> changes);
}

using System;
using System.Text.Json;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Serilog;

namespace RestIdentity.Server.Services.WritableSettings;

internal sealed class WritableSettingsService<T> : IWritableSettingsService<T> where T : class, new()
{
    public T Value => _optionsMonitor.CurrentValue;

    private readonly IWebHostEnvironment _environment;
    private readonly IOptionsMonitor<T> _optionsMonitor;
    private readonly string _section;
    private readonly string _fileName;

    public WritableSettingsService(
        IWebHostEnvironment environment,
        IOptionsMonitor<T> optionsMonitor,
        string section,
        string fileName)
    {
        _environment = environment;
        _optionsMonitor = optionsMonitor;
        _section = section;
        _fileName = fileName;
    }

    public T Get(string name)
    {
        return _optionsMonitor.Get(name);
    }

    public async Task<bool> UpdateAsync(Action<T> changes)
    {
        bool resultError = false;
        try
        {
            IFileProvider fileProvider = _environment.ContentRootFileProvider;
            IFileInfo fileInfo = fileProvider.GetFileInfo(_fileName);
            string pysicalPath = fileInfo.PhysicalPath;

            JObject jsonObject = JsonConvert.DeserializeObject<JObject>(pysicalPath);
            T sectionObject = jsonObject.TryGetValue(_section, out JToken section)
                ? JsonConvert.DeserializeObject<T>(section.ToString())
                : Value ?? new T();

            changes?.Invoke(sectionObject);

            jsonObject[_section] = JObject.Parse(JsonConvert.SerializeObject(sectionObject));
            await File.WriteAllTextAsync(pysicalPath, JsonConvert.SerializeObject(jsonObject, Formatting.Indented));
        }
        catch (Exception e)
        {
            Log.Error("An error occurred while updating {File} settings file. {Error} {StackTrace} {InnerException} {Source}",
                _fileName, e.Message, e.StackTrace, e.InnerException, e.Source);

            resultError = true;
        }

        return resultError;
    }
}

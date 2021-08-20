using System.Text.Json;
using System.Text.Json.Serialization;

namespace RestIdentity.Client.Infrastructure.Extensions;

public static class ResultExtensions
{
    internal static async Task<Result> ToResult(this HttpResponseMessage response)
    {
        string responseAsString = await response.Content.ReadAsStringAsync();
        Result responseObject = JsonSerializer.Deserialize<Result>(responseAsString, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            ReferenceHandler = ReferenceHandler.Preserve
        })!;

        return responseObject;
    }

    internal static async Task<Result<T>> ToResult<T>(this HttpResponseMessage response)
    {
        string responseAsString = await response.Content.ReadAsStringAsync();
        Result<T> responseObject = JsonSerializer.Deserialize<Result<T>>(responseAsString, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            ReferenceHandler = ReferenceHandler.Preserve
        })!;

        return responseObject;
    }
}

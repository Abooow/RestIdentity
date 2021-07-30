using RestIdentity.Shared.Wrapper;
using System.Net.Http;
using System.Text.Json.Serialization;
using System.Text.Json;
using System.Threading.Tasks;

namespace RestIdentity.Client.Infrastructure.Extensions
{
    public static class ResultExtensions
    {
        internal static async Task<IResult> ToResult(this HttpResponseMessage response)
        {
            string responseAsString = await response.Content.ReadAsStringAsync();
            Result responseObject = JsonSerializer.Deserialize<Result>(responseAsString, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                ReferenceHandler = ReferenceHandler.Preserve
            })!;

            return responseObject;
        }

        internal static async Task<IResult<T>> ToResult<T>(this HttpResponseMessage response)
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
}

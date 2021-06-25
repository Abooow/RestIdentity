using System.Net;

namespace RestIdentity.Shared.Wrapper
{
    public static class ResultExtensions
    {
        public static IResult WithStatusCode(this IResult result, HttpStatusCode httpStatusCode)
        {
            result.StatusCode = httpStatusCode;
            return result;
        }

        public static IResult AsBadRequest(this IResult result)
        {
            return result.WithStatusCode(HttpStatusCode.BadRequest);
        }

        public static IResult AsNotFound(this IResult result)
        {
            return result.WithStatusCode(HttpStatusCode.NotFound);
        }
    }
}

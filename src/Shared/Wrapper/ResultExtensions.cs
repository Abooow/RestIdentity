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

        public static IResult AsUnauthorized(this IResult result)
        {
            return result.WithStatusCode(HttpStatusCode.Unauthorized);
        }

        public static IResult WithDescription(this IResult result, string description)
        {
            result.StatusCodeDescription = description;
            return result;
        }
    }
}

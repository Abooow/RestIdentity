using System.Net;

namespace RestIdentity.Shared.Wrapper;

public static class ResultExtensions
{
    public static Result WithStatusCode(this Result result, HttpStatusCode httpStatusCode)
    {
        return result with { StatusCode = httpStatusCode };
    }

    public static Result<T> WithStatusCode<T>(this Result<T> result, HttpStatusCode httpStatusCode)
    {
        return result with { StatusCode = httpStatusCode };
    }

    public static Result AsBadRequest(this Result result)
    {
        return result.WithStatusCode(HttpStatusCode.BadRequest);
    }

    public static Result<T> AsBadRequest<T>(this Result<T> result)
    {
        return result.WithStatusCode(HttpStatusCode.BadRequest);
    }

    public static Result AsNotFound(this Result result)
    {
        return result.WithStatusCode(HttpStatusCode.NotFound);
    }

    public static Result<T> AsNotFound<T>(this Result<T> result)
    {
        return result.WithStatusCode(HttpStatusCode.NotFound);
    }

    public static Result AsUnauthorized(this Result result)
    {
        return result.WithStatusCode(HttpStatusCode.Unauthorized);
    }

    public static Result<T> AsUnauthorized<T>(this Result<T> result)
    {
        return result.WithStatusCode(HttpStatusCode.Unauthorized);
    }

    public static Result WithDescription(this Result result, string description)
    {
        return result with { StatusCodeDescription = description };
    }

    public static Result<T> WithDescription<T>(this Result<T> result, string description)
    {
        return result with { StatusCodeDescription = description };
    }
}

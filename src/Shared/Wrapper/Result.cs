using System.Net;

namespace RestIdentity.Shared.Wrapper;

public record Result(bool Succeeded, HttpStatusCode StatusCode, string StatusCodeDescription, IEnumerable<string> Messages)
{
    private static readonly Result successResult = new Result(true, HttpStatusCode.OK, StatusCodeDescriptions.None, Array.Empty<string>());
    private static readonly Result failResult = new Result(false, HttpStatusCode.BadRequest, StatusCodeDescriptions.None, Array.Empty<string>());

    public static Result Success()
    {
        return successResult;
    }

    public static Result Success(string message)
    {
        return successResult with { Messages = new string[] { message } };
    }

    public static Result Fail()
    {
        return failResult;
    }

    public static Result Fail(string message)
    {
        return failResult with { Messages = new string[] { message } };
    }

    public static Result Fail(IEnumerable<string> messages)
    {
        return failResult with { Messages = messages };
    }
}

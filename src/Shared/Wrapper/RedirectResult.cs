using System.Net;

namespace RestIdentity.Shared.Wrapper;

public class RedirectResult : Result, IRedirectResult
{
    public RedirectResult()
        : base()
    {
        StatusCode = HttpStatusCode.Redirect;
    }

    public string Url { get; set; }

    public static IRedirectResult RedirectTo(string url)
    {
        return new RedirectResult() { Url = url };
    }

    public static IRedirectResult RedirectTo(string url, string message)
    {
        return new RedirectResult() { Url = url, Messages = new string[] { message } };
    }
}

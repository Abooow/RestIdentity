using System.Collections.Generic;
using System.Net;

namespace RestIdentity.Shared.Wrapper
{
    public interface IResult
    {
        bool Succeeded { get; set; }
        HttpStatusCode StatusCode { get; set; }
        IEnumerable<string> Messages { get; set; }
    }

    public interface IResult<out T> : IResult
    {
        T Data { get; }
    }

    public interface IRedirectResult : IResult
    {
        string Url { get; }
    }
}

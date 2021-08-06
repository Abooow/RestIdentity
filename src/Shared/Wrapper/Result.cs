using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;

namespace RestIdentity.Shared.Wrapper
{
    public class Result : IResult
    {
        public Result()
        {
            StatusCode = HttpStatusCode.OK;
            StatusCodeDescription = StatusCodeDescriptions.None;
            Messages = Array.Empty<string>();
        }

        public bool Succeeded { get; set; }

        public HttpStatusCode StatusCode { get; set; }

        public string StatusCodeDescription { get; set; }

        public IEnumerable<string> Messages { get; set; }

        public static IResult Fail()
        {
            return new Result { Succeeded = false };
        }

        public static IResult Fail(string message)
        {
            return new Result { Succeeded = false, Messages = new string[] { message } };
        }

        public static IResult Fail(IEnumerable<string> messages)
        {
            return new Result { Succeeded = false, Messages = messages };
        }

        public static Task<IResult> FailAsync()
        {
            return Task.FromResult(Fail());
        }

        public static Task<IResult> FailAsync(string message)
        {
            return Task.FromResult(Fail(message));
        }

        public static Task<IResult> FailAsync(IEnumerable<string> messages)
        {
            return Task.FromResult(Fail(messages));
        }

        public static IResult Success()
        {
            return new Result { Succeeded = true };
        }

        public static IResult Success(string message)
        {
            return new Result { Succeeded = true, Messages = new string[] { message } };
        }

        public static Task<IResult> SuccessAsync()
        {
            return Task.FromResult(Success());
        }

        public static Task<IResult> SuccessAsync(string message)
        {
            return Task.FromResult(Success(message));
        }
    }
}

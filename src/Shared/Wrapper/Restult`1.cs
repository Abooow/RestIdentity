using System.Collections.Generic;
using System.Threading.Tasks;

namespace RestIdentity.Shared.Wrapper
{
    public class Result<T> : Result, IResult<T>
    {
        public Result()
            : base()
        {
        }

        public T Data { get; set; }

        public new static Result<T> Fail()
        {
            return new Result<T> { Succeeded = false };
        }

        public new static Result<T> Fail(string message)
        {
            return new Result<T> { Succeeded = false, Messages = new string[] { message } };
        }

        public new static Result<T> Fail(IEnumerable<string> messages)
        {
            return new Result<T> { Succeeded = false, Messages = messages };
        }

        public new static Task<Result<T>> FailAsync()
        {
            return Task.FromResult(Fail());
        }

        public new static Task<Result<T>> FailAsync(string message)
        {
            return Task.FromResult(Fail(message));
        }

        public new static Task<Result<T>> FailAsync(IEnumerable<string> messages)
        {
            return Task.FromResult(Fail(messages));
        }

        public new static Result<T> Success()
        {
            return new Result<T> { Succeeded = true };
        }

        public new static Result<T> Success(string message)
        {
            return new Result<T> { Succeeded = true, Messages = new string[] { message } };
        }

        public static Result<T> Success(T data)
        {
            return new Result<T> { Succeeded = true, Data = data };
        }

        public static Result<T> Success(T data, string message)
        {
            return new Result<T> { Succeeded = true, Data = data, Messages = new string[] { message } };
        }

        public static Result<T> Success(T data, IEnumerable<string> messages)
        {
            return new Result<T> { Succeeded = true, Data = data, Messages = messages };
        }

        public new static Task<Result<T>> SuccessAsync()
        {
            return Task.FromResult(Success());
        }

        public new static Task<Result<T>> SuccessAsync(string message)
        {
            return Task.FromResult(Success(message));
        }

        public static Task<Result<T>> SuccessAsync(T data)
        {
            return Task.FromResult(Success(data));
        }

        public static Task<Result<T>> SuccessAsync(T data, string message)
        {
            return Task.FromResult(Success(data, message));
        }
    }
}

// minimal endpoint https://youtu.be/gsAuFIhXz3g?si=MfaGxzKFgLlgWIbR
// reflection endpoint mapping https://youtu.be/CkGFV5bekbY?si=GkVIYuPIObrZDMu1
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
using static MongoDB.Driver.WriteConcern;

namespace MMIv8_Ktype.Models
{
    public interface IResponse { }



    public class Result : IResultWrapper
    {
        protected internal Result(bool isSuccess, Error error)
        {
            if (isSuccess && error != Error.None ||
                !isSuccess && error == Error.None)
            {
                throw new ArgumentException("Invalid error", nameof(error));
            }

            IsSuccess = isSuccess;
            Error = error == Error.None ? null : error;
        }

        public bool IsSuccess { get; }

        public Error? Error { get; }

        public static Result Success() => new(true, Error.None);

        public static Result<TValue> Success<TValue>(TValue value) => new(value, true, Error.None);

        public static Result Failure(Error error) => new(false, error);

        public static Result<TValue> Failure<TValue>(Error error) => new(default, false, error);

        public static implicit operator Result(Error error) =>
            Failure(error);
    }

    public class Result<TValue> : Result
    {
        private readonly TValue? _value;

        protected internal Result(TValue? value, bool isSuccess, Error error)
            : base(isSuccess, error)
        {
            _value = value;
        }

        [NotNull]
        public TValue Value => IsSuccess
            ? _value!
            : throw new InvalidOperationException("The value of a failure result can't be accessed.");

        public static implicit operator Result<TValue>(TValue value) =>
            Success(value);

        public static implicit operator Result<TValue>(Error error) =>
            Failure<TValue>(error);

    }

    public interface IResultWrapper
    {
        bool IsSuccess { get; }
        Error? Error { get; }
    }

    public sealed class SerializableResult<T> : IResultWrapper
    {
        public required bool IsSuccess { get; init; }
        public T? Value { get; init; }
        public Error? Error { get; init; }

        public static implicit operator SerializableResult<T>(Result<T> result) => new()
        {
            IsSuccess = result.IsSuccess,
            Value = result.IsSuccess ? result.Value : default,
            Error = result.Error
        };

        public static implicit operator Result<T>(SerializableResult<T> result) => 
            result.IsSuccess ? Result.Success(result.Value!) : Result.Failure<T>(result.Error!);

        public Result<T> ToResult() => this;
    }

    public record Error(ErrorType Type, string Code, string Description)
    {
        public static readonly Error None = new(ErrorType.None, string.Empty, string.Empty);

        public static Error Failure(string Code, string description) => new(ErrorType.Failure, Code, description);
        public static Error Validation(string Code, string description) => new(ErrorType.Validation, Code, description);
        public static Error NotFound(string Code, string description) => new(ErrorType.NotFound, Code, description);
        public static Error NoContent(string Code, string description) => new(ErrorType.NoContent, Code, description);
        public static Error Conflict(string Code, string description) => new(ErrorType.Conflict, Code, description);
    }

    public enum ErrorType
    {
        None = -1,
        Failure,
        Validation,
        NotFound,
        NoContent,
        Conflict,
    }


    public static class ResultExtensions
    {
        public static IResult ToProblemDetails(this IResultWrapper result)
        {
            if (result.IsSuccess)
            {
                throw new InvalidOperationException("Can't convert success result to problem");
            }

            return Results.Problem(
                statusCode: result.Error?.Type.GetStatusCode(),
                title: result.Error?.Type.GetTitle(),
                type: result.Error?.Type.GetProblemDetailsType(),
                extensions: new Dictionary<string, object?>
                {
                    { "errors", new[] { result.Error } }
                });
        }

        private static int GetStatusCode(this ErrorType errorType) => errorType switch
        {
            ErrorType.None => throw new NotImplementedException(),
            ErrorType.Failure => StatusCodes.Status500InternalServerError,
            ErrorType.Validation => StatusCodes.Status400BadRequest,
            ErrorType.NotFound => StatusCodes.Status404NotFound,
            ErrorType.NoContent => StatusCodes.Status204NoContent,
            ErrorType.Conflict => StatusCodes.Status409Conflict,
            _ => throw new NotImplementedException(),
        };

        private static string GetTitle(this ErrorType errorType) => errorType switch
        {
            ErrorType.None => throw new NotImplementedException(),
            ErrorType.Validation => "Bad Request",
            ErrorType.NotFound => "Not Found",
            ErrorType.NoContent => "No Content",
            ErrorType.Conflict => "Conflict",
            _ => "Server Failure",
        };

        private static string GetProblemDetailsType(this ErrorType errorType) => errorType switch
        {
            ErrorType.None => throw new NotImplementedException(),
            ErrorType.Validation => "https://datatracker.ietf.org/doc/html/rfc7231#section-6.5.1",
            ErrorType.NotFound => "https://datatracker.ietf.org/doc/html/rfc7231#section-6.5.4",
            ErrorType.NoContent => "https://datatracker.ietf.org/doc/html/rfc7231#section-6.3.5",
            ErrorType.Conflict => "https://datatracker.ietf.org/doc/html/rfc7231#section-6.5.8",
            _ => "https://datatracker.ietf.org/doc/html/rfc7231#section-6.6.1",
        };
    }
}

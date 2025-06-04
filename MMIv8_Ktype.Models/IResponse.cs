// minimal endpoint https://youtu.be/gsAuFIhXz3g?si=MfaGxzKFgLlgWIbR
// reflection endpoint mapping https://youtu.be/CkGFV5bekbY?si=GkVIYuPIObrZDMu1
using Microsoft.AspNetCore.Http;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using static MongoDB.Driver.WriteConcern;

namespace MMIv8_Ktype.Models
{
    public interface IResponse { }



    public class Result
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

        public static implicit operator Result<TValue>(TValue? value) =>
            value is not null ? Success(value) : Failure<TValue>(Error.NotFound);

        public static implicit operator Result<TValue>(Error error) =>
            Failure<TValue>(error);
    }

    public sealed class SerializableResult<T>
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
    }

    public record Error(ErrorType Type, string Code, string Description)
    {
        public static readonly Error None = new(ErrorType.None, string.Empty, string.Empty);
        public static readonly Error NullValue = new(ErrorType.Validation, "Error.NullValue", "Null value was provided");
        public static readonly Error NotFound = new(ErrorType.NotFound, "Error.NotFound", "Value was not found");


        public static Error CannotFindDocument(Type documentType, string? description = null) => new(ErrorType.NotFound, $"{documentType.Name}.NotFound", description ?? "Cannot find document");
        public static Error CannotCreateDocument(Type documentType, string? description = null) => new(ErrorType.Failure, $"{documentType.Name}.CreationError", description ?? "Could not create document");
    }

    public enum ErrorType
    {
        None = -1,
        Failure = 1,
        Validation = 2,
        NotFound = 3,
        Conflict = 4
    }


    public static class ResultExtensions
    {
        public static IResult ToProblemDetails(this Result result)
        {
            if (result.IsSuccess)
            {
                throw new InvalidOperationException("Can't convert success result to problem");
            }

            return Results.Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Bad Request",
                type: "https://tools.ietf.org/html/rfc7231#section-6.5.1",
                extensions: new Dictionary<string, object?>
                {
                    { "errors", new[] { result.Error } }
                });
        }
    }
}

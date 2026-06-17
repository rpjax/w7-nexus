using System.Text.Json.Serialization;
using Aidan.Core.Errors;
using Aidan.Core.Patterns;

namespace Nexus.Authorizations.Application.Models;

public sealed class OperationResult<T> : IOperationResult<T>
{
    public bool IsSuccess { get; init; }
    public IReadOnlyList<Error> Errors { get; init; } = Array.Empty<Error>();
    public T? Value { get; init; }
    public bool IsAuthorized { get; init; }
    public IReadOnlyList<Error> AuthorizationErrors { get; init; } = Array.Empty<Error>();

    [JsonIgnore]
    public bool IsFailure => !IsSuccess;

    T IResult<T>.Value => Value ?? throw new InvalidOperationException("Cannot access Value on a failed operation result.");

    public static OperationResult<T> Unauthorized(Error error)
        => Unauthorized([error]);

    public static OperationResult<T> Unauthorized(IEnumerable<Error> authorizationErrors)
        => new()
        {
            IsSuccess = false,
            IsAuthorized = false,
            AuthorizationErrors = authorizationErrors.ToArray(),
        };

    public static OperationResult<T> Failure(Error error)
        => Failure([error]);

    public static OperationResult<T> Failure(IEnumerable<Error> errors)
        => new()
        {
            IsSuccess = false,
            IsAuthorized = true,
            Errors = errors.ToArray(),
        };

    public static OperationResult<T> Success(T value)
        => new()
        {
            IsSuccess = true,
            IsAuthorized = true,
            Value = value,
        };
}

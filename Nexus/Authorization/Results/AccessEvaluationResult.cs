using System.Text.Json.Serialization;
using Aidan.Core.Errors;

namespace Nexus.Authorization.Results;

public class AccessEvaluationResult<T> : IAccessEvaluationResult<T> where T : class
{
    public bool IsSuccess { get; init; }
    public IReadOnlyList<Error> Errors { get; init; }
    public bool IsAuthorized { get; }
    public IReadOnlyList<Error> AuthorizationErrors { get; init; }
    public T Role { get; }

    [JsonIgnore]
    public bool IsFailure => !IsSuccess;

    public static AccessEvaluationResult<T> Failure(Error error)
    {
        return new AccessEvaluationResult<T>(error);
    }

    public static AccessEvaluationResult<T> Failure(IEnumerable<Error> errors)
    {
        return new AccessEvaluationResult<T>(errors);
    }

    public static AccessEvaluationResult<T> Authorized(T role)
    {
        return new AccessEvaluationResult<T>(true, role, []);
    }

    public static AccessEvaluationResult<T> Unauthorized()
    {
        return new AccessEvaluationResult<T>(false, default!, []);
    }

    public static AccessEvaluationResult<T> Unauthorized(Error error)
    {
        return new AccessEvaluationResult<T>(false, default!, [error]);
    }

    public static AccessEvaluationResult<T> Unauthorized(IEnumerable<Error> errors)
    {
        return new AccessEvaluationResult<T>(false, default!, errors);
    }

    private AccessEvaluationResult(Error error)
    {
        IsSuccess = false;
        Errors = new Error[1] { error };
        IsAuthorized = false;
        AuthorizationErrors = Array.Empty<Error>();
        Role = default!;
    }

    private AccessEvaluationResult(IEnumerable<Error> errors)
    {
        IsSuccess = false;
        Errors = errors.ToArray();
        if (Errors.Count == 0)
        {
            throw new ArgumentException("At least one error must be provided for a failed operation result.", "errors");
        }
        IsAuthorized = false;
        AuthorizationErrors = Array.Empty<Error>();
        Role = default!;
    }

    private AccessEvaluationResult(bool isAuthorized, T role, IEnumerable<Error> errors)
    {
        IsSuccess = true;
        Errors = Array.Empty<Error>();
        IsAuthorized = isAuthorized;
        AuthorizationErrors = errors.ToArray();
        Role = role ?? default!;

        if (isAuthorized && Role is null)
        {
            throw new ArgumentException("Role cannot be null if the access is authorized.", "role");
        }
    }
}

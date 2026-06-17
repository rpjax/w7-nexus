using System.Text.Json.Serialization;
using Aidan.Core.Errors;

namespace Nexus.Authorizations.Application.Models;

public sealed class AuthorizationResult : IAuthorizationResult
{
    public bool IsSuccess { get; init; }
    public IReadOnlyList<Error> Errors { get; init; }
    public bool IsAuthorized { get; }
    public IReadOnlyList<Error> AuthorizationErrors { get; init; }

    [JsonIgnore]
    public bool IsFailure => !IsSuccess;

    public static AuthorizationResult Failure(Error error)
        => new(error);

    public static AuthorizationResult Failure(IEnumerable<Error> errors)
        => new(errors);

    public static AuthorizationResult Authorized()
        => new(true, []);

    public static AuthorizationResult Unauthorized(Error error)
        => new(false, [error]);

    public static AuthorizationResult Unauthorized(IEnumerable<Error> errors)
        => new(false, errors);

    private AuthorizationResult(Error error)
    {
        IsSuccess = false;
        Errors = new Error[1] { error };
        IsAuthorized = false;
        AuthorizationErrors = Array.Empty<Error>();
    }

    private AuthorizationResult(IEnumerable<Error> errors)
    {
        IsSuccess = false;
        Errors = errors.ToArray();
        if (Errors.Count == 0)
        {
            throw new ArgumentException(
                "At least one error must be provided for a failed operation result.",
                nameof(errors));
        }

        IsAuthorized = false;
        AuthorizationErrors = Array.Empty<Error>();
    }

    private AuthorizationResult(bool isAuthorized, IEnumerable<Error> authorizationErrors)
    {
        IsSuccess = true;
        Errors = Array.Empty<Error>();
        IsAuthorized = isAuthorized;
        AuthorizationErrors = authorizationErrors.ToArray();
    }
}

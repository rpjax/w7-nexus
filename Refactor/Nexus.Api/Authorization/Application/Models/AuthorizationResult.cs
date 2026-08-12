using System.Text.Json.Serialization;
using Aidan.Core.Errors;

namespace Refactor.Nexus.Api.Authorization.Application.Models;

public sealed class AuthorizationResult : IAuthorizationResult
{
    public bool IsSuccess { get; init; }
    public IReadOnlyList<Error> Errors { get; init; } = Array.Empty<Error>();
    public bool IsAuthorized { get; init; }
    public IReadOnlyList<Error> AuthorizationErrors { get; init; } = Array.Empty<Error>();

    [JsonIgnore]
    public bool IsFailure => !IsSuccess;

    public static AuthorizationResult Failure(Error error) => Failure([error]);

    public static AuthorizationResult Failure(IEnumerable<Error> errors) =>
        new()
        {
            IsSuccess = false,
            IsAuthorized = false,
            Errors = errors.ToArray()
        };

    public static AuthorizationResult Authorized() =>
        new()
        {
            IsSuccess = true,
            IsAuthorized = true
        };

    public static AuthorizationResult Unauthorized(Error error) => Unauthorized([error]);

    public static AuthorizationResult Unauthorized(IEnumerable<Error> errors) =>
        new()
        {
            IsSuccess = false,
            IsAuthorized = false,
            AuthorizationErrors = errors.ToArray()
        };
}

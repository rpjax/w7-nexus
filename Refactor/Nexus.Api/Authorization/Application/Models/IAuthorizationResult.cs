using Aidan.Core.Errors;
using Aidan.Core.Patterns;
using IResult = Aidan.Core.Patterns.IResult;

namespace Refactor.Nexus.Api.Authorization.Application.Models;

public interface IAuthorizationResult : IResult
{
    bool IsAuthorized { get; }
    IReadOnlyList<Error> AuthorizationErrors { get; }
}

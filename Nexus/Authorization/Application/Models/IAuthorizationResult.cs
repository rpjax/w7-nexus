using Aidan.Core.Errors;
using Aidan.Core.Patterns;

namespace Nexus.Authorization.Application.Models;

public interface IAuthorizationResult : IResult
{
    bool IsAuthorized { get; }
    IReadOnlyList<Error> AuthorizationErrors { get; }
}

using Aidan.Core.Errors;
using Aidan.Core.Patterns;

namespace Refactor.Nexus.Api.Authorization.Application.Models;

public interface IOperationResult<T> : IResult<T>
{
    bool IsAuthorized { get; }
    IReadOnlyList<Error> AuthorizationErrors { get; }
}

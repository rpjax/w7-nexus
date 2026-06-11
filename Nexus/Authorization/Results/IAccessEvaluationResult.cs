using Aidan.Core.Errors;
using Aidan.Core.Patterns;

namespace Nexus.Authorization.Results;

public interface IAccessEvaluationResult<T> : IResult where T : class
{
    bool IsAuthorized { get; }
    IReadOnlyList<Error> AuthorizationErrors { get; }
    T Role { get; }
}

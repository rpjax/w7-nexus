using Aidan.Core.Patterns;
using Nexus.Actors.Requests;
using Nexus.Actors.Responses;

namespace Nexus.Actors.Contracts;

public interface IOperator
{
    Task<IResult<SearchOperationsResponse>> SearchOperationsAsync(
        SearchOperatorOperationsRequest request);
}

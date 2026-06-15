using Aidan.Core.Patterns;
using Nexus.Operator.Application.Requests;
using Nexus.Operator.Application.Responses;

namespace Nexus.Operator.Application.Contracts;

public interface IOperator
{
    Task<IResult<SearchOperationsResponse>> SearchOperationsAsync(
        SearchOperatorOperationsRequest request);
}

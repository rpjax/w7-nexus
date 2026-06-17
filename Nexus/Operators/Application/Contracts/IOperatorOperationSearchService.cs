using Aidan.Core.Patterns;
using Nexus.Authorizations.Application.Models;
using Nexus.Operators.Application.Requests;
using Nexus.Operators.Application.Responses;

namespace Nexus.Operators.Application.Contracts;

public interface IOperatorOperationSearchService
{
    Task<IResult<SearchOperationsResponse>> SearchOperationsAsync(
        RequesterIdentity identity,
        SearchOperationsRequest request);
}

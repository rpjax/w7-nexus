using Aidan.Core.Patterns;
using Nexus.Operations.Application.Requests.OperationAdministrator;
using Nexus.Operations.Application.Responses.OperationAdministrator;

namespace Nexus.Operations.Application.Contracts;

public interface IOperationAdministratorAccountSearchService
{
    Task<IResult<SearchAccountsResponse>> SearchAccountsAsync(SearchAccountsRequest request);
}

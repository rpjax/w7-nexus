using Aidan.Core.Patterns;
using Nexus.OperationAdministrator.Application.Requests;
using Nexus.OperationAdministrator.Application.Responses;

namespace Nexus.OperationAdministrator.Application.Contracts;

public interface IOperationAdministratorAccountSearchService
{
    Task<IResult<SearchAccountsResponse>> SearchAccountsAsync(SearchAccountsRequest request);
}

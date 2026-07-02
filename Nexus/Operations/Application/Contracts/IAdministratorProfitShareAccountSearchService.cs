using Aidan.Core.Patterns;
using Nexus.Operations.Application.Requests.Administrator;
using Nexus.Operations.Application.Responses.Administrator;

namespace Nexus.Operations.Application.Contracts;

public interface IAdministratorProfitShareAccountSearchService
{
    Task<IResult<SearchProfitShareAccountsToAssignResponse>> SearchProfitShareAccountsToAssignAsync(
        SearchProfitShareAccountsToAssignRequest request);
}

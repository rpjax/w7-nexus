using Aidan.Core.Patterns;
using Nexus.Accounts.Application.Requests.Administrator;
using Nexus.Accounts.Application.Responses.Administrator;

namespace Nexus.Accounts.Application.Contracts;

public interface IAdministratorAccountSearchService
{
    Task<IResult<SearchAccountsResponse>> SearchAccountsAsync(SearchAccountsRequest request);
}

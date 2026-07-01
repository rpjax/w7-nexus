using Aidan.Core.Patterns;
using Nexus.Olx.Application.Requests.Administrator;
using Nexus.Olx.Application.Responses.Administrator;

namespace Nexus.Olx.Application.Contracts;

public interface IOlxAdministratorAdSpoofSearchService
{
    Task<IResult<SearchAdSpoofsResponse>> SearchAdSpoofsAsync(SearchAdSpoofsRequest request);
}

using Aidan.Core.Patterns;
using Nexus.Olx.Application.Requests.Administrator;
using Nexus.Olx.Application.Responses.Administrator;

namespace Nexus.Olx.Application.Contracts;

public interface IOlxAdministratorAdPatchSearchService
{
    Task<IResult<SearchAdPatchesResponse>> SearchAdPatchesAsync(SearchAdPatchesRequest request);
}

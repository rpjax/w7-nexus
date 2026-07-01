using Aidan.Core.Patterns;
using Nexus.Authorization.Application.Models;
using Nexus.Olx.Application.Requests.Operator;
using Nexus.Olx.Application.Responses.Operator;

namespace Nexus.Olx.Application.Contracts;

public interface IOlxOperatorAdPatchSearchService
{
    Task<IResult<SearchAdPatchesResponse>> SearchAdPatchesAsync(
        RequesterIdentity identity,
        SearchAdPatchesRequest request);
}

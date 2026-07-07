using Aidan.Core.Patterns;
using Nexus.Authorization.Application.Models;
using Nexus.Olx.Application.Requests;
using Nexus.Olx.Application.Requests.Administrator;
using Nexus.Olx.Application.Responses;
using Nexus.Olx.Application.Responses.Administrator;

namespace Nexus.Olx.Application.Contracts;

public interface IOlxAdministrator
{
    Task<IOperationResult<SearchAdPatchesResponse>> SearchAdPatchesAsync(
        RequesterIdentity identity,
        SearchAdPatchesRequest request,
        CancellationToken cancellationToken = default);

    Task<IOperationResult<UnimpersonateAdResponse>> UnimpersonateAdAsync(
        RequesterIdentity identity,
        UnimpersonateAdRequest request,
        CancellationToken cancellationToken = default);
}

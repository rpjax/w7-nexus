using Aidan.Core.Patterns;
using Nexus.Authorization.Application.Models;
using Nexus.Olx.Application.Requests;
using Nexus.Olx.Application.Requests.Operator;
using Nexus.Olx.Application.Responses;
using Nexus.Olx.Application.Responses.Operator;

namespace Nexus.Olx.Application.Contracts;

public interface IOlxOperator
{
    Task<IOperationResult<SearchAdPatchesResponse>> SearchAdPatchesAsync(
        RequesterIdentity identity,
        SearchAdPatchesRequest request,
        CancellationToken cancellationToken = default);

    Task<IOperationResult<ImpersonateAdResponse>> ImpersonateAdAsync(
        RequesterIdentity identity,
        ImpersonateAdRequest request,
        CancellationToken cancellationToken = default);

    Task<IOperationResult<UnimpersonateAdResponse>> UnimpersonateAdAsync(
        RequesterIdentity identity,
        UnimpersonateAdRequest request,
        CancellationToken cancellationToken = default);

    Task<IOperationResult<UpdateAdDetailsPatchResponse>> UpdateAdDetailsPatchAsync(
        RequesterIdentity identity,
        UpdateAdDetailsPatchRequest request,
        CancellationToken cancellationToken = default);
}

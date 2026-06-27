using Aidan.Core.Patterns;
using Nexus.Authorization.Application.Models;
using Nexus.Olx.Application.Requests;
using Nexus.Olx.Application.Requests.Operator;
using Nexus.Olx.Application.Responses;
using Nexus.Olx.Application.Responses.Operator;

namespace Nexus.Olx.Application.Contracts;

public interface IOlxOperator
{
    Task<IOperationResult<SearchAdSpoofsResponse>> SearchAdSpoofsAsync(
        RequesterIdentity identity,
        SearchAdSpoofsRequest request,
        CancellationToken cancellationToken = default);

    Task<IOperationResult<ImpersonateAdResponse>> ImpersonateAdAsync(
        RequesterIdentity identity,
        ImpersonateAdRequest request,
        CancellationToken cancellationToken = default);

    Task<IOperationResult<UnimpersonateAdResponse>> UnimpersonateAdAsync(
        RequesterIdentity identity,
        UnimpersonateAdRequest request,
        CancellationToken cancellationToken = default);

    Task<IOperationResult<UpdateAdDetailsSpoofResponse>> UpdateAdDetailsSpoofAsync(
        RequesterIdentity identity,
        UpdateAdDetailsSpoofRequest request,
        CancellationToken cancellationToken = default);
}

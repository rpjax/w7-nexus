using Aidan.Core.Patterns;
using Nexus.Authorization.Application.Models;
using Nexus.Olx.Application.Requests;

namespace Nexus.Olx.Application.Contracts;

public interface IOlxService
{
    Task<IResult> ImpersonateAdAsync(
        RequesterIdentity identity,
        ImpersonateAdRequest request,
        CancellationToken? cancellationToken = default);

    Task<IResult> UnimpersonateAdAsync(
        RequesterIdentity identity,
        UnimpersonateAdRequest request,
        CancellationToken? cancellationToken = default);

    Task<IResult> UpdateAdDetailsSpoofAsync(
        RequesterIdentity identity,
        UpdateAdDetailsSpoofRequest request,
        CancellationToken? cancellationToken = default);
}


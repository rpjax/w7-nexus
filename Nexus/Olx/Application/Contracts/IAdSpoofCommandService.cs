using Aidan.Core.Patterns;
using Nexus.Olx.Application.Requests;
using Nexus.Olx.Application.Responses;

namespace Nexus.Olx.Application.Contracts;

public interface IAdSpoofCommandService
{
    Task<IResult<ImpersonateAdResponse>> ImpersonateAdAsync(
        string requesterAccountId,
        ImpersonateAdRequest request,
        bool requireSelfOperator,
        CancellationToken cancellationToken = default);

    Task<IResult<UnimpersonateAdResponse>> UnimpersonateAdAsync(
        string requesterAccountId,
        UnimpersonateAdRequest request,
        bool requireSelfOperator,
        CancellationToken cancellationToken = default);

    Task<IResult<UpdateAdDetailsSpoofResponse>> UpdateAdDetailsSpoofAsync(
        string requesterAccountId,
        UpdateAdDetailsSpoofRequest request,
        CancellationToken cancellationToken = default);
}

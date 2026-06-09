using Aidan.Core.Patterns;
using Nexus.Actors.Requests;
using Nexus.Actors.Responses;

namespace Nexus.Actors.Contracts;

public interface ITeamOperator
{
    Task<IResult<CreateTeamPixPaymentResponse>> CreateTeamPixPaymentAsync(
        CreateTeamPixPaymentRequest request);
}

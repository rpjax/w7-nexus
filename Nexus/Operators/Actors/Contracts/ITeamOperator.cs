using Aidan.Core.Patterns;
using Nexus.Operators.Actors.Requests;
using Nexus.Operators.Actors.Responses;

namespace Nexus.Operators.Actors.Contracts;

public interface ITeamOperator
{
    Task<IResult<CreateTeamPixPaymentResponse>> CreateTeamPixPaymentAsync(
        CreateTeamPixPaymentRequest request);
}

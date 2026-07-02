using Aidan.Core.Patterns;
using Nexus.Operations.Application.Requests.TeamLeader;
using Nexus.Operations.Application.Responses.TeamLeader;

namespace Nexus.Operations.Application.Contracts;

public interface ITeamLeaderTeamCommandService
{
    Task<IResult<AssignOperatorToTeamResponse>> AssignOperatorToTeamAsync(AssignOperatorToTeamRequest request);

    Task<IResult<UnassignOperatorFromTeamResponse>> UnassignOperatorFromTeamAsync(
        UnassignOperatorFromTeamRequest request);

    Task<IResult<SetOperatorProfitShareRuleResponse>> SetOperatorProfitShareRuleAsync(
        SetOperatorProfitShareRuleRequest request);
}

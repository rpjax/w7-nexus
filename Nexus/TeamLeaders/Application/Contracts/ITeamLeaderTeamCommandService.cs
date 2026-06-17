using Aidan.Core.Patterns;
using Nexus.TeamLeaders.Application.Requests;
using Nexus.TeamLeaders.Application.Responses;

namespace Nexus.TeamLeaders.Application.Contracts;

public interface ITeamLeaderTeamCommandService
{
    Task<IResult<AssignOperatorToTeamResponse>> AssignOperatorToTeamAsync(AssignOperatorToTeamRequest request);

    Task<IResult<UnassignOperatorFromTeamResponse>> UnassignOperatorFromTeamAsync(
        UnassignOperatorFromTeamRequest request);

    Task<IResult<SetOperatorProfitShareRuleResponse>> SetOperatorProfitShareRuleAsync(
        SetOperatorProfitShareRuleRequest request);
}

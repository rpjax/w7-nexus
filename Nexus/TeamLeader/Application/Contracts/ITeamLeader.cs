using Aidan.Core.Patterns;
using Nexus.TeamLeader.Application.Requests;
using Nexus.TeamLeader.Application.Responses;

namespace Nexus.TeamLeader.Application.Contracts;

public interface ITeamLeader
{
    Task<IResult<SearchLedTeamsResponse>> SearchLedTeamsAsync(SearchLedTeamsRequest request);

    Task<IResult<AssignOperatorToTeamResponse>> AssignOperatorToTeamAsync(
        AssignOperatorToTeamRequest request);

    Task<IResult<UnassignOperatorFromTeamResponse>> UnassignOperatorFromTeamAsync(
        UnassignOperatorFromTeamRequest request);

    Task<IResult<SetOperatorProfitShareRuleResponse>> SetOperatorProfitShareRuleAsync(
        SetOperatorProfitShareRuleRequest request);
}

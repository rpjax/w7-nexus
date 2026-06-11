using Aidan.Core.Patterns;
using Nexus.Actors.Requests;
using Nexus.Actors.Responses;

namespace Nexus.Actors.Contracts;

public interface ITeamLeader
{
    Task<IResult<AssignOperatorToTeamResponse>> AssignOperatorToTeamAsync(
        AssignOperatorToTeamRequest request);

    Task<IResult<UnassignOperatorFromTeamResponse>> UnassignOperatorFromTeamAsync(
        UnassignOperatorFromTeamRequest request);

    Task<IResult<SetTeamGatewaySelectionStrategyResponse>> SetTeamGatewaySelectionStrategyAsync(
        SetTeamGatewaySelectionStrategyRequest request);

    Task<IResult<AssignStrawManToTeamResponse>> AssignStrawManToTeamAsync(
        AssignStrawManToTeamRequest request);

    Task<IResult<UnassignStrawManFromTeamResponse>> UnassignStrawManFromTeamAsync(
        UnassignStrawManFromTeamRequest request);

    Task<IResult<AssignGatewayAccountGroupToTeamResponse>> AssignGatewayAccountGroupToTeamAsync(
        AssignGatewayAccountGroupToTeamRequest request);

    Task<IResult<UnassignGatewayAccountGroupFromTeamResponse>> UnassignGatewayAccountGroupFromTeamAsync(
        UnassignGatewayAccountGroupFromTeamRequest request);

    Task<IResult<AssignGatewayAccountToTeamResponse>> AssignGatewayAccountToTeamAsync(
        AssignGatewayAccountToTeamRequest request);

    Task<IResult<UnassignGatewayAccountFromTeamResponse>> UnassignGatewayAccountFromTeamAsync(
        UnassignGatewayAccountFromTeamRequest request);

    Task<IResult<SetOperatorProfitShareRuleResponse>> SetOperatorProfitShareRuleAsync(
        SetOperatorProfitShareRuleRequest request);
}

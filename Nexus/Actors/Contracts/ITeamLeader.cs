using Aidan.Core.Patterns;
using Nexus.Actors.Requests;
using Nexus.Actors.Responses;

namespace Nexus.Actors.Contracts;

// A team is a contexto to set their own gateway selection strategy and profit sharing in an independent way of the operation the team belongs to
public interface ITeamLeader
{
    // operator management
    Task<IResult<AssignOperatorToTeamResponse>> AssignOperatorToTeamAsync(
        AssignOperatorToTeamRequest request);
    Task<IResult<UnassignOperatorFromTeamResponse>> UnassignOperatorFromTeamAsync(
        UnassignOperatorFromTeamRequest request);

    // per strawman, per group, manual
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

    // set profit share strategy
    Task<IResult<SetTeamProfitShareStrategyResponse>> SetTeamProfitShareStrategyAsync(
        SetTeamProfitShareStrategyRequest request);
}

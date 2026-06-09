using Aidan.Core.Patterns;
using Nexus.Operators.Actors.Requests;
using Nexus.Operators.Actors.Responses;

namespace Nexus.Operators.Actors.Contracts;

// A team is a contexto to set their own gateway selection strategy and profit sharing in an independent way of the operation the team belongs to
public interface ITeamLeader
{
    // operator management
    Task<IResult<AddOperatorToTeamResponse>> AddOperatorToTeamAsync(
        AddOperatorToTeamRequest request);
    Task<IResult<RemoveOperatorFromTeamResponse>> RemoveOperatorFromTeamAsync(
        RemoveOperatorFromTeamRequest request);

    // per strawman, per group, manual
    Task<IResult<SetTeamGatewaySelectionStrategyResponse>> SetTeamGatewaySelectionStrategyAsync(
        SetTeamGatewaySelectionStrategyRequest request);
    Task<IResult<AddStrawManToTeamResponse>> AddStrawManToTeamAsync(
        AddStrawManToTeamRequest request);
    Task<IResult<RemoveStrawManFromTeamResponse>> RemoveStrawManFromTeamAsync(
        RemoveStrawManFromTeamRequest request);
    Task<IResult<AddGatewayAccountGroupToTeamResponse>> AddGatewayAccountGroupToTeamAsync(
        AddGatewayAccountGroupToTeamRequest request);
    Task<IResult<RemoveGatewayAccountGroupFromTeamResponse>> RemoveGatewayAccountGroupFromTeamAsync(
        RemoveGatewayAccountGroupFromTeamRequest request);
    Task<IResult<AddGatewayAccountToTeamResponse>> AddGatewayAccountToTeamAsync(
        AddGatewayAccountToTeamRequest request);
    Task<IResult<RemoveGatewayAccountFromTeamResponse>> RemoveGatewayAccountFromTeamAsync(
        RemoveGatewayAccountFromTeamRequest request);

    // set profit share strategy
    Task<IResult<SetTeamProfitShareStrategyResponse>> SetTeamProfitShareStrategyAsync(
        SetTeamProfitShareStrategyRequest request);
}

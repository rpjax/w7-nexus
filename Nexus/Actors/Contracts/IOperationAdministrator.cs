using Aidan.Core.Patterns;
using Nexus.Actors.Requests;
using Nexus.Actors.Responses;

namespace Nexus.Actors.Contracts;

public interface IOperationAdministrator
{
    // operator management
    Task<IResult<AssignOperatorToOperationResponse>> AssignOperatorToOperationAsync(
        AssignOperatorToOperationRequest request);
    Task<IResult<UnassignOperatorFromOperationResponse>> UnassignOperatorFromOperationAsync(
        UnassignOperatorFromOperationRequest request);

    // per strawman, per group, manual
    Task<IResult<SetOperationGatewaySelectionStrategyResponse>> SetOperationGatewaySelectionStrategyAsync(
        SetOperationGatewaySelectionStrategyRequest request);
    Task<IResult<AssignStrawManToOperationResponse>> AssignStrawManToOperationAsync(
        AssignStrawManToOperationRequest request);
    Task<IResult<UnassignStrawManFromOperationResponse>> UnassignStrawManFromOperationAsync(
        UnassignStrawManFromOperationRequest request);
    Task<IResult<AssignGatewayAccountGroupToOperationResponse>> AssignGatewayAccountGroupToOperationAsync(
        AssignGatewayAccountGroupToOperationRequest request);
    Task<IResult<UnassignGatewayAccountGroupFromOperationResponse>> UnassignGatewayAccountGroupFromOperationAsync(
        UnassignGatewayAccountGroupFromOperationRequest request);
    Task<IResult<AssignGatewayAccountToOperationResponse>> AssignGatewayAccountToOperationAsync(
        AssignGatewayAccountToOperationRequest request);
    Task<IResult<UnassignGatewayAccountFromOperationResponse>> UnassignGatewayAccountFromOperationAsync(
        UnassignGatewayAccountFromOperationRequest request);

    // set profit share strategy
    Task<IResult<SetOperationProfitShareStrategyResponse>> SetOperationProfitShareStrategyAsync(
        SetOperationProfitShareStrategyRequest request);

    // team management
    Task<IResult<CreateOperationTeamResponse>> CreateOperationTeamAsync(
        CreateOperationTeamRequest request);
    Task<IResult<DeleteOperationTeamResponse>> DeleteOperationTeamAsync(
        DeleteOperationTeamRequest request);
    Task<IResult<AssignOperationTeamLeaderResponse>> AssignOperationTeamLeaderAsync(
        AssignOperationTeamLeaderRequest request);
    Task<IResult<UnassignOperationTeamLeaderResponse>> UnassignOperationTeamLeaderAsync(
        UnassignOperationTeamLeaderRequest request);
}

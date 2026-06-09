using Aidan.Core.Patterns;
using Nexus.Actors.Requests;
using Nexus.Actors.Responses;

namespace Nexus.Actors.Contracts;

public interface IOperationAdministrator
{
    // operator management
    Task<IResult<AddOperatorToOperationResponse>> AddOperatorToOperationAsync(
        AddOperatorToOperationRequest request);
    Task<IResult<RemoveOperatorFromOperationResponse>> RemoveOperatorFromOperationAsync(
        RemoveOperatorFromOperationRequest request);

    // per strawman, per group, manual
    Task<IResult<SetOperationGatewaySelectionStrategyResponse>> SetOperationGatewaySelectionStrategyAsync(
        SetOperationGatewaySelectionStrategyRequest request);
    Task<IResult<AddStrawManToOperationResponse>> AddStrawManToOperationAsync(
        AddStrawManToOperationRequest request);
    Task<IResult<RemoveStrawManFromOperationResponse>> RemoveStrawManFromOperationAsync(
        RemoveStrawManFromOperationRequest request);
    Task<IResult<AddGatewayAccountGroupToOperationResponse>> AddGatewayAccountGroupToOperationAsync(
        AddGatewayAccountGroupToOperationRequest request);
    Task<IResult<RemoveGatewayAccountGroupFromOperationResponse>> RemoveGatewayAccountGroupFromOperationAsync(
        RemoveGatewayAccountGroupFromOperationRequest request);
    Task<IResult<AddGatewayAccountToOperationResponse>> AddGatewayAccountToOperationAsync(
        AddGatewayAccountToOperationRequest request);
    Task<IResult<RemoveGatewayAccountFromOperationResponse>> RemoveGatewayAccountFromOperationAsync(
        RemoveGatewayAccountFromOperationRequest request);

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

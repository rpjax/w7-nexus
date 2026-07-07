using Aidan.Core.Patterns;
using Nexus.Operations.Application.Requests.OperationAdministrator;
using Nexus.Operations.Application.Responses.OperationAdministrator;

namespace Nexus.Operations.Application.Contracts;

public interface IOperationAdministratorOperationCommandService
{
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
}

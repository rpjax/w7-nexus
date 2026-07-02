using Aidan.Core.Patterns;
using Nexus.Operations.Application.Requests.Administrator;
using Nexus.Operations.Application.Responses.Administrator;
using Nexus.Operations.Application.Responses.Administrator.Models;

namespace Nexus.Operations.Application.Contracts;

public interface IAdministratorOperationCommandService
{
    Task<IResult<OperationDetails>> CreateOperationAsync(CreateOperationRequest request);

    Task<IResult<DeleteOperationResponse>> DeleteOperationAsync(DeleteOperationRequest request);

    Task<IResult<AssignOperationAdministratorResponse>> AssignOperationAdministratorAsync(
        AssignOperationAdministratorRequest request);

    Task<IResult<UnassignOperationAdministratorResponse>> UnassignOperationAdministratorAsync(
        UnassignOperationAdministratorRequest request);

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

using Aidan.Core.Patterns;
using Nexus.Administrators.Application.Requests;
using Nexus.Administrators.Application.Responses;
using Nexus.Administrators.Application.Responses.Models;

namespace Nexus.Administrators.Application.Contracts;

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

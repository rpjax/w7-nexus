using Aidan.Core.Patterns;
using Nexus.Administrators.Application.Requests;
using Nexus.Administrators.Application.Responses;

namespace Nexus.Administrators.Application.Contracts;

public interface IAdministratorTeamCommandService
{
    Task<IResult<CreateOperationTeamResponse>> CreateOperationTeamAsync(CreateOperationTeamRequest request);

    Task<IResult<DeleteOperationTeamResponse>> DeleteOperationTeamAsync(DeleteOperationTeamRequest request);

    Task<IResult<AssignOperationTeamLeaderResponse>> AssignOperationTeamLeaderAsync(
        AssignOperationTeamLeaderRequest request);

    Task<IResult<UnassignOperationTeamLeaderResponse>> UnassignOperationTeamLeaderAsync(
        UnassignOperationTeamLeaderRequest request);

    Task<IResult<SetTeamGatewaySelectionStrategyResponse>> SetTeamGatewaySelectionStrategyAsync(
        SetTeamGatewaySelectionStrategyRequest request);

    Task<IResult<AssignStrawManToTeamResponse>> AssignStrawManToTeamAsync(AssignStrawManToTeamRequest request);

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
}

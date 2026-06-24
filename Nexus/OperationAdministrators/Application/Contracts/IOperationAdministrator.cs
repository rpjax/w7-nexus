using Nexus.Authorization.Application.Models;
using Nexus.OperationAdministrators.Application.Requests;
using Nexus.OperationAdministrators.Application.Responses;

namespace Nexus.OperationAdministrators.Application.Contracts;

public interface IOperationAdministrator
{
    Task<IOperationResult<SearchOperationsResponse>> SearchOperationsAsync(
        RequesterIdentity identity,
        SearchOperationsRequest request,
        CancellationToken cancellationToken = default);

    Task<IOperationResult<SearchTeamLeaderCandidatesResponse>> SearchTeamLeaderCandidatesAsync(
        RequesterIdentity identity,
        SearchTeamLeaderCandidatesRequest request,
        CancellationToken cancellationToken = default);

    Task<IOperationResult<SearchStrawMenToAssignResponse>> SearchStrawMenToAssignAsync(
        RequesterIdentity identity,
        SearchStrawMenToAssignRequest request,
        CancellationToken cancellationToken = default);

    Task<IOperationResult<CreateOperationTeamResponse>> CreateOperationTeamAsync(
        RequesterIdentity identity,
        CreateOperationTeamRequest request,
        CancellationToken cancellationToken = default);

    Task<IOperationResult<DeleteOperationTeamResponse>> DeleteOperationTeamAsync(
        RequesterIdentity identity,
        DeleteOperationTeamRequest request,
        CancellationToken cancellationToken = default);

    Task<IOperationResult<AssignOperationTeamLeaderResponse>> AssignOperationTeamLeaderAsync(
        RequesterIdentity identity,
        AssignOperationTeamLeaderRequest request,
        CancellationToken cancellationToken = default);

    Task<IOperationResult<UnassignOperationTeamLeaderResponse>> UnassignOperationTeamLeaderAsync(
        RequesterIdentity identity,
        UnassignOperationTeamLeaderRequest request,
        CancellationToken cancellationToken = default);

    Task<IOperationResult<SetTeamGatewaySelectionStrategyResponse>> SetTeamGatewaySelectionStrategyAsync(
        RequesterIdentity identity,
        SetTeamGatewaySelectionStrategyRequest request,
        CancellationToken cancellationToken = default);

    Task<IOperationResult<AssignStrawManToTeamResponse>> AssignStrawManToTeamAsync(
        RequesterIdentity identity,
        AssignStrawManToTeamRequest request,
        CancellationToken cancellationToken = default);

    Task<IOperationResult<UnassignStrawManFromTeamResponse>> UnassignStrawManFromTeamAsync(
        RequesterIdentity identity,
        UnassignStrawManFromTeamRequest request,
        CancellationToken cancellationToken = default);

    Task<IOperationResult<AssignGatewayAccountGroupToTeamResponse>> AssignGatewayAccountGroupToTeamAsync(
        RequesterIdentity identity,
        AssignGatewayAccountGroupToTeamRequest request,
        CancellationToken cancellationToken = default);

    Task<IOperationResult<UnassignGatewayAccountGroupFromTeamResponse>> UnassignGatewayAccountGroupFromTeamAsync(
        RequesterIdentity identity,
        UnassignGatewayAccountGroupFromTeamRequest request,
        CancellationToken cancellationToken = default);

    Task<IOperationResult<AssignGatewayAccountToTeamResponse>> AssignGatewayAccountToTeamAsync(
        RequesterIdentity identity,
        AssignGatewayAccountToTeamRequest request,
        CancellationToken cancellationToken = default);

    Task<IOperationResult<UnassignGatewayAccountFromTeamResponse>> UnassignGatewayAccountFromTeamAsync(
        RequesterIdentity identity,
        UnassignGatewayAccountFromTeamRequest request,
        CancellationToken cancellationToken = default);

    Task<IOperationResult<SetOperationGatewaySelectionStrategyResponse>> SetOperationGatewaySelectionStrategyAsync(
        RequesterIdentity identity,
        SetOperationGatewaySelectionStrategyRequest request,
        CancellationToken cancellationToken = default);

    Task<IOperationResult<AssignStrawManToOperationResponse>> AssignStrawManToOperationAsync(
        RequesterIdentity identity,
        AssignStrawManToOperationRequest request,
        CancellationToken cancellationToken = default);

    Task<IOperationResult<UnassignStrawManFromOperationResponse>> UnassignStrawManFromOperationAsync(
        RequesterIdentity identity,
        UnassignStrawManFromOperationRequest request,
        CancellationToken cancellationToken = default);

    Task<IOperationResult<AssignGatewayAccountGroupToOperationResponse>> AssignGatewayAccountGroupToOperationAsync(
        RequesterIdentity identity,
        AssignGatewayAccountGroupToOperationRequest request,
        CancellationToken cancellationToken = default);

    Task<IOperationResult<UnassignGatewayAccountGroupFromOperationResponse>> UnassignGatewayAccountGroupFromOperationAsync(
        RequesterIdentity identity,
        UnassignGatewayAccountGroupFromOperationRequest request,
        CancellationToken cancellationToken = default);

    Task<IOperationResult<AssignGatewayAccountToOperationResponse>> AssignGatewayAccountToOperationAsync(
        RequesterIdentity identity,
        AssignGatewayAccountToOperationRequest request,
        CancellationToken cancellationToken = default);

    Task<IOperationResult<UnassignGatewayAccountFromOperationResponse>> UnassignGatewayAccountFromOperationAsync(
        RequesterIdentity identity,
        UnassignGatewayAccountFromOperationRequest request,
        CancellationToken cancellationToken = default);
}

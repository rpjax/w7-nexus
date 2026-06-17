using Aidan.Core.Patterns;
using Nexus.Authorizations.Application.Models;
using Nexus.OperationAdministrators.Application.Contracts;
using Nexus.OperationAdministrators.Application.Requests;
using Nexus.OperationAdministrators.Application.Responses;

namespace Nexus.OperationAdministrators.Application.Services;

public class OperationAdministrator : IOperationAdministrator
{
    private IOperationAdministratorAccessPolicy _policy { get; }
    private IOperationAdministratorOperationSearchService _operationSearch { get; }
    private IOperationAdministratorTeamCommandService _teamCommands { get; }
    private IOperationAdministratorTeamLeaderCandidateSearchService _teamLeaderCandidateSearch { get; }
    private IOperationAdministratorStrawManAssignmentSearchService _strawManAssignmentSearch { get; }

    public OperationAdministrator(
        IOperationAdministratorAccessPolicy policy,
        IOperationAdministratorOperationSearchService operationSearch,
        IOperationAdministratorTeamCommandService teamCommands,
        IOperationAdministratorTeamLeaderCandidateSearchService teamLeaderCandidateSearch,
        IOperationAdministratorStrawManAssignmentSearchService strawManAssignmentSearch)
    {
        _policy = policy;
        _operationSearch = operationSearch;
        _teamCommands = teamCommands;
        _teamLeaderCandidateSearch = teamLeaderCandidateSearch;
        _strawManAssignmentSearch = strawManAssignmentSearch;
    }

    public Task<IOperationResult<SearchOperationsResponse>> SearchOperationsAsync(
        RequesterIdentity identity,
        SearchOperationsRequest request,
        CancellationToken cancellationToken = default)
    {
        return ExecuteAsync(
            identity,
            ct => _policy.AuthorizeSearchOperationsAsync(identity, ct),
            () => _operationSearch.SearchOperationsAsync(identity, request),
            cancellationToken);
    }

    public Task<IOperationResult<SearchTeamLeaderCandidatesResponse>> SearchTeamLeaderCandidatesAsync(
        RequesterIdentity identity,
        SearchTeamLeaderCandidatesRequest request,
        CancellationToken cancellationToken = default)
    {
        return ExecuteAsync(
            identity,
            ct => _policy.AuthorizeSearchOperationsAsync(identity, ct),
            () => _teamLeaderCandidateSearch.SearchTeamLeaderCandidatesAsync(request),
            cancellationToken);
    }

    public Task<IOperationResult<SearchStrawMenToAssignResponse>> SearchStrawMenToAssignAsync(
        RequesterIdentity identity,
        SearchStrawMenToAssignRequest request,
        CancellationToken cancellationToken = default)
    {
        return ExecuteAsync(
            identity,
            ct => _policy.AuthorizeSearchOperationsAsync(identity, ct),
            () => _strawManAssignmentSearch.SearchStrawMenToAssignAsync(request),
            cancellationToken);
    }

    public Task<IOperationResult<CreateOperationTeamResponse>> CreateOperationTeamAsync(
        RequesterIdentity identity,
        CreateOperationTeamRequest request,
        CancellationToken cancellationToken = default)
    {
        return ExecuteAsync(
            identity,
            ct => _policy.AuthorizeManageOperationAsync(identity, operationId: request?.OperationId ?? string.Empty, cancellationToken: ct),
            () => _teamCommands.CreateOperationTeamAsync(request),
            cancellationToken);
    }

    public Task<IOperationResult<DeleteOperationTeamResponse>> DeleteOperationTeamAsync(
        RequesterIdentity identity,
        DeleteOperationTeamRequest request,
        CancellationToken cancellationToken = default)
    {
        return ExecuteAsync(
            identity,
            ct => _policy.AuthorizeManageOperationAsync(identity, teamId: request?.TeamId ?? string.Empty, cancellationToken: ct),
            () => _teamCommands.DeleteOperationTeamAsync(request),
            cancellationToken);
    }

    public Task<IOperationResult<AssignOperationTeamLeaderResponse>> AssignOperationTeamLeaderAsync(
        RequesterIdentity identity,
        AssignOperationTeamLeaderRequest request,
        CancellationToken cancellationToken = default)
    {
        return ExecuteAsync(
            identity,
            ct => _policy.AuthorizeManageOperationAsync(identity, teamId: request?.TeamId ?? string.Empty, cancellationToken: ct),
            () => _teamCommands.AssignOperationTeamLeaderAsync(request),
            cancellationToken);
    }

    public Task<IOperationResult<UnassignOperationTeamLeaderResponse>> UnassignOperationTeamLeaderAsync(
        RequesterIdentity identity,
        UnassignOperationTeamLeaderRequest request,
        CancellationToken cancellationToken = default)
    {
        return ExecuteAsync(
            identity,
            ct => _policy.AuthorizeManageOperationAsync(identity, teamId: request?.TeamId ?? string.Empty, cancellationToken: ct),
            () => _teamCommands.UnassignOperationTeamLeaderAsync(request),
            cancellationToken);
    }

    public Task<IOperationResult<SetTeamGatewaySelectionStrategyResponse>> SetTeamGatewaySelectionStrategyAsync(
        RequesterIdentity identity,
        SetTeamGatewaySelectionStrategyRequest request,
        CancellationToken cancellationToken = default)
    {
        return ExecuteAsync(
            identity,
            ct => _policy.AuthorizeManageOperationAsync(identity, teamId: request?.TeamId ?? string.Empty, cancellationToken: ct),
            () => _teamCommands.SetTeamGatewaySelectionStrategyAsync(request),
            cancellationToken);
    }

    public Task<IOperationResult<AssignStrawManToTeamResponse>> AssignStrawManToTeamAsync(
        RequesterIdentity identity,
        AssignStrawManToTeamRequest request,
        CancellationToken cancellationToken = default)
    {
        return ExecuteAsync(
            identity,
            ct => _policy.AuthorizeManageOperationAsync(identity, teamId: request?.TeamId ?? string.Empty, cancellationToken: ct),
            () => _teamCommands.AssignStrawManToTeamAsync(request),
            cancellationToken);
    }

    public Task<IOperationResult<UnassignStrawManFromTeamResponse>> UnassignStrawManFromTeamAsync(
        RequesterIdentity identity,
        UnassignStrawManFromTeamRequest request,
        CancellationToken cancellationToken = default)
    {
        return ExecuteAsync(
            identity,
            ct => _policy.AuthorizeManageOperationAsync(identity, teamId: request?.TeamId ?? string.Empty, cancellationToken: ct),
            () => _teamCommands.UnassignStrawManFromTeamAsync(request),
            cancellationToken);
    }

    public Task<IOperationResult<AssignGatewayAccountGroupToTeamResponse>> AssignGatewayAccountGroupToTeamAsync(
        RequesterIdentity identity,
        AssignGatewayAccountGroupToTeamRequest request,
        CancellationToken cancellationToken = default)
    {
        return ExecuteAsync(
            identity,
            ct => _policy.AuthorizeManageOperationAsync(identity, teamId: request?.TeamId ?? string.Empty, cancellationToken: ct),
            () => _teamCommands.AssignGatewayAccountGroupToTeamAsync(request),
            cancellationToken);
    }

    public Task<IOperationResult<UnassignGatewayAccountGroupFromTeamResponse>> UnassignGatewayAccountGroupFromTeamAsync(
        RequesterIdentity identity,
        UnassignGatewayAccountGroupFromTeamRequest request,
        CancellationToken cancellationToken = default)
    {
        return ExecuteAsync(
            identity,
            ct => _policy.AuthorizeManageOperationAsync(identity, teamId: request?.TeamId ?? string.Empty, cancellationToken: ct),
            () => _teamCommands.UnassignGatewayAccountGroupFromTeamAsync(request),
            cancellationToken);
    }

    public Task<IOperationResult<AssignGatewayAccountToTeamResponse>> AssignGatewayAccountToTeamAsync(
        RequesterIdentity identity,
        AssignGatewayAccountToTeamRequest request,
        CancellationToken cancellationToken = default)
    {
        return ExecuteAsync(
            identity,
            ct => _policy.AuthorizeManageOperationAsync(identity, teamId: request?.TeamId ?? string.Empty, cancellationToken: ct),
            () => _teamCommands.AssignGatewayAccountToTeamAsync(request),
            cancellationToken);
    }

    public Task<IOperationResult<UnassignGatewayAccountFromTeamResponse>> UnassignGatewayAccountFromTeamAsync(
        RequesterIdentity identity,
        UnassignGatewayAccountFromTeamRequest request,
        CancellationToken cancellationToken = default)
    {
        return ExecuteAsync(
            identity,
            ct => _policy.AuthorizeManageOperationAsync(identity, teamId: request?.TeamId ?? string.Empty, cancellationToken: ct),
            () => _teamCommands.UnassignGatewayAccountFromTeamAsync(request),
            cancellationToken);
    }

    private async Task<IOperationResult<T>> ExecuteAsync<T>(
        RequesterIdentity identity,
        Func<CancellationToken, Task<IAuthorizationResult>> authorizeAsync,
        Func<Task<IResult<T>>> executeAsync,
        CancellationToken cancellationToken)
    {
        var authorization = await authorizeAsync(cancellationToken);

        if (authorization.IsFailure)
            return OperationResult<T>.Failure(authorization.Errors);

        if (!authorization.IsAuthorized)
            return OperationResult<T>.Unauthorized(authorization.AuthorizationErrors);

        var result = await executeAsync();

        if (result.IsFailure)
            return OperationResult<T>.Failure(result.Errors);

        if (result.Value is not T value)
            return OperationResult<T>.Failure(result.Errors);

        return OperationResult<T>.Success(value);
    }
}

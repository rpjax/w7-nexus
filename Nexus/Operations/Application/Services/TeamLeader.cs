using Aidan.Core.Patterns;
using Nexus.Authorization.Application.Models;
using Nexus.Operations.Application.Contracts;
using Nexus.Operations.Application.Requests.TeamLeader;
using Nexus.Operations.Application.Responses.TeamLeader;

namespace Nexus.Operations.Application.Services;

public class TeamLeader : ITeamLeader
{
    private ITeamLeaderAccessPolicy _policy { get; }
    private ITeamLeaderLedTeamsSearchService _ledTeamsSearch { get; }
    private ITeamLeaderTeamCommandService _teamCommands { get; }
    private ITeamLeaderOperatorAssignmentSearchService _operatorAssignmentSearch { get; }
    private ITeamLeaderProfitShareAccountSearchService _profitShareAccountSearch { get; }

    public TeamLeader(
        ITeamLeaderAccessPolicy policy,
        ITeamLeaderLedTeamsSearchService ledTeamsSearch,
        ITeamLeaderTeamCommandService teamCommands,
        ITeamLeaderOperatorAssignmentSearchService operatorAssignmentSearch,
        ITeamLeaderProfitShareAccountSearchService profitShareAccountSearch)
    {
        _policy = policy;
        _ledTeamsSearch = ledTeamsSearch;
        _teamCommands = teamCommands;
        _operatorAssignmentSearch = operatorAssignmentSearch;
        _profitShareAccountSearch = profitShareAccountSearch;
    }

    public Task<IOperationResult<SearchLedTeamsResponse>> SearchLedTeamsAsync(
        RequesterIdentity identity,
        SearchLedTeamsRequest request,
        CancellationToken cancellationToken = default)
    {
        return ExecuteAsync(
            identity,
            _ => _policy.AuthorizeSearchLedTeamsAsync(identity),
            () => _ledTeamsSearch.SearchLedTeamsAsync(identity, request),
            cancellationToken);
    }

    public Task<IOperationResult<AssignOperatorToTeamResponse>> AssignOperatorToTeamAsync(
        RequesterIdentity identity,
        AssignOperatorToTeamRequest request,
        CancellationToken cancellationToken = default)
    {
        return ExecuteAsync(
            identity,
            _ => _policy.AuthorizeManageTeamAsync(identity, teamId: request?.TeamId ?? string.Empty),
            () => _teamCommands.AssignOperatorToTeamAsync(request),
            cancellationToken);
    }

    public Task<IOperationResult<UnassignOperatorFromTeamResponse>> UnassignOperatorFromTeamAsync(
        RequesterIdentity identity,
        UnassignOperatorFromTeamRequest request,
        CancellationToken cancellationToken = default)
    {
        return ExecuteAsync(
            identity,
            _ => _policy.AuthorizeManageTeamAsync(identity, teamId: request?.TeamId ?? string.Empty),
            () => _teamCommands.UnassignOperatorFromTeamAsync(request),
            cancellationToken);
    }

    public Task<IOperationResult<SetOperatorProfitShareRuleResponse>> SetOperatorProfitShareRuleAsync(
        RequesterIdentity identity,
        SetOperatorProfitShareRuleRequest request,
        CancellationToken cancellationToken = default)
    {
        return ExecuteAsync(
            identity,
            _ => _policy.AuthorizeManageTeamAsync(identity, teamId: request?.TeamId ?? string.Empty),
            () => _teamCommands.SetOperatorProfitShareRuleAsync(request),
            cancellationToken);
    }

    public Task<IOperationResult<SearchOperatorsToAssignResponse>> SearchOperatorsToAssignAsync(
        RequesterIdentity identity,
        SearchOperatorsToAssignRequest request,
        CancellationToken cancellationToken = default)
    {
        return ExecuteAsync(
            identity,
            _ => _policy.AuthorizeManageTeamAsync(identity, teamId: request?.TeamId ?? string.Empty),
            () => _operatorAssignmentSearch.SearchOperatorsToAssignAsync(request),
            cancellationToken);
    }

    public Task<IOperationResult<SearchProfitShareAccountsToAssignResponse>> SearchProfitShareAccountsToAssignAsync(
        RequesterIdentity identity,
        SearchProfitShareAccountsToAssignRequest request,
        CancellationToken cancellationToken = default)
    {
        return ExecuteAsync(
            identity,
            _ => _policy.AuthorizeManageTeamAsync(identity, teamId: request?.TeamId ?? string.Empty),
            () => _profitShareAccountSearch.SearchProfitShareAccountsToAssignAsync(request),
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

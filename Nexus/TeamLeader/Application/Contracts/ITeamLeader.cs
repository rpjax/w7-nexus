using Aidan.Core.Patterns;
using Nexus.Authorization.Application.Models;
using Nexus.TeamLeader.Application.Requests;
using Nexus.TeamLeader.Application.Responses;

namespace Nexus.TeamLeader.Application.Contracts;

public interface ITeamLeader
{
    Task<IOperationResult<SearchLedTeamsResponse>> SearchLedTeamsAsync(
        RequesterIdentity identity,
        SearchLedTeamsRequest request,
        CancellationToken cancellationToken = default);

    Task<IOperationResult<AssignOperatorToTeamResponse>> AssignOperatorToTeamAsync(
        RequesterIdentity identity,
        AssignOperatorToTeamRequest request,
        CancellationToken cancellationToken = default);

    Task<IOperationResult<UnassignOperatorFromTeamResponse>> UnassignOperatorFromTeamAsync(
        RequesterIdentity identity,
        UnassignOperatorFromTeamRequest request,
        CancellationToken cancellationToken = default);

    Task<IOperationResult<SetOperatorProfitShareRuleResponse>> SetOperatorProfitShareRuleAsync(
        RequesterIdentity identity,
        SetOperatorProfitShareRuleRequest request,
        CancellationToken cancellationToken = default);

    Task<IOperationResult<SearchOperatorsToAssignResponse>> SearchOperatorsToAssignAsync(
        RequesterIdentity identity,
        SearchOperatorsToAssignRequest request,
        CancellationToken cancellationToken = default);

    Task<IOperationResult<SearchProfitShareAccountsToAssignResponse>> SearchProfitShareAccountsToAssignAsync(
        RequesterIdentity identity,
        SearchProfitShareAccountsToAssignRequest request,
        CancellationToken cancellationToken = default);
}

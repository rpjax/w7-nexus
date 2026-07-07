using Aidan.Core.Patterns;
using Nexus.Authorization.Application.Models;
using Nexus.Operations.Application.Contracts;
using Nexus.Operations.Application.Requests.Administrator;
using Nexus.Operations.Application.Responses.Administrator;
using Nexus.Operations.Application.Responses.Administrator.Models;

namespace Nexus.Operations.Application.Services;

public sealed class Administrator : IAdministrator
{
    private readonly IAdministratorAccessPolicy _policy;
    private readonly IAdministratorOperationSearchService _operationSearch;
    private readonly IAdministratorOperationCommandService _operationCommands;
    private readonly IAdministratorTeamCommandService _teamCommands;
    private readonly IAdministratorTeamOperatorCommandService _teamOperatorCommands;
    private readonly IAdministratorOperatorAssignmentSearchService _operatorAssignmentSearch;
    private readonly IAdministratorProfitShareAccountSearchService _profitShareAccountSearch;
    private readonly IAdministratorOperationPickerSearchService _operationPickerSearch;

    public Administrator(
        IAdministratorAccessPolicy policy,
        IAdministratorOperationSearchService operationSearch,
        IAdministratorOperationCommandService operationCommands,
        IAdministratorTeamCommandService teamCommands,
        IAdministratorTeamOperatorCommandService teamOperatorCommands,
        IAdministratorOperatorAssignmentSearchService operatorAssignmentSearch,
        IAdministratorProfitShareAccountSearchService profitShareAccountSearch,
        IAdministratorOperationPickerSearchService operationPickerSearch)
    {
        _policy = policy;
        _operationSearch = operationSearch;
        _operationCommands = operationCommands;
        _teamCommands = teamCommands;
        _teamOperatorCommands = teamOperatorCommands;
        _operatorAssignmentSearch = operatorAssignmentSearch;
        _profitShareAccountSearch = profitShareAccountSearch;
        _operationPickerSearch = operationPickerSearch;
    }

    public Task<IOperationResult<OperationDetails>> CreateOperationAsync(
        RequesterIdentity identity,
        CreateOperationRequest request,
        CancellationToken cancellationToken = default) =>
        ExecuteAsync(identity, () => _operationCommands.CreateOperationAsync(request), cancellationToken);

    public Task<IOperationResult<SearchOperationsResponse>> SearchOperationsAsync(
        RequesterIdentity identity,
        SearchOperationsRequest request,
        CancellationToken cancellationToken = default) =>
        ExecuteAsync(identity, () => _operationSearch.SearchOperationsAsync(request), cancellationToken);

    public Task<IOperationResult<DeleteOperationResponse>> DeleteOperationAsync(
        RequesterIdentity identity,
        DeleteOperationRequest request,
        CancellationToken cancellationToken = default) =>
        ExecuteAsync(identity, () => _operationCommands.DeleteOperationAsync(request), cancellationToken);

    public Task<IOperationResult<AssignOperationAdministratorResponse>> AssignOperationAdministratorAsync(
        RequesterIdentity identity,
        AssignOperationAdministratorRequest request,
        CancellationToken cancellationToken = default) =>
        ExecuteAsync(identity, () => _operationCommands.AssignOperationAdministratorAsync(request), cancellationToken);

    public Task<IOperationResult<UnassignOperationAdministratorResponse>> UnassignOperationAdministratorAsync(
        RequesterIdentity identity,
        UnassignOperationAdministratorRequest request,
        CancellationToken cancellationToken = default) =>
        ExecuteAsync(identity, () => _operationCommands.UnassignOperationAdministratorAsync(request), cancellationToken);

    public Task<IOperationResult<SetOperationGatewaySelectionStrategyResponse>> SetOperationGatewaySelectionStrategyAsync(
        RequesterIdentity identity,
        SetOperationGatewaySelectionStrategyRequest request,
        CancellationToken cancellationToken = default) =>
        ExecuteAsync(identity, () => _operationCommands.SetOperationGatewaySelectionStrategyAsync(request), cancellationToken);

    public Task<IOperationResult<AssignStrawManToOperationResponse>> AssignStrawManToOperationAsync(
        RequesterIdentity identity,
        AssignStrawManToOperationRequest request,
        CancellationToken cancellationToken = default) =>
        ExecuteAsync(identity, () => _operationCommands.AssignStrawManToOperationAsync(request), cancellationToken);

    public Task<IOperationResult<UnassignStrawManFromOperationResponse>> UnassignStrawManFromOperationAsync(
        RequesterIdentity identity,
        UnassignStrawManFromOperationRequest request,
        CancellationToken cancellationToken = default) =>
        ExecuteAsync(identity, () => _operationCommands.UnassignStrawManFromOperationAsync(request), cancellationToken);

    public Task<IOperationResult<AssignGatewayAccountGroupToOperationResponse>> AssignGatewayAccountGroupToOperationAsync(
        RequesterIdentity identity,
        AssignGatewayAccountGroupToOperationRequest request,
        CancellationToken cancellationToken = default) =>
        ExecuteAsync(identity, () => _operationCommands.AssignGatewayAccountGroupToOperationAsync(request), cancellationToken);

    public Task<IOperationResult<UnassignGatewayAccountGroupFromOperationResponse>> UnassignGatewayAccountGroupFromOperationAsync(
        RequesterIdentity identity,
        UnassignGatewayAccountGroupFromOperationRequest request,
        CancellationToken cancellationToken = default) =>
        ExecuteAsync(identity, () => _operationCommands.UnassignGatewayAccountGroupFromOperationAsync(request), cancellationToken);

    public Task<IOperationResult<AssignGatewayAccountToOperationResponse>> AssignGatewayAccountToOperationAsync(
        RequesterIdentity identity,
        AssignGatewayAccountToOperationRequest request,
        CancellationToken cancellationToken = default) =>
        ExecuteAsync(identity, () => _operationCommands.AssignGatewayAccountToOperationAsync(request), cancellationToken);

    public Task<IOperationResult<UnassignGatewayAccountFromOperationResponse>> UnassignGatewayAccountFromOperationAsync(
        RequesterIdentity identity,
        UnassignGatewayAccountFromOperationRequest request,
        CancellationToken cancellationToken = default) =>
        ExecuteAsync(identity, () => _operationCommands.UnassignGatewayAccountFromOperationAsync(request), cancellationToken);

    public Task<IOperationResult<CreateOperationTeamResponse>> CreateOperationTeamAsync(
        RequesterIdentity identity,
        CreateOperationTeamRequest request,
        CancellationToken cancellationToken = default) =>
        ExecuteAsync(identity, () => _teamCommands.CreateOperationTeamAsync(request), cancellationToken);

    public Task<IOperationResult<DeleteOperationTeamResponse>> DeleteOperationTeamAsync(
        RequesterIdentity identity,
        DeleteOperationTeamRequest request,
        CancellationToken cancellationToken = default) =>
        ExecuteAsync(identity, () => _teamCommands.DeleteOperationTeamAsync(request), cancellationToken);

    public Task<IOperationResult<AssignOperationTeamLeaderResponse>> AssignOperationTeamLeaderAsync(
        RequesterIdentity identity,
        AssignOperationTeamLeaderRequest request,
        CancellationToken cancellationToken = default) =>
        ExecuteAsync(identity, () => _teamCommands.AssignOperationTeamLeaderAsync(request), cancellationToken);

    public Task<IOperationResult<UnassignOperationTeamLeaderResponse>> UnassignOperationTeamLeaderAsync(
        RequesterIdentity identity,
        UnassignOperationTeamLeaderRequest request,
        CancellationToken cancellationToken = default) =>
        ExecuteAsync(identity, () => _teamCommands.UnassignOperationTeamLeaderAsync(request), cancellationToken);

    public Task<IOperationResult<SetTeamGatewaySelectionStrategyResponse>> SetTeamGatewaySelectionStrategyAsync(
        RequesterIdentity identity,
        SetTeamGatewaySelectionStrategyRequest request,
        CancellationToken cancellationToken = default) =>
        ExecuteAsync(identity, () => _teamCommands.SetTeamGatewaySelectionStrategyAsync(request), cancellationToken);

    public Task<IOperationResult<AssignStrawManToTeamResponse>> AssignStrawManToTeamAsync(
        RequesterIdentity identity,
        AssignStrawManToTeamRequest request,
        CancellationToken cancellationToken = default) =>
        ExecuteAsync(identity, () => _teamCommands.AssignStrawManToTeamAsync(request), cancellationToken);

    public Task<IOperationResult<UnassignStrawManFromTeamResponse>> UnassignStrawManFromTeamAsync(
        RequesterIdentity identity,
        UnassignStrawManFromTeamRequest request,
        CancellationToken cancellationToken = default) =>
        ExecuteAsync(identity, () => _teamCommands.UnassignStrawManFromTeamAsync(request), cancellationToken);

    public Task<IOperationResult<AssignGatewayAccountGroupToTeamResponse>> AssignGatewayAccountGroupToTeamAsync(
        RequesterIdentity identity,
        AssignGatewayAccountGroupToTeamRequest request,
        CancellationToken cancellationToken = default) =>
        ExecuteAsync(identity, () => _teamCommands.AssignGatewayAccountGroupToTeamAsync(request), cancellationToken);

    public Task<IOperationResult<UnassignGatewayAccountGroupFromTeamResponse>> UnassignGatewayAccountGroupFromTeamAsync(
        RequesterIdentity identity,
        UnassignGatewayAccountGroupFromTeamRequest request,
        CancellationToken cancellationToken = default) =>
        ExecuteAsync(identity, () => _teamCommands.UnassignGatewayAccountGroupFromTeamAsync(request), cancellationToken);

    public Task<IOperationResult<AssignGatewayAccountToTeamResponse>> AssignGatewayAccountToTeamAsync(
        RequesterIdentity identity,
        AssignGatewayAccountToTeamRequest request,
        CancellationToken cancellationToken = default) =>
        ExecuteAsync(identity, () => _teamCommands.AssignGatewayAccountToTeamAsync(request), cancellationToken);

    public Task<IOperationResult<UnassignGatewayAccountFromTeamResponse>> UnassignGatewayAccountFromTeamAsync(
        RequesterIdentity identity,
        UnassignGatewayAccountFromTeamRequest request,
        CancellationToken cancellationToken = default) =>
        ExecuteAsync(identity, () => _teamCommands.UnassignGatewayAccountFromTeamAsync(request), cancellationToken);

    public Task<IOperationResult<AssignOperatorToTeamResponse>> AssignOperatorToTeamAsync(
        RequesterIdentity identity,
        AssignOperatorToTeamRequest request,
        CancellationToken cancellationToken = default) =>
        ExecuteAsync(identity, () => _teamOperatorCommands.AssignOperatorToTeamAsync(request), cancellationToken);

    public Task<IOperationResult<UnassignOperatorFromTeamResponse>> UnassignOperatorFromTeamAsync(
        RequesterIdentity identity,
        UnassignOperatorFromTeamRequest request,
        CancellationToken cancellationToken = default) =>
        ExecuteAsync(identity, () => _teamOperatorCommands.UnassignOperatorFromTeamAsync(request), cancellationToken);

    public Task<IOperationResult<SetOperatorProfitShareRuleResponse>> SetOperatorProfitShareRuleAsync(
        RequesterIdentity identity,
        SetOperatorProfitShareRuleRequest request,
        CancellationToken cancellationToken = default) =>
        ExecuteAsync(identity, () => _teamOperatorCommands.SetOperatorProfitShareRuleAsync(request), cancellationToken);

    public Task<IOperationResult<SearchOperatorsToAssignResponse>> SearchOperatorsToAssignAsync(
        RequesterIdentity identity,
        SearchOperatorsToAssignRequest request,
        CancellationToken cancellationToken = default) =>
        ExecuteAsync(identity, () => _operatorAssignmentSearch.SearchOperatorsToAssignAsync(request), cancellationToken);

    public Task<IOperationResult<SearchProfitShareAccountsToAssignResponse>> SearchProfitShareAccountsToAssignAsync(
        RequesterIdentity identity,
        SearchProfitShareAccountsToAssignRequest request,
        CancellationToken cancellationToken = default) =>
        ExecuteAsync(identity, () => _profitShareAccountSearch.SearchProfitShareAccountsToAssignAsync(request), cancellationToken);

    public Task<IOperationResult<SearchOperationsToAssignResponse>> SearchOperationsToAssignAsync(
        RequesterIdentity identity,
        SearchOperationsToAssignRequest request,
        CancellationToken cancellationToken = default) =>
        ExecuteAsync(identity, () => _operationPickerSearch.SearchOperationsToAssignAsync(request), cancellationToken);

    private async Task<IOperationResult<T>> ExecuteAsync<T>(
        RequesterIdentity identity,
        Func<Task<IResult<T>>> executeAsync,
        CancellationToken cancellationToken = default)
    {
        var authorization = await _policy.AuthorizeAdministratorAsync(identity);

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

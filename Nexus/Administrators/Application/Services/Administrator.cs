using Nexus.Administrators.Application.Contracts;
using Nexus.Administrators.Application.Requests;
using Nexus.Administrators.Application.Responses;
using Nexus.Administrators.Application.Responses.Models;
using Nexus.Authorization.Application.Models;
using Nexus.BankAccounts.Aggregates;
using Nexus.BankAccounts.Application.Contracts;
using Nexus.BankAccounts.Application.Requests;
using Nexus.BankAccounts.Application.Responses;
using Nexus.CryptoWallets.Aggregates;
using Nexus.CryptoWallets.Application.Contracts;
using Nexus.CryptoWallets.Application.Requests;
using Nexus.CryptoWallets.Application.Responses;
using Nexus.Payments.Application.Models;
using Nexus.StrawMen.Application.Contracts;
using Nexus.Transfers.Aggregates;
using Nexus.Transfers.Application.Contracts;
using Nexus.Transfers.Application.Models;
using Nexus.Transfers.Application.Requests;

namespace Nexus.Administrators.Application.Services;

public class Administrator : IAdministrator
{
    private IAdministratorAccessPolicy _policy { get; }
    private IAdministratorOperationSearchService _operationSearch { get; }
    private IAdministratorAccountSearchService _accountSearch { get; }
    private IAdministratorAccountCommandService _accountCommands { get; }
    private IAdministratorOperationCommandService _operationCommands { get; }
    private IAdministratorTeamCommandService _teamCommands { get; }
    private IAdministratorTeamOperatorCommandService _teamOperatorCommands { get; }
    private IAdministratorOperatorAssignmentSearchService _operatorAssignmentSearch { get; }
    private IAdministratorProfitShareAccountSearchService _profitShareAccountSearch { get; }
    private IAdministratorOperationPickerSearchService _operationPickerSearch { get; }
    private IBankAccountService _bankAccountService { get; }
    private ICryptoWalletService _cryptoWalletService { get; }
    private ITransferService _transfers { get; }
    private IAdministratorPaymentSearchService _paymentSearch { get; }
    private IAdministratorPaymentCommandService _paymentCommands { get; }
    private IAdministratorStrawManSettingsCommandService _strawManSettings { get; }

    public Administrator(
        IAdministratorAccessPolicy policy,
        IAdministratorOperationSearchService operationSearch,
        IAdministratorAccountSearchService accountSearch,
        IAdministratorAccountCommandService accountCommands,
        IAdministratorOperationCommandService operationCommands,
        IAdministratorTeamCommandService teamCommands,
        IAdministratorTeamOperatorCommandService teamOperatorCommands,
        IAdministratorOperatorAssignmentSearchService operatorAssignmentSearch,
        IAdministratorProfitShareAccountSearchService profitShareAccountSearch,
        IAdministratorOperationPickerSearchService operationPickerSearch,
        IBankAccountService bankAccountService,
        ICryptoWalletService cryptoWalletService,
        ITransferService transfers,
        IAdministratorPaymentSearchService paymentSearch,
        IAdministratorPaymentCommandService paymentCommands,
        IAdministratorStrawManSettingsCommandService strawManSettings)
    {
        _policy = policy;
        _operationSearch = operationSearch;
        _accountSearch = accountSearch;
        _accountCommands = accountCommands;
        _operationCommands = operationCommands;
        _teamCommands = teamCommands;
        _teamOperatorCommands = teamOperatorCommands;
        _operatorAssignmentSearch = operatorAssignmentSearch;
        _profitShareAccountSearch = profitShareAccountSearch;
        _operationPickerSearch = operationPickerSearch;
        _bankAccountService = bankAccountService;
        _cryptoWalletService = cryptoWalletService;
        _transfers = transfers;
        _paymentSearch = paymentSearch;
        _paymentCommands = paymentCommands;
        _strawManSettings = strawManSettings;
    }

    public async Task<IOperationResult<OperationDetails>> CreateOperationAsync(
        RequesterIdentity identity,
        CreateOperationRequest request,
        CancellationToken cancellationToken = default)
    {
        var authorization = await _policy.AuthorizeAdministratorAsync(identity);

        if (authorization.IsFailure)
            return OperationResult<OperationDetails>.Failure(authorization.Errors);

        if (!authorization.IsAuthorized)
            return OperationResult<OperationDetails>.Unauthorized(authorization.AuthorizationErrors);

        var result = await _operationCommands.CreateOperationAsync(request);

        if (result.IsFailure)
            return OperationResult<OperationDetails>.Failure(result.Errors);

        if (result.Value is not OperationDetails value)
            throw new InvalidOperationException();

        return OperationResult<OperationDetails>.Success(value);
    }

    public async Task<IOperationResult<SearchOperationsResponse>> SearchOperationsAsync(
        RequesterIdentity identity,
        SearchOperationsRequest request,
        CancellationToken cancellationToken = default)
    {
        var authorization = await _policy.AuthorizeAdministratorAsync(identity);

        if (authorization.IsFailure)
            return OperationResult<SearchOperationsResponse>.Failure(authorization.Errors);

        if (!authorization.IsAuthorized)
            return OperationResult<SearchOperationsResponse>.Unauthorized(authorization.AuthorizationErrors);

        var result = await _operationSearch.SearchOperationsAsync(request);

        if (result.IsFailure)
            return OperationResult<SearchOperationsResponse>.Failure(result.Errors);

        if (result.Value is not SearchOperationsResponse value)
            throw new InvalidOperationException();

        return OperationResult<SearchOperationsResponse>.Success(value);
    }

    public async Task<IOperationResult<DeleteOperationResponse>> DeleteOperationAsync(
        RequesterIdentity identity,
        DeleteOperationRequest request,
        CancellationToken cancellationToken = default)
    {
        var authorization = await _policy.AuthorizeAdministratorAsync(identity);

        if (authorization.IsFailure)
            return OperationResult<DeleteOperationResponse>.Failure(authorization.Errors);

        if (!authorization.IsAuthorized)
            return OperationResult<DeleteOperationResponse>.Unauthorized(authorization.AuthorizationErrors);

        var result = await _operationCommands.DeleteOperationAsync(request);

        if (result.IsFailure)
            return OperationResult<DeleteOperationResponse>.Failure(result.Errors);

        if (result.Value is not DeleteOperationResponse value)
            throw new InvalidOperationException();

        return OperationResult<DeleteOperationResponse>.Success(value);
    }

    public async Task<IOperationResult<AssignOperationAdministratorResponse>> AssignOperationAdministratorAsync(
        RequesterIdentity identity,
        AssignOperationAdministratorRequest request,
        CancellationToken cancellationToken = default)
    {
        var authorization = await _policy.AuthorizeAdministratorAsync(identity);

        if (authorization.IsFailure)
            return OperationResult<AssignOperationAdministratorResponse>.Failure(authorization.Errors);

        if (!authorization.IsAuthorized)
            return OperationResult<AssignOperationAdministratorResponse>.Unauthorized(authorization.AuthorizationErrors);

        var result = await _operationCommands.AssignOperationAdministratorAsync(request);

        if (result.IsFailure)
            return OperationResult<AssignOperationAdministratorResponse>.Failure(result.Errors);

        if (result.Value is not AssignOperationAdministratorResponse value)
            throw new InvalidOperationException();

        return OperationResult<AssignOperationAdministratorResponse>.Success(value);
    }

    public async Task<IOperationResult<UnassignOperationAdministratorResponse>> UnassignOperationAdministratorAsync(
        RequesterIdentity identity,
        UnassignOperationAdministratorRequest request,
        CancellationToken cancellationToken = default)
    {
        var authorization = await _policy.AuthorizeAdministratorAsync(identity);

        if (authorization.IsFailure)
            return OperationResult<UnassignOperationAdministratorResponse>.Failure(authorization.Errors);

        if (!authorization.IsAuthorized)
            return OperationResult<UnassignOperationAdministratorResponse>.Unauthorized(authorization.AuthorizationErrors);

        var result = await _operationCommands.UnassignOperationAdministratorAsync(request);

        if (result.IsFailure)
            return OperationResult<UnassignOperationAdministratorResponse>.Failure(result.Errors);

        if (result.Value is not UnassignOperationAdministratorResponse value)
            throw new InvalidOperationException();

        return OperationResult<UnassignOperationAdministratorResponse>.Success(value);
    }

    public async Task<IOperationResult<SetOperationGatewaySelectionStrategyResponse>> SetOperationGatewaySelectionStrategyAsync(
        RequesterIdentity identity,
        SetOperationGatewaySelectionStrategyRequest request,
        CancellationToken cancellationToken = default)
    {
        var authorization = await _policy.AuthorizeAdministratorAsync(identity);

        if (authorization.IsFailure)
            return OperationResult<SetOperationGatewaySelectionStrategyResponse>.Failure(authorization.Errors);

        if (!authorization.IsAuthorized)
            return OperationResult<SetOperationGatewaySelectionStrategyResponse>.Unauthorized(authorization.AuthorizationErrors);

        var result = await _operationCommands.SetOperationGatewaySelectionStrategyAsync(request);

        if (result.IsFailure)
            return OperationResult<SetOperationGatewaySelectionStrategyResponse>.Failure(result.Errors);

        if (result.Value is not SetOperationGatewaySelectionStrategyResponse value)
            throw new InvalidOperationException();

        return OperationResult<SetOperationGatewaySelectionStrategyResponse>.Success(value);
    }

    public async Task<IOperationResult<AssignStrawManToOperationResponse>> AssignStrawManToOperationAsync(
        RequesterIdentity identity,
        AssignStrawManToOperationRequest request,
        CancellationToken cancellationToken = default)
    {
        var authorization = await _policy.AuthorizeAdministratorAsync(identity);

        if (authorization.IsFailure)
            return OperationResult<AssignStrawManToOperationResponse>.Failure(authorization.Errors);

        if (!authorization.IsAuthorized)
            return OperationResult<AssignStrawManToOperationResponse>.Unauthorized(authorization.AuthorizationErrors);

        var result = await _operationCommands.AssignStrawManToOperationAsync(request);

        if (result.IsFailure)
            return OperationResult<AssignStrawManToOperationResponse>.Failure(result.Errors);

        if (result.Value is not AssignStrawManToOperationResponse value)
            throw new InvalidOperationException();

        return OperationResult<AssignStrawManToOperationResponse>.Success(value);
    }

    public async Task<IOperationResult<UnassignStrawManFromOperationResponse>> UnassignStrawManFromOperationAsync(
        RequesterIdentity identity,
        UnassignStrawManFromOperationRequest request,
        CancellationToken cancellationToken = default)
    {
        var authorization = await _policy.AuthorizeAdministratorAsync(identity);

        if (authorization.IsFailure)
            return OperationResult<UnassignStrawManFromOperationResponse>.Failure(authorization.Errors);

        if (!authorization.IsAuthorized)
            return OperationResult<UnassignStrawManFromOperationResponse>.Unauthorized(authorization.AuthorizationErrors);

        var result = await _operationCommands.UnassignStrawManFromOperationAsync(request);

        if (result.IsFailure)
            return OperationResult<UnassignStrawManFromOperationResponse>.Failure(result.Errors);

        if (result.Value is not UnassignStrawManFromOperationResponse value)
            throw new InvalidOperationException();

        return OperationResult<UnassignStrawManFromOperationResponse>.Success(value);
    }

    public async Task<IOperationResult<AssignGatewayAccountGroupToOperationResponse>> AssignGatewayAccountGroupToOperationAsync(
        RequesterIdentity identity,
        AssignGatewayAccountGroupToOperationRequest request,
        CancellationToken cancellationToken = default)
    {
        var authorization = await _policy.AuthorizeAdministratorAsync(identity);

        if (authorization.IsFailure)
            return OperationResult<AssignGatewayAccountGroupToOperationResponse>.Failure(authorization.Errors);

        if (!authorization.IsAuthorized)
            return OperationResult<AssignGatewayAccountGroupToOperationResponse>.Unauthorized(authorization.AuthorizationErrors);

        var result = await _operationCommands.AssignGatewayAccountGroupToOperationAsync(request);

        if (result.IsFailure)
            return OperationResult<AssignGatewayAccountGroupToOperationResponse>.Failure(result.Errors);

        if (result.Value is not AssignGatewayAccountGroupToOperationResponse value)
            throw new InvalidOperationException();

        return OperationResult<AssignGatewayAccountGroupToOperationResponse>.Success(value);
    }

    public async Task<IOperationResult<UnassignGatewayAccountGroupFromOperationResponse>> UnassignGatewayAccountGroupFromOperationAsync(
        RequesterIdentity identity,
        UnassignGatewayAccountGroupFromOperationRequest request,
        CancellationToken cancellationToken = default)
    {
        var authorization = await _policy.AuthorizeAdministratorAsync(identity);

        if (authorization.IsFailure)
            return OperationResult<UnassignGatewayAccountGroupFromOperationResponse>.Failure(authorization.Errors);

        if (!authorization.IsAuthorized)
            return OperationResult<UnassignGatewayAccountGroupFromOperationResponse>.Unauthorized(authorization.AuthorizationErrors);

        var result = await _operationCommands.UnassignGatewayAccountGroupFromOperationAsync(request);

        if (result.IsFailure)
            return OperationResult<UnassignGatewayAccountGroupFromOperationResponse>.Failure(result.Errors);

        if (result.Value is not UnassignGatewayAccountGroupFromOperationResponse value)
            throw new InvalidOperationException();

        return OperationResult<UnassignGatewayAccountGroupFromOperationResponse>.Success(value);
    }

    public async Task<IOperationResult<AssignGatewayAccountToOperationResponse>> AssignGatewayAccountToOperationAsync(
        RequesterIdentity identity,
        AssignGatewayAccountToOperationRequest request,
        CancellationToken cancellationToken = default)
    {
        var authorization = await _policy.AuthorizeAdministratorAsync(identity);

        if (authorization.IsFailure)
            return OperationResult<AssignGatewayAccountToOperationResponse>.Failure(authorization.Errors);

        if (!authorization.IsAuthorized)
            return OperationResult<AssignGatewayAccountToOperationResponse>.Unauthorized(authorization.AuthorizationErrors);

        var result = await _operationCommands.AssignGatewayAccountToOperationAsync(request);

        if (result.IsFailure)
            return OperationResult<AssignGatewayAccountToOperationResponse>.Failure(result.Errors);

        if (result.Value is not AssignGatewayAccountToOperationResponse value)
            throw new InvalidOperationException();

        return OperationResult<AssignGatewayAccountToOperationResponse>.Success(value);
    }

    public async Task<IOperationResult<UnassignGatewayAccountFromOperationResponse>> UnassignGatewayAccountFromOperationAsync(
        RequesterIdentity identity,
        UnassignGatewayAccountFromOperationRequest request,
        CancellationToken cancellationToken = default)
    {
        var authorization = await _policy.AuthorizeAdministratorAsync(identity);

        if (authorization.IsFailure)
            return OperationResult<UnassignGatewayAccountFromOperationResponse>.Failure(authorization.Errors);

        if (!authorization.IsAuthorized)
            return OperationResult<UnassignGatewayAccountFromOperationResponse>.Unauthorized(authorization.AuthorizationErrors);

        var result = await _operationCommands.UnassignGatewayAccountFromOperationAsync(request);

        if (result.IsFailure)
            return OperationResult<UnassignGatewayAccountFromOperationResponse>.Failure(result.Errors);

        if (result.Value is not UnassignGatewayAccountFromOperationResponse value)
            throw new InvalidOperationException();

        return OperationResult<UnassignGatewayAccountFromOperationResponse>.Success(value);
    }

    public async Task<IOperationResult<SearchAccountsResponse>> SearchAccountsAsync(
        RequesterIdentity identity,
        SearchAccountsRequest request,
        CancellationToken cancellationToken = default)
    {
        var authorization = await _policy.AuthorizeAdministratorAsync(identity);

        if (authorization.IsFailure)
            return OperationResult<SearchAccountsResponse>.Failure(authorization.Errors);

        if (!authorization.IsAuthorized)
            return OperationResult<SearchAccountsResponse>.Unauthorized(authorization.AuthorizationErrors);

        var result = await _accountSearch.SearchAccountsAsync(request);

        if (result.IsFailure)
            return OperationResult<SearchAccountsResponse>.Failure(result.Errors);

        if (result.Value is not SearchAccountsResponse value)
            throw new InvalidOperationException();

        return OperationResult<SearchAccountsResponse>.Success(value);
    }

    public async Task<IOperationResult<GrantAccountRoleResponse>> GrantAccountRoleAsync(
        RequesterIdentity identity,
        GrantAccountRoleRequest request,
        CancellationToken cancellationToken = default)
    {
        var authorization = await _policy.AuthorizeAdministratorAsync(identity);

        if (authorization.IsFailure)
            return OperationResult<GrantAccountRoleResponse>.Failure(authorization.Errors);

        if (!authorization.IsAuthorized)
            return OperationResult<GrantAccountRoleResponse>.Unauthorized(authorization.AuthorizationErrors);

        var result = await _accountCommands.GrantAccountRoleAsync(request);

        if (result.IsFailure)
            return OperationResult<GrantAccountRoleResponse>.Failure(result.Errors);

        if (result.Value is not GrantAccountRoleResponse value)
            throw new InvalidOperationException();

        return OperationResult<GrantAccountRoleResponse>.Success(value);
    }

    public async Task<IOperationResult<RevokeAccountRoleResponse>> RevokeAccountRoleAsync(
        RequesterIdentity identity,
        RevokeAccountRoleRequest request,
        CancellationToken cancellationToken = default)
    {
        var authorization = await _policy.AuthorizeAdministratorAsync(identity);

        if (authorization.IsFailure)
            return OperationResult<RevokeAccountRoleResponse>.Failure(authorization.Errors);

        if (!authorization.IsAuthorized)
            return OperationResult<RevokeAccountRoleResponse>.Unauthorized(authorization.AuthorizationErrors);

        var result = await _accountCommands.RevokeAccountRoleAsync(request);

        if (result.IsFailure)
            return OperationResult<RevokeAccountRoleResponse>.Failure(result.Errors);

        if (result.Value is not RevokeAccountRoleResponse value)
            throw new InvalidOperationException();

        return OperationResult<RevokeAccountRoleResponse>.Success(value);
    }

    public async Task<IOperationResult<GrantAccountPermissionResponse>> GrantAccountPermissionAsync(
        RequesterIdentity identity,
        GrantAccountPermissionRequest request,
        CancellationToken cancellationToken = default)
    {
        var authorization = await _policy.AuthorizeAdministratorAsync(identity);

        if (authorization.IsFailure)
            return OperationResult<GrantAccountPermissionResponse>.Failure(authorization.Errors);

        if (!authorization.IsAuthorized)
            return OperationResult<GrantAccountPermissionResponse>.Unauthorized(authorization.AuthorizationErrors);

        var result = await _accountCommands.GrantAccountPermissionAsync(request);

        if (result.IsFailure)
            return OperationResult<GrantAccountPermissionResponse>.Failure(result.Errors);

        if (result.Value is not GrantAccountPermissionResponse value)
            throw new InvalidOperationException();

        return OperationResult<GrantAccountPermissionResponse>.Success(value);
    }

    public async Task<IOperationResult<RevokeAccountPermissionResponse>> RevokeAccountPermissionAsync(
        RequesterIdentity identity,
        RevokeAccountPermissionRequest request,
        CancellationToken cancellationToken = default)
    {
        var authorization = await _policy.AuthorizeAdministratorAsync(identity);

        if (authorization.IsFailure)
            return OperationResult<RevokeAccountPermissionResponse>.Failure(authorization.Errors);

        if (!authorization.IsAuthorized)
            return OperationResult<RevokeAccountPermissionResponse>.Unauthorized(authorization.AuthorizationErrors);

        var result = await _accountCommands.RevokeAccountPermissionAsync(request);

        if (result.IsFailure)
            return OperationResult<RevokeAccountPermissionResponse>.Failure(result.Errors);

        if (result.Value is not RevokeAccountPermissionResponse value)
            throw new InvalidOperationException();

        return OperationResult<RevokeAccountPermissionResponse>.Success(value);
    }

    public async Task<IOperationResult<CreateOperationTeamResponse>> CreateOperationTeamAsync(
        RequesterIdentity identity,
        CreateOperationTeamRequest request,
        CancellationToken cancellationToken = default)
    {
        var authorization = await _policy.AuthorizeAdministratorAsync(identity);

        if (authorization.IsFailure)
            return OperationResult<CreateOperationTeamResponse>.Failure(authorization.Errors);

        if (!authorization.IsAuthorized)
            return OperationResult<CreateOperationTeamResponse>.Unauthorized(authorization.AuthorizationErrors);

        var result = await _teamCommands.CreateOperationTeamAsync(request);

        if (result.IsFailure)
            return OperationResult<CreateOperationTeamResponse>.Failure(result.Errors);

        if (result.Value is not CreateOperationTeamResponse value)
            throw new InvalidOperationException();

        return OperationResult<CreateOperationTeamResponse>.Success(value);
    }

    public async Task<IOperationResult<DeleteOperationTeamResponse>> DeleteOperationTeamAsync(
        RequesterIdentity identity,
        DeleteOperationTeamRequest request,
        CancellationToken cancellationToken = default)
    {
        var authorization = await _policy.AuthorizeAdministratorAsync(identity);

        if (authorization.IsFailure)
            return OperationResult<DeleteOperationTeamResponse>.Failure(authorization.Errors);

        if (!authorization.IsAuthorized)
            return OperationResult<DeleteOperationTeamResponse>.Unauthorized(authorization.AuthorizationErrors);

        var result = await _teamCommands.DeleteOperationTeamAsync(request);

        if (result.IsFailure)
            return OperationResult<DeleteOperationTeamResponse>.Failure(result.Errors);

        if (result.Value is not DeleteOperationTeamResponse value)
            throw new InvalidOperationException();

        return OperationResult<DeleteOperationTeamResponse>.Success(value);
    }

    public async Task<IOperationResult<AssignOperationTeamLeaderResponse>> AssignOperationTeamLeaderAsync(
        RequesterIdentity identity,
        AssignOperationTeamLeaderRequest request,
        CancellationToken cancellationToken = default)
    {
        var authorization = await _policy.AuthorizeAdministratorAsync(identity);

        if (authorization.IsFailure)
            return OperationResult<AssignOperationTeamLeaderResponse>.Failure(authorization.Errors);

        if (!authorization.IsAuthorized)
            return OperationResult<AssignOperationTeamLeaderResponse>.Unauthorized(authorization.AuthorizationErrors);

        var result = await _teamCommands.AssignOperationTeamLeaderAsync(request);

        if (result.IsFailure)
            return OperationResult<AssignOperationTeamLeaderResponse>.Failure(result.Errors);

        if (result.Value is not AssignOperationTeamLeaderResponse value)
            throw new InvalidOperationException();

        return OperationResult<AssignOperationTeamLeaderResponse>.Success(value);
    }

    public async Task<IOperationResult<UnassignOperationTeamLeaderResponse>> UnassignOperationTeamLeaderAsync(
        RequesterIdentity identity,
        UnassignOperationTeamLeaderRequest request,
        CancellationToken cancellationToken = default)
    {
        var authorization = await _policy.AuthorizeAdministratorAsync(identity);

        if (authorization.IsFailure)
            return OperationResult<UnassignOperationTeamLeaderResponse>.Failure(authorization.Errors);

        if (!authorization.IsAuthorized)
            return OperationResult<UnassignOperationTeamLeaderResponse>.Unauthorized(authorization.AuthorizationErrors);

        var result = await _teamCommands.UnassignOperationTeamLeaderAsync(request);

        if (result.IsFailure)
            return OperationResult<UnassignOperationTeamLeaderResponse>.Failure(result.Errors);

        if (result.Value is not UnassignOperationTeamLeaderResponse value)
            throw new InvalidOperationException();

        return OperationResult<UnassignOperationTeamLeaderResponse>.Success(value);
    }

    public async Task<IOperationResult<SetTeamGatewaySelectionStrategyResponse>> SetTeamGatewaySelectionStrategyAsync(
        RequesterIdentity identity,
        SetTeamGatewaySelectionStrategyRequest request,
        CancellationToken cancellationToken = default)
    {
        var authorization = await _policy.AuthorizeAdministratorAsync(identity);

        if (authorization.IsFailure)
            return OperationResult<SetTeamGatewaySelectionStrategyResponse>.Failure(authorization.Errors);

        if (!authorization.IsAuthorized)
            return OperationResult<SetTeamGatewaySelectionStrategyResponse>.Unauthorized(authorization.AuthorizationErrors);

        var result = await _teamCommands.SetTeamGatewaySelectionStrategyAsync(request);

        if (result.IsFailure)
            return OperationResult<SetTeamGatewaySelectionStrategyResponse>.Failure(result.Errors);

        if (result.Value is not SetTeamGatewaySelectionStrategyResponse value)
            throw new InvalidOperationException();

        return OperationResult<SetTeamGatewaySelectionStrategyResponse>.Success(value);
    }

    public async Task<IOperationResult<AssignStrawManToTeamResponse>> AssignStrawManToTeamAsync(
        RequesterIdentity identity,
        AssignStrawManToTeamRequest request,
        CancellationToken cancellationToken = default)
    {
        var authorization = await _policy.AuthorizeAdministratorAsync(identity);

        if (authorization.IsFailure)
            return OperationResult<AssignStrawManToTeamResponse>.Failure(authorization.Errors);

        if (!authorization.IsAuthorized)
            return OperationResult<AssignStrawManToTeamResponse>.Unauthorized(authorization.AuthorizationErrors);

        var result = await _teamCommands.AssignStrawManToTeamAsync(request);

        if (result.IsFailure)
            return OperationResult<AssignStrawManToTeamResponse>.Failure(result.Errors);

        if (result.Value is not AssignStrawManToTeamResponse value)
            throw new InvalidOperationException();

        return OperationResult<AssignStrawManToTeamResponse>.Success(value);
    }

    public async Task<IOperationResult<UnassignStrawManFromTeamResponse>> UnassignStrawManFromTeamAsync(
        RequesterIdentity identity,
        UnassignStrawManFromTeamRequest request,
        CancellationToken cancellationToken = default)
    {
        var authorization = await _policy.AuthorizeAdministratorAsync(identity);

        if (authorization.IsFailure)
            return OperationResult<UnassignStrawManFromTeamResponse>.Failure(authorization.Errors);

        if (!authorization.IsAuthorized)
            return OperationResult<UnassignStrawManFromTeamResponse>.Unauthorized(authorization.AuthorizationErrors);

        var result = await _teamCommands.UnassignStrawManFromTeamAsync(request);

        if (result.IsFailure)
            return OperationResult<UnassignStrawManFromTeamResponse>.Failure(result.Errors);

        if (result.Value is not UnassignStrawManFromTeamResponse value)
            throw new InvalidOperationException();

        return OperationResult<UnassignStrawManFromTeamResponse>.Success(value);
    }

    public async Task<IOperationResult<AssignGatewayAccountGroupToTeamResponse>> AssignGatewayAccountGroupToTeamAsync(
        RequesterIdentity identity,
        AssignGatewayAccountGroupToTeamRequest request,
        CancellationToken cancellationToken = default)
    {
        var authorization = await _policy.AuthorizeAdministratorAsync(identity);

        if (authorization.IsFailure)
            return OperationResult<AssignGatewayAccountGroupToTeamResponse>.Failure(authorization.Errors);

        if (!authorization.IsAuthorized)
            return OperationResult<AssignGatewayAccountGroupToTeamResponse>.Unauthorized(authorization.AuthorizationErrors);

        var result = await _teamCommands.AssignGatewayAccountGroupToTeamAsync(request);

        if (result.IsFailure)
            return OperationResult<AssignGatewayAccountGroupToTeamResponse>.Failure(result.Errors);

        if (result.Value is not AssignGatewayAccountGroupToTeamResponse value)
            throw new InvalidOperationException();

        return OperationResult<AssignGatewayAccountGroupToTeamResponse>.Success(value);
    }

    public async Task<IOperationResult<UnassignGatewayAccountGroupFromTeamResponse>> UnassignGatewayAccountGroupFromTeamAsync(
        RequesterIdentity identity,
        UnassignGatewayAccountGroupFromTeamRequest request,
        CancellationToken cancellationToken = default)
    {
        var authorization = await _policy.AuthorizeAdministratorAsync(identity);

        if (authorization.IsFailure)
            return OperationResult<UnassignGatewayAccountGroupFromTeamResponse>.Failure(authorization.Errors);

        if (!authorization.IsAuthorized)
            return OperationResult<UnassignGatewayAccountGroupFromTeamResponse>.Unauthorized(authorization.AuthorizationErrors);

        var result = await _teamCommands.UnassignGatewayAccountGroupFromTeamAsync(request);

        if (result.IsFailure)
            return OperationResult<UnassignGatewayAccountGroupFromTeamResponse>.Failure(result.Errors);

        if (result.Value is not UnassignGatewayAccountGroupFromTeamResponse value)
            throw new InvalidOperationException();

        return OperationResult<UnassignGatewayAccountGroupFromTeamResponse>.Success(value);
    }

    public async Task<IOperationResult<AssignGatewayAccountToTeamResponse>> AssignGatewayAccountToTeamAsync(
        RequesterIdentity identity,
        AssignGatewayAccountToTeamRequest request,
        CancellationToken cancellationToken = default)
    {
        var authorization = await _policy.AuthorizeAdministratorAsync(identity);

        if (authorization.IsFailure)
            return OperationResult<AssignGatewayAccountToTeamResponse>.Failure(authorization.Errors);

        if (!authorization.IsAuthorized)
            return OperationResult<AssignGatewayAccountToTeamResponse>.Unauthorized(authorization.AuthorizationErrors);

        var result = await _teamCommands.AssignGatewayAccountToTeamAsync(request);

        if (result.IsFailure)
            return OperationResult<AssignGatewayAccountToTeamResponse>.Failure(result.Errors);

        if (result.Value is not AssignGatewayAccountToTeamResponse value)
            throw new InvalidOperationException();

        return OperationResult<AssignGatewayAccountToTeamResponse>.Success(value);
    }

    public async Task<IOperationResult<UnassignGatewayAccountFromTeamResponse>> UnassignGatewayAccountFromTeamAsync(
        RequesterIdentity identity,
        UnassignGatewayAccountFromTeamRequest request,
        CancellationToken cancellationToken = default)
    {
        var authorization = await _policy.AuthorizeAdministratorAsync(identity);

        if (authorization.IsFailure)
            return OperationResult<UnassignGatewayAccountFromTeamResponse>.Failure(authorization.Errors);

        if (!authorization.IsAuthorized)
            return OperationResult<UnassignGatewayAccountFromTeamResponse>.Unauthorized(authorization.AuthorizationErrors);

        var result = await _teamCommands.UnassignGatewayAccountFromTeamAsync(request);

        if (result.IsFailure)
            return OperationResult<UnassignGatewayAccountFromTeamResponse>.Failure(result.Errors);

        if (result.Value is not UnassignGatewayAccountFromTeamResponse value)
            throw new InvalidOperationException();

        return OperationResult<UnassignGatewayAccountFromTeamResponse>.Success(value);
    }

    public async Task<IOperationResult<AssignOperatorToTeamResponse>> AssignOperatorToTeamAsync(
        RequesterIdentity identity,
        AssignOperatorToTeamRequest request,
        CancellationToken cancellationToken = default)
    {
        var authorization = await _policy.AuthorizeAdministratorAsync(identity);

        if (authorization.IsFailure)
            return OperationResult<AssignOperatorToTeamResponse>.Failure(authorization.Errors);

        if (!authorization.IsAuthorized)
            return OperationResult<AssignOperatorToTeamResponse>.Unauthorized(authorization.AuthorizationErrors);

        var result = await _teamOperatorCommands.AssignOperatorToTeamAsync(request);

        if (result.IsFailure)
            return OperationResult<AssignOperatorToTeamResponse>.Failure(result.Errors);

        if (result.Value is not AssignOperatorToTeamResponse value)
            throw new InvalidOperationException();

        return OperationResult<AssignOperatorToTeamResponse>.Success(value);
    }

    public async Task<IOperationResult<UnassignOperatorFromTeamResponse>> UnassignOperatorFromTeamAsync(
        RequesterIdentity identity,
        UnassignOperatorFromTeamRequest request,
        CancellationToken cancellationToken = default)
    {
        var authorization = await _policy.AuthorizeAdministratorAsync(identity);

        if (authorization.IsFailure)
            return OperationResult<UnassignOperatorFromTeamResponse>.Failure(authorization.Errors);

        if (!authorization.IsAuthorized)
            return OperationResult<UnassignOperatorFromTeamResponse>.Unauthorized(authorization.AuthorizationErrors);

        var result = await _teamOperatorCommands.UnassignOperatorFromTeamAsync(request);

        if (result.IsFailure)
            return OperationResult<UnassignOperatorFromTeamResponse>.Failure(result.Errors);

        if (result.Value is not UnassignOperatorFromTeamResponse value)
            throw new InvalidOperationException();

        return OperationResult<UnassignOperatorFromTeamResponse>.Success(value);
    }

    public async Task<IOperationResult<SetOperatorProfitShareRuleResponse>> SetOperatorProfitShareRuleAsync(
        RequesterIdentity identity,
        SetOperatorProfitShareRuleRequest request,
        CancellationToken cancellationToken = default)
    {
        var authorization = await _policy.AuthorizeAdministratorAsync(identity);

        if (authorization.IsFailure)
            return OperationResult<SetOperatorProfitShareRuleResponse>.Failure(authorization.Errors);

        if (!authorization.IsAuthorized)
            return OperationResult<SetOperatorProfitShareRuleResponse>.Unauthorized(authorization.AuthorizationErrors);

        var result = await _teamOperatorCommands.SetOperatorProfitShareRuleAsync(request);

        if (result.IsFailure)
            return OperationResult<SetOperatorProfitShareRuleResponse>.Failure(result.Errors);

        if (result.Value is not SetOperatorProfitShareRuleResponse value)
            throw new InvalidOperationException();

        return OperationResult<SetOperatorProfitShareRuleResponse>.Success(value);
    }

    public async Task<IOperationResult<SearchOperatorsToAssignResponse>> SearchOperatorsToAssignAsync(
        RequesterIdentity identity,
        SearchOperatorsToAssignRequest request,
        CancellationToken cancellationToken = default)
    {
        var authorization = await _policy.AuthorizeAdministratorAsync(identity);

        if (authorization.IsFailure)
            return OperationResult<SearchOperatorsToAssignResponse>.Failure(authorization.Errors);

        if (!authorization.IsAuthorized)
            return OperationResult<SearchOperatorsToAssignResponse>.Unauthorized(authorization.AuthorizationErrors);

        var result = await _operatorAssignmentSearch.SearchOperatorsToAssignAsync(request);

        if (result.IsFailure)
            return OperationResult<SearchOperatorsToAssignResponse>.Failure(result.Errors);

        if (result.Value is not SearchOperatorsToAssignResponse value)
            throw new InvalidOperationException();

        return OperationResult<SearchOperatorsToAssignResponse>.Success(value);
    }

    public async Task<IOperationResult<SearchProfitShareAccountsToAssignResponse>> SearchProfitShareAccountsToAssignAsync(
        RequesterIdentity identity,
        SearchProfitShareAccountsToAssignRequest request,
        CancellationToken cancellationToken = default)
    {
        var authorization = await _policy.AuthorizeAdministratorAsync(identity);

        if (authorization.IsFailure)
            return OperationResult<SearchProfitShareAccountsToAssignResponse>.Failure(authorization.Errors);

        if (!authorization.IsAuthorized)
            return OperationResult<SearchProfitShareAccountsToAssignResponse>.Unauthorized(authorization.AuthorizationErrors);

        var result = await _profitShareAccountSearch.SearchProfitShareAccountsToAssignAsync(request);

        if (result.IsFailure)
            return OperationResult<SearchProfitShareAccountsToAssignResponse>.Failure(result.Errors);

        if (result.Value is not SearchProfitShareAccountsToAssignResponse value)
            throw new InvalidOperationException();

        return OperationResult<SearchProfitShareAccountsToAssignResponse>.Success(value);
    }

    public async Task<IOperationResult<SearchOperationsToAssignResponse>> SearchOperationsToAssignAsync(
        RequesterIdentity identity,
        SearchOperationsToAssignRequest request,
        CancellationToken cancellationToken = default)
    {
        var authorization = await _policy.AuthorizeAdministratorAsync(identity);

        if (authorization.IsFailure)
            return OperationResult<SearchOperationsToAssignResponse>.Failure(authorization.Errors);

        if (!authorization.IsAuthorized)
            return OperationResult<SearchOperationsToAssignResponse>.Unauthorized(authorization.AuthorizationErrors);

        var result = await _operationPickerSearch.SearchOperationsToAssignAsync(request);

        if (result.IsFailure)
            return OperationResult<SearchOperationsToAssignResponse>.Failure(result.Errors);

        if (result.Value is not SearchOperationsToAssignResponse value)
            throw new InvalidOperationException();

        return OperationResult<SearchOperationsToAssignResponse>.Success(value);
    }

    public async Task<IOperationResult<BankAccount>> CreateBankAccountAsync(
        RequesterIdentity identity,
        CreateBankAccountRequest request,
        CancellationToken cancellationToken = default)
    {
        var authorization = await _policy.AuthorizeAdministratorAsync(identity);

        if (authorization.IsFailure)
            return OperationResult<BankAccount>.Failure(authorization.Errors);

        if (!authorization.IsAuthorized)
            return OperationResult<BankAccount>.Unauthorized(authorization.AuthorizationErrors);

        var result = await _bankAccountService.CreateAsync(request);

        if (result.IsFailure)
            return OperationResult<BankAccount>.Failure(result.Errors);

        if (result.Value is not BankAccount value)
            throw new InvalidOperationException();

        return OperationResult<BankAccount>.Success(value);
    }

    public async Task<IOperationResult<CryptoWallet>> CreateCryptoWalletAsync(
        RequesterIdentity identity,
        CreateCryptoWalletRequest request,
        CancellationToken cancellationToken = default)
    {
        var authorization = await _policy.AuthorizeAdministratorAsync(identity);

        if (authorization.IsFailure)
            return OperationResult<CryptoWallet>.Failure(authorization.Errors);

        if (!authorization.IsAuthorized)
            return OperationResult<CryptoWallet>.Unauthorized(authorization.AuthorizationErrors);

        var result = await _cryptoWalletService.CreateAsync(request);

        if (result.IsFailure)
            return OperationResult<CryptoWallet>.Failure(result.Errors);

        if (result.Value is not CryptoWallet value)
            throw new InvalidOperationException();

        return OperationResult<CryptoWallet>.Success(value);
    }

    public async Task<IOperationResult<CryptoWallet>> UpsertCryptoWalletAddressAsync(
        RequesterIdentity identity,
        UpsertCryptoWalletAddressRequest request,
        CancellationToken cancellationToken = default)
    {
        var authorization = await _policy.AuthorizeAdministratorAsync(identity);

        if (authorization.IsFailure)
            return OperationResult<CryptoWallet>.Failure(authorization.Errors);

        if (!authorization.IsAuthorized)
            return OperationResult<CryptoWallet>.Unauthorized(authorization.AuthorizationErrors);

        var result = await _cryptoWalletService.UpsertAddressAsync(request);

        if (result.IsFailure)
            return OperationResult<CryptoWallet>.Failure(result.Errors);

        if (result.Value is not CryptoWallet value)
            throw new InvalidOperationException();

        return OperationResult<CryptoWallet>.Success(value);
    }

    public async Task<IOperationResult<BankAccount>> GetBankAccountAsync(
        RequesterIdentity identity,
        string bankAccountId,
        CancellationToken cancellationToken = default)
    {
        var authorization = await _policy.AuthorizeAdministratorAsync(identity);

        if (authorization.IsFailure)
            return OperationResult<BankAccount>.Failure(authorization.Errors);

        if (!authorization.IsAuthorized)
            return OperationResult<BankAccount>.Unauthorized(authorization.AuthorizationErrors);

        var result = await _bankAccountService.GetByIdAsync(bankAccountId);

        if (result.IsFailure)
            return OperationResult<BankAccount>.Failure(result.Errors);

        if (result.Value is not BankAccount value)
            throw new InvalidOperationException();

        return OperationResult<BankAccount>.Success(value);
    }

    public async Task<IOperationResult<CryptoWallet>> GetCryptoWalletAsync(
        RequesterIdentity identity,
        string cryptoWalletId,
        CancellationToken cancellationToken = default)
    {
        var authorization = await _policy.AuthorizeAdministratorAsync(identity);

        if (authorization.IsFailure)
            return OperationResult<CryptoWallet>.Failure(authorization.Errors);

        if (!authorization.IsAuthorized)
            return OperationResult<CryptoWallet>.Unauthorized(authorization.AuthorizationErrors);

        var result = await _cryptoWalletService.GetByIdAsync(cryptoWalletId);

        if (result.IsFailure)
            return OperationResult<CryptoWallet>.Failure(result.Errors);

        if (result.Value is not CryptoWallet value)
            throw new InvalidOperationException();

        return OperationResult<CryptoWallet>.Success(value);
    }

    public async Task<IOperationResult<Transfer>> ExecuteWithdrawalTransferAsync(
        RequesterIdentity identity,
        WithdrawalTransferRequest request,
        CancellationToken cancellationToken = default)
    {
        var authorization = await _policy.AuthorizeAdministratorAsync(identity);

        if (authorization.IsFailure)
            return OperationResult<Transfer>.Failure(authorization.Errors);

        if (!authorization.IsAuthorized)
            return OperationResult<Transfer>.Unauthorized(authorization.AuthorizationErrors);

        var result = await _transfers.ExecuteWithdrawalAsync(request, cancellationToken);

        if (result.IsFailure)
            return OperationResult<Transfer>.Failure(result.Errors);

        if (result.Value is not Transfer value)
            throw new InvalidOperationException();

        return OperationResult<Transfer>.Success(value);
    }

    public async Task<IOperationResult<Transfer>> ExecuteBankAccountMovementTransferAsync(
        RequesterIdentity identity,
        BankAccountMovementRequest request,
        CancellationToken cancellationToken = default)
    {
        var authorization = await _policy.AuthorizeAdministratorAsync(identity);

        if (authorization.IsFailure)
            return OperationResult<Transfer>.Failure(authorization.Errors);

        if (!authorization.IsAuthorized)
            return OperationResult<Transfer>.Unauthorized(authorization.AuthorizationErrors);

        var result = await _transfers.ExecuteBankAccountMovementAsync(request, cancellationToken);

        if (result.IsFailure)
            return OperationResult<Transfer>.Failure(result.Errors);

        if (result.Value is not Transfer value)
            throw new InvalidOperationException();

        return OperationResult<Transfer>.Success(value);
    }

    public async Task<IOperationResult<Transfer>> ExecuteCryptoWalletMovementTransferAsync(
        RequesterIdentity identity,
        CryptoWalletMovementRequest request,
        CancellationToken cancellationToken = default)
    {
        var authorization = await _policy.AuthorizeAdministratorAsync(identity);

        if (authorization.IsFailure)
            return OperationResult<Transfer>.Failure(authorization.Errors);

        if (!authorization.IsAuthorized)
            return OperationResult<Transfer>.Unauthorized(authorization.AuthorizationErrors);

        var result = await _transfers.ExecuteCryptoWalletMovementAsync(request, cancellationToken);

        if (result.IsFailure)
            return OperationResult<Transfer>.Failure(result.Errors);

        if (result.Value is not Transfer value)
            throw new InvalidOperationException();

        return OperationResult<Transfer>.Success(value);
    }

    public async Task<IOperationResult<Transfer>> ExecutePayoutTransferAsync(
        RequesterIdentity identity,
        PayoutTransferRequest request,
        CancellationToken cancellationToken = default)
    {
        var authorization = await _policy.AuthorizeAdministratorAsync(identity);

        if (authorization.IsFailure)
            return OperationResult<Transfer>.Failure(authorization.Errors);

        if (!authorization.IsAuthorized)
            return OperationResult<Transfer>.Unauthorized(authorization.AuthorizationErrors);

        var result = await _transfers.ExecutePayoutAsync(request, cancellationToken);

        if (result.IsFailure)
            return OperationResult<Transfer>.Failure(result.Errors);

        if (result.Value is not Transfer value)
            throw new InvalidOperationException();

        return OperationResult<Transfer>.Success(value);
    }

    public async Task<IOperationResult<Transfer>> GetTransferAsync(
        RequesterIdentity identity,
        string transferId,
        CancellationToken cancellationToken = default)
    {
        var authorization = await _policy.AuthorizeAdministratorAsync(identity);

        if (authorization.IsFailure)
            return OperationResult<Transfer>.Failure(authorization.Errors);

        if (!authorization.IsAuthorized)
            return OperationResult<Transfer>.Unauthorized(authorization.AuthorizationErrors);

        var result = await _transfers.GetByIdAsync(transferId);

        if (result.IsFailure)
            return OperationResult<Transfer>.Failure(result.Errors);

        if (result.Value is not Transfer value)
            throw new InvalidOperationException();

        return OperationResult<Transfer>.Success(value);
    }

    public async Task<IOperationResult<TransferTimelineDetails>> GetTransferTimelineAsync(
        RequesterIdentity identity,
        string transferId,
        CancellationToken cancellationToken = default)
    {
        var authorization = await _policy.AuthorizeAdministratorAsync(identity);

        if (authorization.IsFailure)
            return OperationResult<TransferTimelineDetails>.Failure(authorization.Errors);

        if (!authorization.IsAuthorized)
            return OperationResult<TransferTimelineDetails>.Unauthorized(authorization.AuthorizationErrors);

        var result = await _transfers.GetTimelineAsync(transferId, cancellationToken);

        if (result.IsFailure)
            return OperationResult<TransferTimelineDetails>.Failure(result.Errors);

        if (result.Value is not TransferTimelineDetails value)
            throw new InvalidOperationException();

        return OperationResult<TransferTimelineDetails>.Success(value);
    }

    public async Task<IOperationResult<SearchTransfersResponse>> SearchTransfersAsync(
        RequesterIdentity identity,
        SearchTransfersRequest? request,
        CancellationToken cancellationToken = default)
    {
        var authorization = await _policy.AuthorizeAdministratorAsync(identity);

        if (authorization.IsFailure)
            return OperationResult<SearchTransfersResponse>.Failure(authorization.Errors);

        if (!authorization.IsAuthorized)
            return OperationResult<SearchTransfersResponse>.Unauthorized(authorization.AuthorizationErrors);

        var result = await _transfers.SearchAsync(request, cancellationToken);

        if (result.IsFailure)
            return OperationResult<SearchTransfersResponse>.Failure(result.Errors);

        if (result.Value is not SearchTransfersResponse value)
            throw new InvalidOperationException();

        return OperationResult<SearchTransfersResponse>.Success(value);
    }

    public async Task<IOperationResult<SearchBankAccountsResponse>> SearchBankAccountsAsync(
        RequesterIdentity identity,
        SearchBankAccountsRequest? request,
        CancellationToken cancellationToken = default)
    {
        var authorization = await _policy.AuthorizeAdministratorAsync(identity);

        if (authorization.IsFailure)
            return OperationResult<SearchBankAccountsResponse>.Failure(authorization.Errors);

        if (!authorization.IsAuthorized)
            return OperationResult<SearchBankAccountsResponse>.Unauthorized(authorization.AuthorizationErrors);

        var result = await _bankAccountService.SearchAsync(request);

        if (result.IsFailure)
            return OperationResult<SearchBankAccountsResponse>.Failure(result.Errors);

        if (result.Value is not SearchBankAccountsResponse value)
            throw new InvalidOperationException();

        return OperationResult<SearchBankAccountsResponse>.Success(value);
    }

    public async Task<IOperationResult<BankAccount>> UpdateBankAccountLabelAsync(
        RequesterIdentity identity,
        string bankAccountId,
        string? label,
        CancellationToken cancellationToken = default)
    {
        var authorization = await _policy.AuthorizeAdministratorAsync(identity);

        if (authorization.IsFailure)
            return OperationResult<BankAccount>.Failure(authorization.Errors);

        if (!authorization.IsAuthorized)
            return OperationResult<BankAccount>.Unauthorized(authorization.AuthorizationErrors);

        var result = await _bankAccountService.UpdateLabelAsync(bankAccountId, label);

        if (result.IsFailure)
            return OperationResult<BankAccount>.Failure(result.Errors);

        if (result.Value is not BankAccount value)
            throw new InvalidOperationException();

        return OperationResult<BankAccount>.Success(value);
    }

    public async Task<IOperationResult<SearchCryptoWalletsResponse>> SearchCryptoWalletsAsync(
        RequesterIdentity identity,
        SearchCryptoWalletsRequest? request,
        CancellationToken cancellationToken = default)
    {
        var authorization = await _policy.AuthorizeAdministratorAsync(identity);

        if (authorization.IsFailure)
            return OperationResult<SearchCryptoWalletsResponse>.Failure(authorization.Errors);

        if (!authorization.IsAuthorized)
            return OperationResult<SearchCryptoWalletsResponse>.Unauthorized(authorization.AuthorizationErrors);

        var result = await _cryptoWalletService.SearchAsync(request);

        if (result.IsFailure)
            return OperationResult<SearchCryptoWalletsResponse>.Failure(result.Errors);

        if (result.Value is not SearchCryptoWalletsResponse value)
            throw new InvalidOperationException();

        return OperationResult<SearchCryptoWalletsResponse>.Success(value);
    }

    public async Task<IOperationResult<SearchPaymentsResponse>> SearchPaymentsAsync(
        RequesterIdentity identity,
        SearchPaymentsRequest request,
        CancellationToken cancellationToken = default)
    {
        var authorization = await _policy.AuthorizeAdministratorAsync(identity);

        if (authorization.IsFailure)
            return OperationResult<SearchPaymentsResponse>.Failure(authorization.Errors);

        if (!authorization.IsAuthorized)
            return OperationResult<SearchPaymentsResponse>.Unauthorized(authorization.AuthorizationErrors);

        var result = await _paymentSearch.SearchPaymentsAsync(request);

        if (result.IsFailure)
            return OperationResult<SearchPaymentsResponse>.Failure(result.Errors);

        if (result.Value is not SearchPaymentsResponse value)
            throw new InvalidOperationException();

        return OperationResult<SearchPaymentsResponse>.Success(value);
    }

    public async Task<IOperationResult<PaymentDetails>> GetPaymentAsync(
        RequesterIdentity identity,
        string paymentId,
        CancellationToken cancellationToken = default)
    {
        var authorization = await _policy.AuthorizeAdministratorAsync(identity);

        if (authorization.IsFailure)
            return OperationResult<PaymentDetails>.Failure(authorization.Errors);

        if (!authorization.IsAuthorized)
            return OperationResult<PaymentDetails>.Unauthorized(authorization.AuthorizationErrors);

        var result = await _paymentSearch.GetPaymentAsync(paymentId);

        if (result.IsFailure)
            return OperationResult<PaymentDetails>.Failure(result.Errors);

        if (result.Value is not PaymentDetails value)
            throw new InvalidOperationException();

        return OperationResult<PaymentDetails>.Success(value);
    }

    public async Task<IOperationResult<PaymentDetails>> PayPaymentAsync(
        RequesterIdentity identity,
        string paymentId,
        CancellationToken cancellationToken = default)
    {
        var authorization = await _policy.AuthorizeAdministratorAsync(identity);

        if (authorization.IsFailure)
            return OperationResult<PaymentDetails>.Failure(authorization.Errors);

        if (!authorization.IsAuthorized)
            return OperationResult<PaymentDetails>.Unauthorized(authorization.AuthorizationErrors);

        var result = await _paymentCommands.PayAndGetAsync(paymentId);

        if (result.IsFailure)
            return OperationResult<PaymentDetails>.Failure(result.Errors);

        if (result.Value is not PaymentDetails value)
            throw new InvalidOperationException();

        return OperationResult<PaymentDetails>.Success(value);
    }

    public async Task<IOperationResult<PaymentDetails>> RefundPaymentAsync(
        RequesterIdentity identity,
        string paymentId,
        CancellationToken cancellationToken = default)
    {
        var authorization = await _policy.AuthorizeAdministratorAsync(identity);

        if (authorization.IsFailure)
            return OperationResult<PaymentDetails>.Failure(authorization.Errors);

        if (!authorization.IsAuthorized)
            return OperationResult<PaymentDetails>.Unauthorized(authorization.AuthorizationErrors);

        var result = await _paymentCommands.RefundAndGetAsync(paymentId);

        if (result.IsFailure)
            return OperationResult<PaymentDetails>.Failure(result.Errors);

        if (result.Value is not PaymentDetails value)
            throw new InvalidOperationException();

        return OperationResult<PaymentDetails>.Success(value);
    }

    public async Task<IOperationResult<PaymentDetails>> KillPaymentAsync(
        RequesterIdentity identity,
        string paymentId,
        string reason,
        CancellationToken cancellationToken = default)
    {
        var authorization = await _policy.AuthorizeAdministratorAsync(identity);

        if (authorization.IsFailure)
            return OperationResult<PaymentDetails>.Failure(authorization.Errors);

        if (!authorization.IsAuthorized)
            return OperationResult<PaymentDetails>.Unauthorized(authorization.AuthorizationErrors);

        var result = await _paymentCommands.KillAndGetAsync(paymentId, reason);

        if (result.IsFailure)
            return OperationResult<PaymentDetails>.Failure(result.Errors);

        if (result.Value is not PaymentDetails value)
            throw new InvalidOperationException();

        return OperationResult<PaymentDetails>.Success(value);
    }

    public async Task<IOperationResult<PaymentDetails>> MarkPaymentAsDistributedAsync(
        RequesterIdentity identity,
        string paymentId,
        CancellationToken cancellationToken = default)
    {
        var authorization = await _policy.AuthorizeAdministratorAsync(identity);

        if (authorization.IsFailure)
            return OperationResult<PaymentDetails>.Failure(authorization.Errors);

        if (!authorization.IsAuthorized)
            return OperationResult<PaymentDetails>.Unauthorized(authorization.AuthorizationErrors);

        var result = await _paymentCommands.MarkAsDistributedAndGetAsync(paymentId);

        if (result.IsFailure)
            return OperationResult<PaymentDetails>.Failure(result.Errors);

        if (result.Value is not PaymentDetails value)
            throw new InvalidOperationException();

        return OperationResult<PaymentDetails>.Success(value);
    }

    public async Task<IOperationResult<bool>> DeletePaymentAsync(
        RequesterIdentity identity,
        string paymentId,
        CancellationToken cancellationToken = default)
    {
        var authorization = await _policy.AuthorizeAdministratorAsync(identity);

        if (authorization.IsFailure)
            return OperationResult<bool>.Failure(authorization.Errors);

        if (!authorization.IsAuthorized)
            return OperationResult<bool>.Unauthorized(authorization.AuthorizationErrors);

        var deleteResult = await _paymentCommands.DeletePaymentAsync(paymentId);
        if (deleteResult.IsFailure)
            return OperationResult<bool>.Failure(deleteResult.Errors);

        return OperationResult<bool>.Success(true);
    }

    public async Task<IOperationResult<PaymentDetails>> BindPaymentOperatorAsync(
        RequesterIdentity identity,
        string paymentId,
        string operatorAccountId,
        CancellationToken cancellationToken = default)
    {
        var authorization = await _policy.AuthorizeAdministratorAsync(identity);

        if (authorization.IsFailure)
            return OperationResult<PaymentDetails>.Failure(authorization.Errors);

        if (!authorization.IsAuthorized)
            return OperationResult<PaymentDetails>.Unauthorized(authorization.AuthorizationErrors);

        var result = await _paymentCommands.BindOperatorAsync(paymentId, operatorAccountId);

        if (result.IsFailure)
            return OperationResult<PaymentDetails>.Failure(result.Errors);

        if (result.Value is not PaymentDetails value)
            throw new InvalidOperationException();

        return OperationResult<PaymentDetails>.Success(value);
    }

    public async Task<IOperationResult<PaymentDetails>> BindPaymentStrawManAsync(
        RequesterIdentity identity,
        string paymentId,
        string strawManAccountId,
        CancellationToken cancellationToken = default)
    {
        var authorization = await _policy.AuthorizeAdministratorAsync(identity);

        if (authorization.IsFailure)
            return OperationResult<PaymentDetails>.Failure(authorization.Errors);

        if (!authorization.IsAuthorized)
            return OperationResult<PaymentDetails>.Unauthorized(authorization.AuthorizationErrors);

        var result = await _paymentCommands.BindStrawManAsync(paymentId, strawManAccountId);

        if (result.IsFailure)
            return OperationResult<PaymentDetails>.Failure(result.Errors);

        if (result.Value is not PaymentDetails value)
            throw new InvalidOperationException();

        return OperationResult<PaymentDetails>.Success(value);
    }

    public async Task<IOperationResult<StrawManSettingsDetails>> UpsertStrawManSettingsAsync(
        RequesterIdentity identity,
        string strawManId,
        decimal movementFeePercentage,
        CancellationToken cancellationToken = default)
    {
        var authorization = await _policy.AuthorizeAdministratorAsync(identity);

        if (authorization.IsFailure)
            return OperationResult<StrawManSettingsDetails>.Failure(authorization.Errors);

        if (!authorization.IsAuthorized)
            return OperationResult<StrawManSettingsDetails>.Unauthorized(authorization.AuthorizationErrors);

        var result = await _strawManSettings.UpsertStrawManSettingsAsync(
                identity,
                strawManId,
                movementFeePercentage);

        if (result.IsFailure)
            return OperationResult<StrawManSettingsDetails>.Failure(result.Errors);

        if (result.Value is not StrawManSettingsDetails value)
            throw new InvalidOperationException();

        return OperationResult<StrawManSettingsDetails>.Success(value);
    }
}

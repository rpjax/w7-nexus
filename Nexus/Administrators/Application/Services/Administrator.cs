using Aidan.Core.Patterns;
using Nexus.Administrators.Application.Contracts;
using Nexus.Administrators.Application.Requests;
using Nexus.Administrators.Application.Responses;
using Nexus.Administrators.Application.Responses.Models;
using Nexus.Authorization.Application.Models;
using Nexus.StrawMen.Application.Contracts;
using Nexus.AccountNodes.Aggregates;
using Nexus.AccountNodes.Application.Contracts;
using Nexus.Transfers.Application.Contracts;
using Nexus.Transfers.Application.Models;
using Nexus.Transfers.Aggregates;

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
    private IAdministratorAccountNodeCommandService _accountNodes { get; }
    private IAdministratorTransferCommandService _transfers { get; }
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
        IAdministratorAccountNodeCommandService accountNodes,
        IAdministratorTransferCommandService transfers,
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
        _accountNodes = accountNodes;
        _transfers = transfers;
        _paymentSearch = paymentSearch;
        _paymentCommands = paymentCommands;
        _strawManSettings = strawManSettings;
    }

    public Task<IOperationResult<OperationDetails>> CreateOperationAsync(
        RequesterIdentity identity,
        CreateOperationRequest request,
        CancellationToken cancellationToken = default)
    {
        return ExecuteAsync(
            identity,
            _ => _policy.AuthorizeAdministratorAsync(identity),
            () => _operationCommands.CreateOperationAsync(request),
            cancellationToken);
    }

    public Task<IOperationResult<SearchOperationsResponse>> SearchOperationsAsync(
        RequesterIdentity identity,
        SearchOperationsRequest request,
        CancellationToken cancellationToken = default)
    {
        return ExecuteAsync(
            identity,
            _ => _policy.AuthorizeAdministratorAsync(identity),
            () => _operationSearch.SearchOperationsAsync(request),
            cancellationToken);
    }

    public Task<IOperationResult<DeleteOperationResponse>> DeleteOperationAsync(
        RequesterIdentity identity,
        DeleteOperationRequest request,
        CancellationToken cancellationToken = default)
    {
        return ExecuteAsync(
            identity,
            _ => _policy.AuthorizeAdministratorAsync(identity),
            () => _operationCommands.DeleteOperationAsync(request),
            cancellationToken);
    }

    public Task<IOperationResult<AssignOperationAdministratorResponse>> AssignOperationAdministratorAsync(
        RequesterIdentity identity,
        AssignOperationAdministratorRequest request,
        CancellationToken cancellationToken = default)
    {
        return ExecuteAsync(
            identity,
            _ => _policy.AuthorizeAdministratorAsync(identity),
            () => _operationCommands.AssignOperationAdministratorAsync(request),
            cancellationToken);
    }

    public Task<IOperationResult<UnassignOperationAdministratorResponse>> UnassignOperationAdministratorAsync(
        RequesterIdentity identity,
        UnassignOperationAdministratorRequest request,
        CancellationToken cancellationToken = default)
    {
        return ExecuteAsync(
            identity,
            _ => _policy.AuthorizeAdministratorAsync(identity),
            () => _operationCommands.UnassignOperationAdministratorAsync(request),
            cancellationToken);
    }

    public Task<IOperationResult<SetOperationGatewaySelectionStrategyResponse>> SetOperationGatewaySelectionStrategyAsync(
        RequesterIdentity identity,
        SetOperationGatewaySelectionStrategyRequest request,
        CancellationToken cancellationToken = default)
    {
        return ExecuteAsync(
            identity,
            _ => _policy.AuthorizeAdministratorAsync(identity),
            () => _operationCommands.SetOperationGatewaySelectionStrategyAsync(request),
            cancellationToken);
    }

    public Task<IOperationResult<AssignStrawManToOperationResponse>> AssignStrawManToOperationAsync(
        RequesterIdentity identity,
        AssignStrawManToOperationRequest request,
        CancellationToken cancellationToken = default)
    {
        return ExecuteAsync(
            identity,
            _ => _policy.AuthorizeAdministratorAsync(identity),
            () => _operationCommands.AssignStrawManToOperationAsync(request),
            cancellationToken);
    }

    public Task<IOperationResult<UnassignStrawManFromOperationResponse>> UnassignStrawManFromOperationAsync(
        RequesterIdentity identity,
        UnassignStrawManFromOperationRequest request,
        CancellationToken cancellationToken = default)
    {
        return ExecuteAsync(
            identity,
            _ => _policy.AuthorizeAdministratorAsync(identity),
            () => _operationCommands.UnassignStrawManFromOperationAsync(request),
            cancellationToken);
    }

    public Task<IOperationResult<AssignGatewayAccountGroupToOperationResponse>> AssignGatewayAccountGroupToOperationAsync(
        RequesterIdentity identity,
        AssignGatewayAccountGroupToOperationRequest request,
        CancellationToken cancellationToken = default)
    {
        return ExecuteAsync(
            identity,
            _ => _policy.AuthorizeAdministratorAsync(identity),
            () => _operationCommands.AssignGatewayAccountGroupToOperationAsync(request),
            cancellationToken);
    }

    public Task<IOperationResult<UnassignGatewayAccountGroupFromOperationResponse>> UnassignGatewayAccountGroupFromOperationAsync(
        RequesterIdentity identity,
        UnassignGatewayAccountGroupFromOperationRequest request,
        CancellationToken cancellationToken = default)
    {
        return ExecuteAsync(
            identity,
            _ => _policy.AuthorizeAdministratorAsync(identity),
            () => _operationCommands.UnassignGatewayAccountGroupFromOperationAsync(request),
            cancellationToken);
    }

    public Task<IOperationResult<AssignGatewayAccountToOperationResponse>> AssignGatewayAccountToOperationAsync(
        RequesterIdentity identity,
        AssignGatewayAccountToOperationRequest request,
        CancellationToken cancellationToken = default)
    {
        return ExecuteAsync(
            identity,
            _ => _policy.AuthorizeAdministratorAsync(identity),
            () => _operationCommands.AssignGatewayAccountToOperationAsync(request),
            cancellationToken);
    }

    public Task<IOperationResult<UnassignGatewayAccountFromOperationResponse>> UnassignGatewayAccountFromOperationAsync(
        RequesterIdentity identity,
        UnassignGatewayAccountFromOperationRequest request,
        CancellationToken cancellationToken = default)
    {
        return ExecuteAsync(
            identity,
            _ => _policy.AuthorizeAdministratorAsync(identity),
            () => _operationCommands.UnassignGatewayAccountFromOperationAsync(request),
            cancellationToken);
    }

    public Task<IOperationResult<SearchAccountsResponse>> SearchAccountsAsync(
        RequesterIdentity identity,
        SearchAccountsRequest request,
        CancellationToken cancellationToken = default)
    {
        return ExecuteAsync(
            identity,
            _ => _policy.AuthorizeAdministratorAsync(identity),
            () => _accountSearch.SearchAccountsAsync(request),
            cancellationToken);
    }

    public Task<IOperationResult<GrantAccountRoleResponse>> GrantAccountRoleAsync(
        RequesterIdentity identity,
        GrantAccountRoleRequest request,
        CancellationToken cancellationToken = default)
    {
        return ExecuteAsync(
            identity,
            _ => _policy.AuthorizeAdministratorAsync(identity),
            () => _accountCommands.GrantAccountRoleAsync(request),
            cancellationToken);
    }

    public Task<IOperationResult<RevokeAccountRoleResponse>> RevokeAccountRoleAsync(
        RequesterIdentity identity,
        RevokeAccountRoleRequest request,
        CancellationToken cancellationToken = default)
    {
        return ExecuteAsync(
            identity,
            _ => _policy.AuthorizeAdministratorAsync(identity),
            () => _accountCommands.RevokeAccountRoleAsync(request),
            cancellationToken);
    }

    public Task<IOperationResult<GrantAccountPermissionResponse>> GrantAccountPermissionAsync(
        RequesterIdentity identity,
        GrantAccountPermissionRequest request,
        CancellationToken cancellationToken = default)
    {
        return ExecuteAsync(
            identity,
            _ => _policy.AuthorizeAdministratorAsync(identity),
            () => _accountCommands.GrantAccountPermissionAsync(request),
            cancellationToken);
    }

    public Task<IOperationResult<RevokeAccountPermissionResponse>> RevokeAccountPermissionAsync(
        RequesterIdentity identity,
        RevokeAccountPermissionRequest request,
        CancellationToken cancellationToken = default)
    {
        return ExecuteAsync(
            identity,
            _ => _policy.AuthorizeAdministratorAsync(identity),
            () => _accountCommands.RevokeAccountPermissionAsync(request),
            cancellationToken);
    }

    public Task<IOperationResult<CreateOperationTeamResponse>> CreateOperationTeamAsync(
        RequesterIdentity identity,
        CreateOperationTeamRequest request,
        CancellationToken cancellationToken = default)
    {
        return ExecuteAsync(
            identity,
            _ => _policy.AuthorizeAdministratorAsync(identity),
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
            _ => _policy.AuthorizeAdministratorAsync(identity),
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
            _ => _policy.AuthorizeAdministratorAsync(identity),
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
            _ => _policy.AuthorizeAdministratorAsync(identity),
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
            _ => _policy.AuthorizeAdministratorAsync(identity),
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
            _ => _policy.AuthorizeAdministratorAsync(identity),
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
            _ => _policy.AuthorizeAdministratorAsync(identity),
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
            _ => _policy.AuthorizeAdministratorAsync(identity),
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
            _ => _policy.AuthorizeAdministratorAsync(identity),
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
            _ => _policy.AuthorizeAdministratorAsync(identity),
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
            _ => _policy.AuthorizeAdministratorAsync(identity),
            () => _teamCommands.UnassignGatewayAccountFromTeamAsync(request),
            cancellationToken);
    }

    public Task<IOperationResult<AssignOperatorToTeamResponse>> AssignOperatorToTeamAsync(
        RequesterIdentity identity,
        AssignOperatorToTeamRequest request,
        CancellationToken cancellationToken = default)
    {
        return ExecuteAsync(
            identity,
            _ => _policy.AuthorizeAdministratorAsync(identity),
            () => _teamOperatorCommands.AssignOperatorToTeamAsync(request),
            cancellationToken);
    }

    public Task<IOperationResult<UnassignOperatorFromTeamResponse>> UnassignOperatorFromTeamAsync(
        RequesterIdentity identity,
        UnassignOperatorFromTeamRequest request,
        CancellationToken cancellationToken = default)
    {
        return ExecuteAsync(
            identity,
            _ => _policy.AuthorizeAdministratorAsync(identity),
            () => _teamOperatorCommands.UnassignOperatorFromTeamAsync(request),
            cancellationToken);
    }

    public Task<IOperationResult<SetOperatorProfitShareRuleResponse>> SetOperatorProfitShareRuleAsync(
        RequesterIdentity identity,
        SetOperatorProfitShareRuleRequest request,
        CancellationToken cancellationToken = default)
    {
        return ExecuteAsync(
            identity,
            _ => _policy.AuthorizeAdministratorAsync(identity),
            () => _teamOperatorCommands.SetOperatorProfitShareRuleAsync(request),
            cancellationToken);
    }

    public Task<IOperationResult<SearchOperatorsToAssignResponse>> SearchOperatorsToAssignAsync(
        RequesterIdentity identity,
        SearchOperatorsToAssignRequest request,
        CancellationToken cancellationToken = default)
    {
        return ExecuteAsync(
            identity,
            _ => _policy.AuthorizeAdministratorAsync(identity),
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
            _ => _policy.AuthorizeAdministratorAsync(identity),
            () => _profitShareAccountSearch.SearchProfitShareAccountsToAssignAsync(request),
            cancellationToken);
    }

    public Task<IOperationResult<SearchOperationsToAssignResponse>> SearchOperationsToAssignAsync(
        RequesterIdentity identity,
        SearchOperationsToAssignRequest request,
        CancellationToken cancellationToken = default)
    {
        return ExecuteAsync(
            identity,
            _ => _policy.AuthorizeAdministratorAsync(identity),
            () => _operationPickerSearch.SearchOperationsToAssignAsync(request),
            cancellationToken);
    }

    public Task<IOperationResult<BankAccount>> CreateBankAccountAsync(
        RequesterIdentity identity,
        CreateBankAccountRequest request,
        CancellationToken cancellationToken = default)
    {
        return ExecuteAsync(
            identity,
            _ => _policy.AuthorizeAdministratorAsync(identity),
            () => _accountNodes.CreateBankAccountAsync(request),
            cancellationToken);
    }

    public Task<IOperationResult<CryptoWallet>> CreateCryptoWalletAsync(
        RequesterIdentity identity,
        CreateCryptoWalletRequest request,
        CancellationToken cancellationToken = default)
    {
        return ExecuteAsync(
            identity,
            _ => _policy.AuthorizeAdministratorAsync(identity),
            () => _accountNodes.CreateCryptoWalletAsync(request),
            cancellationToken);
    }

    public Task<IOperationResult<CryptoWallet>> UpsertCryptoWalletAddressAsync(
        RequesterIdentity identity,
        UpsertCryptoWalletAddressRequest request,
        CancellationToken cancellationToken = default)
    {
        return ExecuteAsync(
            identity,
            _ => _policy.AuthorizeAdministratorAsync(identity),
            () => _accountNodes.UpsertCryptoWalletAddressAsync(request),
            cancellationToken);
    }

    public Task<IOperationResult<BankAccount>> GetBankAccountAsync(
        RequesterIdentity identity,
        string bankAccountId,
        CancellationToken cancellationToken = default)
    {
        return ExecuteAsync(
            identity,
            _ => _policy.AuthorizeAdministratorAsync(identity),
            () => _accountNodes.GetBankAccountAsync(bankAccountId),
            cancellationToken);
    }

    public Task<IOperationResult<CryptoWallet>> GetCryptoWalletAsync(
        RequesterIdentity identity,
        string cryptoWalletId,
        CancellationToken cancellationToken = default)
    {
        return ExecuteAsync(
            identity,
            _ => _policy.AuthorizeAdministratorAsync(identity),
            () => _accountNodes.GetCryptoWalletAsync(cryptoWalletId),
            cancellationToken);
    }

    public Task<IOperationResult<Transfer>> ExecuteWithdrawalTransferAsync(
        RequesterIdentity identity,
        WithdrawalTransferRequest request,
        CancellationToken cancellationToken = default)
    {
        return ExecuteAsync(
            identity,
            _ => _policy.AuthorizeAdministratorAsync(identity),
            () => _transfers.ExecuteWithdrawalAsync(request, cancellationToken),
            cancellationToken);
    }

    public Task<IOperationResult<Transfer>> ExecuteMovementTransferAsync(
        RequesterIdentity identity,
        MovementTransferRequest request,
        CancellationToken cancellationToken = default)
    {
        return ExecuteAsync(
            identity,
            _ => _policy.AuthorizeAdministratorAsync(identity),
            () => _transfers.ExecuteMovementAsync(request, cancellationToken),
            cancellationToken);
    }

    public Task<IOperationResult<Transfer>> ExecutePayoutTransferAsync(
        RequesterIdentity identity,
        PayoutTransferRequest request,
        CancellationToken cancellationToken = default)
    {
        return ExecuteAsync(
            identity,
            _ => _policy.AuthorizeAdministratorAsync(identity),
            () => _transfers.ExecutePayoutAsync(request, cancellationToken),
            cancellationToken);
    }

    public Task<IOperationResult<Transfer>> GetTransferAsync(
        RequesterIdentity identity,
        string transferId,
        CancellationToken cancellationToken = default)
    {
        return ExecuteAsync(
            identity,
            _ => _policy.AuthorizeAdministratorAsync(identity),
            () => _transfers.GetTransferAsync(transferId),
            cancellationToken);
    }

    public Task<IOperationResult<TransferTimelineDetails>> GetTransferTimelineAsync(
        RequesterIdentity identity,
        string transferId,
        CancellationToken cancellationToken = default)
    {
        return ExecuteAsync(
            identity,
            _ => _policy.AuthorizeAdministratorAsync(identity),
            () => _transfers.GetTransferTimelineAsync(transferId),
            cancellationToken);
    }

    public Task<IOperationResult<SearchTransfersResponse>> SearchTransfersAsync(
        RequesterIdentity identity,
        SearchTransfersRequest? request,
        CancellationToken cancellationToken = default)
    {
        return ExecuteAsync(
            identity,
            _ => _policy.AuthorizeAdministratorAsync(identity),
            () => _transfers.SearchTransfersAsync(request),
            cancellationToken);
    }

    public Task<IOperationResult<SearchBankAccountsResponse>> SearchBankAccountsAsync(
        RequesterIdentity identity,
        SearchBankAccountsRequest? request,
        CancellationToken cancellationToken = default)
    {
        return ExecuteAsync(
            identity,
            _ => _policy.AuthorizeAdministratorAsync(identity),
            () => _accountNodes.SearchBankAccountsAsync(request),
            cancellationToken);
    }

    public Task<IOperationResult<BankAccount>> UpdateBankAccountLabelAsync(
        RequesterIdentity identity,
        string bankAccountId,
        string? label,
        CancellationToken cancellationToken = default)
    {
        return ExecuteAsync(
            identity,
            _ => _policy.AuthorizeAdministratorAsync(identity),
            () => _accountNodes.UpdateBankAccountLabelAsync(bankAccountId, label),
            cancellationToken);
    }

    public Task<IOperationResult<SearchCryptoWalletsResponse>> SearchCryptoWalletsAsync(
        RequesterIdentity identity,
        SearchCryptoWalletsRequest? request,
        CancellationToken cancellationToken = default)
    {
        return ExecuteAsync(
            identity,
            _ => _policy.AuthorizeAdministratorAsync(identity),
            () => _accountNodes.SearchCryptoWalletsAsync(request),
            cancellationToken);
    }

    public Task<IOperationResult<Payments.Application.Models.SearchPaymentsResponse>> SearchPaymentsAsync(
        RequesterIdentity identity,
        Payments.Application.Models.SearchPaymentsRequest request,
        CancellationToken cancellationToken = default)
    {
        return ExecuteAsync(
            identity,
            _ => _policy.AuthorizeAdministratorAsync(identity),
            () => _paymentSearch.SearchPaymentsAsync(request),
            cancellationToken);
    }

    public Task<IOperationResult<Payments.Application.Models.PaymentDetails>> GetPaymentAsync(
        RequesterIdentity identity,
        string paymentId,
        CancellationToken cancellationToken = default)
    {
        return ExecuteAsync(
            identity,
            _ => _policy.AuthorizeAdministratorAsync(identity),
            () => _paymentSearch.GetPaymentAsync(paymentId),
            cancellationToken);
    }

    public Task<IOperationResult<Payments.Application.Models.PaymentDetails>> PayPaymentAsync(
        RequesterIdentity identity,
        string paymentId,
        CancellationToken cancellationToken = default)
    {
        return ExecuteAsync(
            identity,
            _ => _policy.AuthorizeAdministratorAsync(identity),
            () => _paymentCommands.PayAndGetAsync(paymentId),
            cancellationToken);
    }

    public Task<IOperationResult<Payments.Application.Models.PaymentDetails>> RefundPaymentAsync(
        RequesterIdentity identity,
        string paymentId,
        CancellationToken cancellationToken = default)
    {
        return ExecuteAsync(
            identity,
            _ => _policy.AuthorizeAdministratorAsync(identity),
            () => _paymentCommands.RefundAndGetAsync(paymentId),
            cancellationToken);
    }

    public Task<IOperationResult<Payments.Application.Models.PaymentDetails>> KillPaymentAsync(
        RequesterIdentity identity,
        string paymentId,
        string reason,
        CancellationToken cancellationToken = default)
    {
        return ExecuteAsync(
            identity,
            _ => _policy.AuthorizeAdministratorAsync(identity),
            () => _paymentCommands.KillAndGetAsync(paymentId, reason),
            cancellationToken);
    }

    public Task<IOperationResult<bool>> DeletePaymentAsync(
        RequesterIdentity identity,
        string paymentId,
        CancellationToken cancellationToken = default)
    {
        return ExecuteAsync<bool>(
            identity,
            _ => _policy.AuthorizeAdministratorAsync(identity),
            async () =>
            {
                var deleteResult = await _paymentCommands.DeletePaymentAsync(paymentId);
                if (deleteResult.IsFailure)
                    return Result<bool>.Failure(deleteResult.Errors);

                return Result<bool>.Success(true);
            },
            cancellationToken);
    }

    public Task<IOperationResult<Payments.Application.Models.PaymentDetails>> BindPaymentOperatorAsync(
        RequesterIdentity identity,
        string paymentId,
        string operatorAccountId,
        CancellationToken cancellationToken = default)
    {
        return ExecuteAsync(
            identity,
            _ => _policy.AuthorizeAdministratorAsync(identity),
            () => _paymentCommands.BindOperatorAsync(paymentId, operatorAccountId),
            cancellationToken);
    }

    public Task<IOperationResult<Payments.Application.Models.PaymentDetails>> BindPaymentStrawManAsync(
        RequesterIdentity identity,
        string paymentId,
        string strawManAccountId,
        CancellationToken cancellationToken = default)
    {
        return ExecuteAsync(
            identity,
            _ => _policy.AuthorizeAdministratorAsync(identity),
            () => _paymentCommands.BindStrawManAsync(paymentId, strawManAccountId),
            cancellationToken);
    }

    public Task<IOperationResult<StrawManSettingsDetails>> UpsertStrawManSettingsAsync(
        RequesterIdentity identity,
        string strawManId,
        decimal movementFeePercentage,
        CancellationToken cancellationToken = default)
    {
        return ExecuteAsync(
            identity,
            _ => _policy.AuthorizeAdministratorAsync(identity),
            () => _strawManSettings.UpsertStrawManSettingsAsync(
                identity,
                strawManId,
                movementFeePercentage),
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

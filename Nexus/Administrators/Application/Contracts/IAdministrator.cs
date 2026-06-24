using Aidan.Core.Patterns;
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

namespace Nexus.Administrators.Application.Contracts;

public interface IAdministrator
{
    Task<IOperationResult<OperationDetails>> CreateOperationAsync(
        RequesterIdentity identity,
        CreateOperationRequest request,
        CancellationToken cancellationToken = default);

    Task<IOperationResult<SearchOperationsResponse>> SearchOperationsAsync(
        RequesterIdentity identity,
        SearchOperationsRequest request,
        CancellationToken cancellationToken = default);

    Task<IOperationResult<DeleteOperationResponse>> DeleteOperationAsync(
        RequesterIdentity identity,
        DeleteOperationRequest request,
        CancellationToken cancellationToken = default);

    Task<IOperationResult<AssignOperationAdministratorResponse>> AssignOperationAdministratorAsync(
        RequesterIdentity identity,
        AssignOperationAdministratorRequest request,
        CancellationToken cancellationToken = default);

    Task<IOperationResult<UnassignOperationAdministratorResponse>> UnassignOperationAdministratorAsync(
        RequesterIdentity identity,
        UnassignOperationAdministratorRequest request,
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

    Task<IOperationResult<SearchAccountsResponse>> SearchAccountsAsync(
        RequesterIdentity identity,
        SearchAccountsRequest request,
        CancellationToken cancellationToken = default);

    Task<IOperationResult<GrantAccountRoleResponse>> GrantAccountRoleAsync(
        RequesterIdentity identity,
        GrantAccountRoleRequest request,
        CancellationToken cancellationToken = default);

    Task<IOperationResult<RevokeAccountRoleResponse>> RevokeAccountRoleAsync(
        RequesterIdentity identity,
        RevokeAccountRoleRequest request,
        CancellationToken cancellationToken = default);

    Task<IOperationResult<GrantAccountPermissionResponse>> GrantAccountPermissionAsync(
        RequesterIdentity identity,
        GrantAccountPermissionRequest request,
        CancellationToken cancellationToken = default);

    Task<IOperationResult<RevokeAccountPermissionResponse>> RevokeAccountPermissionAsync(
        RequesterIdentity identity,
        RevokeAccountPermissionRequest request,
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

    Task<IOperationResult<SearchOperationsToAssignResponse>> SearchOperationsToAssignAsync(
        RequesterIdentity identity,
        SearchOperationsToAssignRequest request,
        CancellationToken cancellationToken = default);

    Task<IOperationResult<BankAccount>> CreateBankAccountAsync(
        RequesterIdentity identity,
        CreateBankAccountRequest request,
        CancellationToken cancellationToken = default);

    Task<IOperationResult<CryptoWallet>> CreateCryptoWalletAsync(
        RequesterIdentity identity,
        CreateCryptoWalletRequest request,
        CancellationToken cancellationToken = default);

    Task<IOperationResult<CryptoWallet>> UpsertCryptoWalletAddressAsync(
        RequesterIdentity identity,
        UpsertCryptoWalletAddressRequest request,
        CancellationToken cancellationToken = default);

    Task<IOperationResult<BankAccount>> GetBankAccountAsync(
        RequesterIdentity identity,
        string bankAccountId,
        CancellationToken cancellationToken = default);

    Task<IOperationResult<CryptoWallet>> GetCryptoWalletAsync(
        RequesterIdentity identity,
        string cryptoWalletId,
        CancellationToken cancellationToken = default);

    Task<IOperationResult<Transfer>> ExecuteWithdrawalTransferAsync(
        RequesterIdentity identity,
        WithdrawalTransferRequest request,
        CancellationToken cancellationToken = default);

    Task<IOperationResult<Transfer>> ExecuteMovementTransferAsync(
        RequesterIdentity identity,
        MovementTransferRequest request,
        CancellationToken cancellationToken = default);

    Task<IOperationResult<Transfer>> ExecutePayoutTransferAsync(
        RequesterIdentity identity,
        PayoutTransferRequest request,
        CancellationToken cancellationToken = default);

    Task<IOperationResult<Transfer>> GetTransferAsync(
        RequesterIdentity identity,
        string transferId,
        CancellationToken cancellationToken = default);

    Task<IOperationResult<TransferTimelineDetails>> GetTransferTimelineAsync(
        RequesterIdentity identity,
        string transferId,
        CancellationToken cancellationToken = default);

    Task<IOperationResult<SearchTransfersResponse>> SearchTransfersAsync(
        RequesterIdentity identity,
        SearchTransfersRequest? request,
        CancellationToken cancellationToken = default);

    Task<IOperationResult<SearchBankAccountsResponse>> SearchBankAccountsAsync(
        RequesterIdentity identity,
        SearchBankAccountsRequest? request,
        CancellationToken cancellationToken = default);

    Task<IOperationResult<BankAccount>> UpdateBankAccountLabelAsync(
        RequesterIdentity identity,
        string bankAccountId,
        string? label,
        CancellationToken cancellationToken = default);

    Task<IOperationResult<SearchCryptoWalletsResponse>> SearchCryptoWalletsAsync(
        RequesterIdentity identity,
        SearchCryptoWalletsRequest? request,
        CancellationToken cancellationToken = default);

    Task<IOperationResult<Payments.Application.Models.SearchPaymentsResponse>> SearchPaymentsAsync(
        RequesterIdentity identity,
        Payments.Application.Models.SearchPaymentsRequest request,
        CancellationToken cancellationToken = default);

    Task<IOperationResult<Payments.Application.Models.PaymentDetails>> GetPaymentAsync(
        RequesterIdentity identity,
        string paymentId,
        CancellationToken cancellationToken = default);

    Task<IOperationResult<Payments.Application.Models.PaymentDetails>> PayPaymentAsync(
        RequesterIdentity identity,
        string paymentId,
        CancellationToken cancellationToken = default);

    Task<IOperationResult<Payments.Application.Models.PaymentDetails>> RefundPaymentAsync(
        RequesterIdentity identity,
        string paymentId,
        CancellationToken cancellationToken = default);

    Task<IOperationResult<Payments.Application.Models.PaymentDetails>> KillPaymentAsync(
        RequesterIdentity identity,
        string paymentId,
        string reason,
        CancellationToken cancellationToken = default);

    Task<IOperationResult<bool>> DeletePaymentAsync(
        RequesterIdentity identity,
        string paymentId,
        CancellationToken cancellationToken = default);

    Task<IOperationResult<Payments.Application.Models.PaymentDetails>> BindPaymentOperatorAsync(
        RequesterIdentity identity,
        string paymentId,
        string operatorId,
        CancellationToken cancellationToken = default);

    Task<IOperationResult<Payments.Application.Models.PaymentDetails>> BindPaymentStrawManAsync(
        RequesterIdentity identity,
        string paymentId,
        string strawManId,
        CancellationToken cancellationToken = default);

    Task<IOperationResult<StrawManSettingsDetails>> UpsertStrawManSettingsAsync(
        RequesterIdentity identity,
        string strawManId,
        decimal movementFeePercentage,
        CancellationToken cancellationToken = default);
}

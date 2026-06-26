using Aidan.Core.Linq.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nexus.Administrators.Application.Contracts;
using Nexus.Administrators.Application.Requests;
using Nexus.Authorization.Application.Contracts;
using Nexus.BankAccounts.Aggregates;
using Nexus.BankAccounts.Application.Contracts;
using Nexus.BankAccounts.Application.Requests;
using Nexus.BankAccounts.Presentation;
using Nexus.Controllers;
using Nexus.CryptoWallets.Aggregates;
using Nexus.CryptoWallets.Application.Contracts;
using Nexus.CryptoWallets.Application.Requests;
using Nexus.CryptoWallets.Presentation;
using Nexus.StrawMen.Application.Contracts;
using Nexus.Transfers.Application.Models;
using Nexus.Transfers.Application.Requests;
using Nexus.Transfers.Presentation;

namespace Nexus.Administrators.Presentation;

[Route("api/administrator")]
[Authorize]
public class AdministratorController : NexusController
{
    private IAdministrator _administrator { get; }
    private IRequesterIdentityResolver _identityResolver { get; }
    private IBankBalanceRepository _bankBalances { get; }
    private ICryptoBalanceRepository _cryptoBalances { get; }

    public AdministratorController(
        IAdministrator administrator,
        IRequesterIdentityResolver identityResolver,
        IBankBalanceRepository bankBalances,
        ICryptoBalanceRepository cryptoBalances)
    {
        _administrator = administrator;
        _identityResolver = identityResolver;
        _bankBalances = bankBalances;
        _cryptoBalances = cryptoBalances;
    }

    [HttpPost("operations")]
    public async Task<ActionResult> CreateOperationAsync(
        [FromBody] CreateOperationRequest request,
        CancellationToken cancellationToken)
    {
        var identity = await ResolveIdentityAsync(_identityResolver, cancellationToken);

        return ToOperationResult(await _administrator.CreateOperationAsync(
            identity,
            request,
            cancellationToken));
    }

    [HttpPost("operations/search")]
    public async Task<ActionResult> SearchOperationsAsync(
        [FromBody] SearchOperationsRequest request,
        CancellationToken cancellationToken)
    {
        var identity = await ResolveIdentityAsync(_identityResolver, cancellationToken);

        return ToOperationResult(await _administrator.SearchOperationsAsync(
            identity,
            request,
            cancellationToken));
    }

    [HttpPost("accounts/search")]
    public async Task<ActionResult> SearchAccountsAsync(
        [FromBody] SearchAccountsRequest request,
        CancellationToken cancellationToken)
    {
        var identity = await ResolveIdentityAsync(_identityResolver, cancellationToken);

        return ToOperationResult(await _administrator.SearchAccountsAsync(
            identity,
            request,
            cancellationToken));
    }

    [HttpPost("accounts/roles")]
    public async Task<ActionResult> GrantAccountRoleAsync(
        [FromBody] GrantAccountRoleRequest request,
        CancellationToken cancellationToken)
    {
        var identity = await ResolveIdentityAsync(_identityResolver, cancellationToken);

        return ToOperationResult(await _administrator.GrantAccountRoleAsync(
            identity,
            request,
            cancellationToken));
    }

    [HttpDelete("accounts/roles")]
    public async Task<ActionResult> RevokeAccountRoleAsync(
        [FromBody] RevokeAccountRoleRequest request,
        CancellationToken cancellationToken)
    {
        var identity = await ResolveIdentityAsync(_identityResolver, cancellationToken);

        return ToOperationResult(await _administrator.RevokeAccountRoleAsync(
            identity,
            request,
            cancellationToken));
    }

    [HttpPost("accounts/permissions")]
    public async Task<ActionResult> GrantAccountPermissionAsync(
        [FromBody] GrantAccountPermissionRequest request,
        CancellationToken cancellationToken)
    {
        var identity = await ResolveIdentityAsync(_identityResolver, cancellationToken);

        return ToOperationResult(await _administrator.GrantAccountPermissionAsync(
            identity,
            request,
            cancellationToken));
    }

    [HttpDelete("accounts/permissions")]
    public async Task<ActionResult> RevokeAccountPermissionAsync(
        [FromBody] RevokeAccountPermissionRequest request,
        CancellationToken cancellationToken)
    {
        var identity = await ResolveIdentityAsync(_identityResolver, cancellationToken);

        return ToOperationResult(await _administrator.RevokeAccountPermissionAsync(
            identity,
            request,
            cancellationToken));
    }

    [HttpPost("teams/operators/search")]
    public async Task<ActionResult> SearchOperatorsToAssignAsync(
        [FromBody] SearchOperatorsToAssignRequest request,
        CancellationToken cancellationToken)
    {
        var identity = await ResolveIdentityAsync(_identityResolver, cancellationToken);

        return ToOperationResult(await _administrator.SearchOperatorsToAssignAsync(
            identity,
            request,
            cancellationToken));
    }

    [HttpPost("teams/profit-share-accounts/search")]
    public async Task<ActionResult> SearchProfitShareAccountsToAssignAsync(
        [FromBody] SearchProfitShareAccountsToAssignRequest request,
        CancellationToken cancellationToken)
    {
        var identity = await ResolveIdentityAsync(_identityResolver, cancellationToken);

        return ToOperationResult(await _administrator.SearchProfitShareAccountsToAssignAsync(
            identity,
            request,
            cancellationToken));
    }

    [HttpPost("operations/to-assign/search")]
    public async Task<ActionResult> SearchOperationsToAssignAsync(
        [FromBody] SearchOperationsToAssignRequest request,
        CancellationToken cancellationToken)
    {
        var identity = await ResolveIdentityAsync(_identityResolver, cancellationToken);

        return ToOperationResult(await _administrator.SearchOperationsToAssignAsync(
            identity,
            request,
            cancellationToken));
    }

    [HttpDelete("operations")]
    public async Task<ActionResult> DeleteOperationAsync(
        [FromBody] DeleteOperationRequest request,
        CancellationToken cancellationToken)
    {
        var identity = await ResolveIdentityAsync(_identityResolver, cancellationToken);

        return ToOperationResult(await _administrator.DeleteOperationAsync(
            identity,
            request,
            cancellationToken));
    }

    [HttpPost("operations/administrators")]
    public async Task<ActionResult> AssignOperationAdministratorAsync(
        [FromBody] AssignOperationAdministratorRequest request,
        CancellationToken cancellationToken)
    {
        var identity = await ResolveIdentityAsync(_identityResolver, cancellationToken);

        return ToOperationResult(await _administrator.AssignOperationAdministratorAsync(
            identity,
            request,
            cancellationToken));
    }

    [HttpDelete("operations/administrators")]
    public async Task<ActionResult> UnassignOperationAdministratorAsync(
        [FromBody] UnassignOperationAdministratorRequest request,
        CancellationToken cancellationToken)
    {
        var identity = await ResolveIdentityAsync(_identityResolver, cancellationToken);

        return ToOperationResult(await _administrator.UnassignOperationAdministratorAsync(
            identity,
            request,
            cancellationToken));
    }

    [HttpPut("operations/gateway-selection-strategy")]
    public async Task<ActionResult> SetOperationGatewaySelectionStrategyAsync(
        [FromBody] SetOperationGatewaySelectionStrategyRequest request,
        CancellationToken cancellationToken)
    {
        var identity = await ResolveIdentityAsync(_identityResolver, cancellationToken);

        return ToOperationResult(await _administrator.SetOperationGatewaySelectionStrategyAsync(
            identity,
            request,
            cancellationToken));
    }

    [HttpPost("operations/straw-men")]
    public async Task<ActionResult> AssignStrawManToOperationAsync(
        [FromBody] AssignStrawManToOperationRequest request,
        CancellationToken cancellationToken)
    {
        var identity = await ResolveIdentityAsync(_identityResolver, cancellationToken);

        return ToOperationResult(await _administrator.AssignStrawManToOperationAsync(
            identity,
            request,
            cancellationToken));
    }

    [HttpDelete("operations/straw-men")]
    public async Task<ActionResult> UnassignStrawManFromOperationAsync(
        [FromBody] UnassignStrawManFromOperationRequest request,
        CancellationToken cancellationToken)
    {
        var identity = await ResolveIdentityAsync(_identityResolver, cancellationToken);

        return ToOperationResult(await _administrator.UnassignStrawManFromOperationAsync(
            identity,
            request,
            cancellationToken));
    }

    [HttpPost("operations/gateway-account-groups")]
    public async Task<ActionResult> AssignGatewayAccountGroupToOperationAsync(
        [FromBody] AssignGatewayAccountGroupToOperationRequest request,
        CancellationToken cancellationToken)
    {
        var identity = await ResolveIdentityAsync(_identityResolver, cancellationToken);

        return ToOperationResult(await _administrator.AssignGatewayAccountGroupToOperationAsync(
            identity,
            request,
            cancellationToken));
    }

    [HttpDelete("operations/gateway-account-groups")]
    public async Task<ActionResult> UnassignGatewayAccountGroupFromOperationAsync(
        [FromBody] UnassignGatewayAccountGroupFromOperationRequest request,
        CancellationToken cancellationToken)
    {
        var identity = await ResolveIdentityAsync(_identityResolver, cancellationToken);

        return ToOperationResult(await _administrator.UnassignGatewayAccountGroupFromOperationAsync(
            identity,
            request,
            cancellationToken));
    }

    [HttpPost("operations/gateway-accounts")]
    public async Task<ActionResult> AssignGatewayAccountToOperationAsync(
        [FromBody] AssignGatewayAccountToOperationRequest request,
        CancellationToken cancellationToken)
    {
        var identity = await ResolveIdentityAsync(_identityResolver, cancellationToken);

        return ToOperationResult(await _administrator.AssignGatewayAccountToOperationAsync(
            identity,
            request,
            cancellationToken));
    }

    [HttpDelete("operations/gateway-accounts")]
    public async Task<ActionResult> UnassignGatewayAccountFromOperationAsync(
        [FromBody] UnassignGatewayAccountFromOperationRequest request,
        CancellationToken cancellationToken)
    {
        var identity = await ResolveIdentityAsync(_identityResolver, cancellationToken);

        return ToOperationResult(await _administrator.UnassignGatewayAccountFromOperationAsync(
            identity,
            request,
            cancellationToken));
    }

    [HttpPost("teams")]
    public async Task<ActionResult> CreateOperationTeamAsync(
        [FromBody] CreateOperationTeamRequest request,
        CancellationToken cancellationToken)
    {
        var identity = await ResolveIdentityAsync(_identityResolver, cancellationToken);

        return ToOperationResult(await _administrator.CreateOperationTeamAsync(
            identity,
            request,
            cancellationToken));
    }

    [HttpDelete("teams")]
    public async Task<ActionResult> DeleteOperationTeamAsync(
        [FromBody] DeleteOperationTeamRequest request,
        CancellationToken cancellationToken)
    {
        var identity = await ResolveIdentityAsync(_identityResolver, cancellationToken);

        return ToOperationResult(await _administrator.DeleteOperationTeamAsync(
            identity,
            request,
            cancellationToken));
    }

    [HttpPost("teams/leaders")]
    public async Task<ActionResult> AssignOperationTeamLeaderAsync(
        [FromBody] AssignOperationTeamLeaderRequest request,
        CancellationToken cancellationToken)
    {
        var identity = await ResolveIdentityAsync(_identityResolver, cancellationToken);

        return ToOperationResult(await _administrator.AssignOperationTeamLeaderAsync(
            identity,
            request,
            cancellationToken));
    }

    [HttpDelete("teams/leaders")]
    public async Task<ActionResult> UnassignOperationTeamLeaderAsync(
        [FromBody] UnassignOperationTeamLeaderRequest request,
        CancellationToken cancellationToken)
    {
        var identity = await ResolveIdentityAsync(_identityResolver, cancellationToken);

        return ToOperationResult(await _administrator.UnassignOperationTeamLeaderAsync(
            identity,
            request,
            cancellationToken));
    }

    [HttpPut("teams/gateway-selection-strategy")]
    public async Task<ActionResult> SetTeamGatewaySelectionStrategyAsync(
        [FromBody] SetTeamGatewaySelectionStrategyRequest request,
        CancellationToken cancellationToken)
    {
        var identity = await ResolveIdentityAsync(_identityResolver, cancellationToken);

        return ToOperationResult(await _administrator.SetTeamGatewaySelectionStrategyAsync(
            identity,
            request,
            cancellationToken));
    }

    [HttpPost("teams/straw-men")]
    public async Task<ActionResult> AssignStrawManToTeamAsync(
        [FromBody] AssignStrawManToTeamRequest request,
        CancellationToken cancellationToken)
    {
        var identity = await ResolveIdentityAsync(_identityResolver, cancellationToken);

        return ToOperationResult(await _administrator.AssignStrawManToTeamAsync(
            identity,
            request,
            cancellationToken));
    }

    [HttpDelete("teams/straw-men")]
    public async Task<ActionResult> UnassignStrawManFromTeamAsync(
        [FromBody] UnassignStrawManFromTeamRequest request,
        CancellationToken cancellationToken)
    {
        var identity = await ResolveIdentityAsync(_identityResolver, cancellationToken);

        return ToOperationResult(await _administrator.UnassignStrawManFromTeamAsync(
            identity,
            request,
            cancellationToken));
    }

    [HttpPost("teams/gateway-account-groups")]
    public async Task<ActionResult> AssignGatewayAccountGroupToTeamAsync(
        [FromBody] AssignGatewayAccountGroupToTeamRequest request,
        CancellationToken cancellationToken)
    {
        var identity = await ResolveIdentityAsync(_identityResolver, cancellationToken);

        return ToOperationResult(await _administrator.AssignGatewayAccountGroupToTeamAsync(
            identity,
            request,
            cancellationToken));
    }

    [HttpDelete("teams/gateway-account-groups")]
    public async Task<ActionResult> UnassignGatewayAccountGroupFromTeamAsync(
        [FromBody] UnassignGatewayAccountGroupFromTeamRequest request,
        CancellationToken cancellationToken)
    {
        var identity = await ResolveIdentityAsync(_identityResolver, cancellationToken);

        return ToOperationResult(await _administrator.UnassignGatewayAccountGroupFromTeamAsync(
            identity,
            request,
            cancellationToken));
    }

    [HttpPost("teams/gateway-accounts")]
    public async Task<ActionResult> AssignGatewayAccountToTeamAsync(
        [FromBody] AssignGatewayAccountToTeamRequest request,
        CancellationToken cancellationToken)
    {
        var identity = await ResolveIdentityAsync(_identityResolver, cancellationToken);

        return ToOperationResult(await _administrator.AssignGatewayAccountToTeamAsync(
            identity,
            request,
            cancellationToken));
    }

    [HttpDelete("teams/gateway-accounts")]
    public async Task<ActionResult> UnassignGatewayAccountFromTeamAsync(
        [FromBody] UnassignGatewayAccountFromTeamRequest request,
        CancellationToken cancellationToken)
    {
        var identity = await ResolveIdentityAsync(_identityResolver, cancellationToken);

        return ToOperationResult(await _administrator.UnassignGatewayAccountFromTeamAsync(
            identity,
            request,
            cancellationToken));
    }

    [HttpPost("teams/operators")]
    public async Task<ActionResult> AssignOperatorToTeamAsync(
        [FromBody] AssignOperatorToTeamRequest request,
        CancellationToken cancellationToken)
    {
        var identity = await ResolveIdentityAsync(_identityResolver, cancellationToken);

        return ToOperationResult(await _administrator.AssignOperatorToTeamAsync(
            identity,
            request,
            cancellationToken));
    }

    [HttpDelete("teams/operators")]
    public async Task<ActionResult> UnassignOperatorFromTeamAsync(
        [FromBody] UnassignOperatorFromTeamRequest request,
        CancellationToken cancellationToken)
    {
        var identity = await ResolveIdentityAsync(_identityResolver, cancellationToken);

        return ToOperationResult(await _administrator.UnassignOperatorFromTeamAsync(
            identity,
            request,
            cancellationToken));
    }

    [HttpPut("teams/operators/profit-share-rules")]
    public async Task<ActionResult> SetOperatorProfitShareRuleAsync(
        [FromBody] SetOperatorProfitShareRuleRequest request,
        CancellationToken cancellationToken)
    {
        var identity = await ResolveIdentityAsync(_identityResolver, cancellationToken);

        return ToOperationResult(await _administrator.SetOperatorProfitShareRuleAsync(
            identity,
            request,
            cancellationToken));
    }

    [HttpPost("bank-accounts")]
    public async Task<ActionResult> CreateBankAccountAsync(
        [FromBody] CreateBankAccountRequest request,
        CancellationToken cancellationToken)
    {
        var identity = await ResolveIdentityAsync(_identityResolver, cancellationToken);
        var result = await _administrator.CreateBankAccountAsync(identity, request, cancellationToken);

        if (!result.IsAuthorized)
            return ProblemResponse(403, result.AuthorizationErrors);
        if (result.IsFailure)
            return ProblemResponse(422, result.Errors);

        return Ok(await ToBankAccountResponseAsync(result.Value!, cancellationToken));
    }

    [HttpGet("bank-accounts/{bankAccountId}")]
    public async Task<ActionResult> GetBankAccountAsync(
        string bankAccountId,
        CancellationToken cancellationToken)
    {
        var identity = await ResolveIdentityAsync(_identityResolver, cancellationToken);
        var result = await _administrator.GetBankAccountAsync(identity, bankAccountId, cancellationToken);

        if (!result.IsAuthorized)
            return ProblemResponse(403, result.AuthorizationErrors);
        if (result.IsFailure)
            return ProblemResponse(422, result.Errors);

        return Ok(await ToBankAccountResponseAsync(result.Value!, cancellationToken));
    }

    [HttpPost("crypto-wallets")]
    public async Task<ActionResult> CreateCryptoWalletAsync(
        [FromBody] CreateCryptoWalletRequest request,
        CancellationToken cancellationToken)
    {
        var identity = await ResolveIdentityAsync(_identityResolver, cancellationToken);
        var result = await _administrator.CreateCryptoWalletAsync(identity, request, cancellationToken);

        if (!result.IsAuthorized)
            return ProblemResponse(403, result.AuthorizationErrors);
        if (result.IsFailure)
            return ProblemResponse(422, result.Errors);

        return Ok(await ToCryptoWalletResponseAsync(result.Value!, cancellationToken));
    }

    [HttpPut("crypto-wallets/{cryptoWalletId}/addresses")]
    public async Task<ActionResult> UpsertCryptoWalletAddressAsync(
        string cryptoWalletId,
        [FromBody] UpsertCryptoWalletAddressBody request,
        CancellationToken cancellationToken)
    {
        var identity = await ResolveIdentityAsync(_identityResolver, cancellationToken);
        var result = await _administrator.UpsertCryptoWalletAddressAsync(
            identity,
            new UpsertCryptoWalletAddressRequest
            {
                CryptoWalletId = cryptoWalletId,
                Namespace = request.Namespace,
                Address = request.Address,
                Memo = request.Memo,
            },
            cancellationToken);

        if (!result.IsAuthorized)
            return ProblemResponse(403, result.AuthorizationErrors);
        if (result.IsFailure)
            return ProblemResponse(422, result.Errors);

        return Ok(await ToCryptoWalletResponseAsync(result.Value!, cancellationToken));
    }

    [HttpGet("crypto-wallets/{cryptoWalletId}")]
    public async Task<ActionResult> GetCryptoWalletAsync(
        string cryptoWalletId,
        CancellationToken cancellationToken)
    {
        var identity = await ResolveIdentityAsync(_identityResolver, cancellationToken);
        var result = await _administrator.GetCryptoWalletAsync(identity, cryptoWalletId, cancellationToken);

        if (!result.IsAuthorized)
            return ProblemResponse(403, result.AuthorizationErrors);
        if (result.IsFailure)
            return ProblemResponse(422, result.Errors);

        return Ok(await ToCryptoWalletResponseAsync(result.Value!, cancellationToken));
    }

    [HttpPost("bank-accounts/search")]
    public async Task<ActionResult> SearchBankAccountsAsync(
        [FromBody] SearchBankAccountsRequest? request,
        CancellationToken cancellationToken)
    {
        var identity = await ResolveIdentityAsync(_identityResolver, cancellationToken);
        var result = await _administrator.SearchBankAccountsAsync(identity, request, cancellationToken);

        if (!result.IsAuthorized)
            return ProblemResponse(403, result.AuthorizationErrors);
        if (result.IsFailure)
            return ProblemResponse(422, result.Errors);

        var data = result.Value!;
        return Ok(new
        {
            Total = data.Total,
            Items = await ToBankAccountResponsesAsync(data.Items, cancellationToken),
        });
    }

    [HttpPatch("bank-accounts/{bankAccountId}/label")]
    public async Task<ActionResult> UpdateBankAccountLabelAsync(
        string bankAccountId,
        [FromBody] UpdateBankAccountLabelRequest request,
        CancellationToken cancellationToken)
    {
        var identity = await ResolveIdentityAsync(_identityResolver, cancellationToken);
        var result = await _administrator.UpdateBankAccountLabelAsync(
            identity,
            bankAccountId,
            request?.Label,
            cancellationToken);

        if (!result.IsAuthorized)
            return ProblemResponse(403, result.AuthorizationErrors);
        if (result.IsFailure)
            return ProblemResponse(422, result.Errors);

        return Ok(await ToBankAccountResponseAsync(result.Value!, cancellationToken));
    }

    [HttpPost("crypto-wallets/search")]
    public async Task<ActionResult> SearchCryptoWalletsAsync(
        [FromBody] SearchCryptoWalletsRequest? request,
        CancellationToken cancellationToken)
    {
        var identity = await ResolveIdentityAsync(_identityResolver, cancellationToken);
        var result = await _administrator.SearchCryptoWalletsAsync(identity, request, cancellationToken);

        if (!result.IsAuthorized)
            return ProblemResponse(403, result.AuthorizationErrors);
        if (result.IsFailure)
            return ProblemResponse(422, result.Errors);

        var data = result.Value!;
        return Ok(new
        {
            Total = data.Total,
            Items = await ToCryptoWalletResponsesAsync(data.Items, cancellationToken),
        });
    }

    [HttpPost("transfers/search")]
    public async Task<ActionResult> SearchTransfersAsync(
        [FromBody] SearchTransfersRequest? request,
        CancellationToken cancellationToken)
    {
        var identity = await ResolveIdentityAsync(_identityResolver, cancellationToken);
        var result = await _administrator.SearchTransfersAsync(identity, request, cancellationToken);

        if (!result.IsAuthorized)
            return ProblemResponse(403, result.AuthorizationErrors);
        if (result.IsFailure)
            return ProblemResponse(422, result.Errors);

        var data = result.Value!;
        return Ok(new
        {
            Total = data.Total,
            Items = data.Items.Select(TransferApiMapping.ToTransferResponse).ToArray(),
        });
    }

    [HttpPost("transfers/withdrawal")]
    public async Task<ActionResult> ExecuteWithdrawalTransferAsync(
        [FromBody] WithdrawalTransferRequest request,
        CancellationToken cancellationToken)
    {
        var identity = await ResolveIdentityAsync(_identityResolver, cancellationToken);
        var result = await _administrator.ExecuteWithdrawalTransferAsync(identity, request, cancellationToken);

        if (!result.IsAuthorized)
            return ProblemResponse(403, result.AuthorizationErrors);
        if (result.IsFailure)
            return ProblemResponse(422, result.Errors);

        return Ok(TransferApiMapping.ToTransferResponse(result.Value!));
    }

    [HttpPost("transfers/bank-accounts/movement")]
    public async Task<ActionResult> ExecuteBankAccountMovementTransferAsync(
        [FromBody] BankAccountMovementRequest request,
        CancellationToken cancellationToken)
    {
        var identity = await ResolveIdentityAsync(_identityResolver, cancellationToken);
        var result = await _administrator.ExecuteBankAccountMovementTransferAsync(identity, request, cancellationToken);

        if (!result.IsAuthorized)
            return ProblemResponse(403, result.AuthorizationErrors);
        if (result.IsFailure)
            return ProblemResponse(422, result.Errors);

        return Ok(TransferApiMapping.ToTransferResponse(result.Value!));
    }

    [HttpPost("transfers/crypto-wallets/movement")]
    public async Task<ActionResult> ExecuteCryptoWalletMovementTransferAsync(
        [FromBody] CryptoWalletMovementRequest request,
        CancellationToken cancellationToken)
    {
        var identity = await ResolveIdentityAsync(_identityResolver, cancellationToken);
        var result = await _administrator.ExecuteCryptoWalletMovementTransferAsync(identity, request, cancellationToken);

        if (!result.IsAuthorized)
            return ProblemResponse(403, result.AuthorizationErrors);
        if (result.IsFailure)
            return ProblemResponse(422, result.Errors);

        return Ok(TransferApiMapping.ToTransferResponse(result.Value!));
    }

    [HttpPost("transfers/payout")]
    public async Task<ActionResult> ExecutePayoutTransferAsync(
        [FromBody] PayoutTransferRequest request,
        CancellationToken cancellationToken)
    {
        var identity = await ResolveIdentityAsync(_identityResolver, cancellationToken);
        var result = await _administrator.ExecutePayoutTransferAsync(identity, request, cancellationToken);

        if (!result.IsAuthorized)
            return ProblemResponse(403, result.AuthorizationErrors);
        if (result.IsFailure)
            return ProblemResponse(422, result.Errors);

        return Ok(TransferApiMapping.ToTransferResponse(result.Value!));
    }

    [HttpGet("transfers/{transferId}")]
    public async Task<ActionResult> GetTransferAsync(
        string transferId,
        CancellationToken cancellationToken)
    {
        var identity = await ResolveIdentityAsync(_identityResolver, cancellationToken);
        var result = await _administrator.GetTransferAsync(identity, transferId, cancellationToken);

        if (!result.IsAuthorized)
            return ProblemResponse(403, result.AuthorizationErrors);
        if (result.IsFailure)
            return ProblemResponse(422, result.Errors);

        return Ok(TransferApiMapping.ToTransferResponse(result.Value!));
    }

    [HttpGet("transfers/{transferId}/timeline")]
    public async Task<ActionResult> GetTransferTimelineAsync(
        string transferId,
        CancellationToken cancellationToken)
    {
        var identity = await ResolveIdentityAsync(_identityResolver, cancellationToken);
        var result = await _administrator.GetTransferTimelineAsync(identity, transferId, cancellationToken);

        if (!result.IsAuthorized)
            return ProblemResponse(403, result.AuthorizationErrors);
        if (result.IsFailure)
            return ProblemResponse(422, result.Errors);

        return Ok(TransferTimelineApiMapping.ToTimelineResponse(result.Value!));
    }

    [HttpPost("payments/search")]
    public async Task<ActionResult> SearchPaymentsAsync(
        [FromBody] Payments.Application.Models.SearchPaymentsRequest? request,
        CancellationToken cancellationToken)
    {
        var identity = await ResolveIdentityAsync(_identityResolver, cancellationToken);

        return ToOperationResult(await _administrator.SearchPaymentsAsync(
            identity,
            request,
            cancellationToken));
    }

    [HttpGet("payments/{paymentId}")]
    public async Task<ActionResult> GetPaymentAsync(
        string paymentId,
        CancellationToken cancellationToken)
    {
        var identity = await ResolveIdentityAsync(_identityResolver, cancellationToken);

        return ToOperationResult(await _administrator.GetPaymentAsync(
            identity,
            paymentId,
            cancellationToken));
    }

    [HttpPost("payments/{paymentId}/pay")]
    public async Task<ActionResult> PayPaymentAsync(
        string paymentId,
        CancellationToken cancellationToken)
    {
        var identity = await ResolveIdentityAsync(_identityResolver, cancellationToken);

        return ToOperationResult(await _administrator.PayPaymentAsync(
            identity,
            paymentId,
            cancellationToken));
    }

    [HttpPost("payments/{paymentId}/refund")]
    public async Task<ActionResult> RefundPaymentAsync(
        string paymentId,
        CancellationToken cancellationToken)
    {
        var identity = await ResolveIdentityAsync(_identityResolver, cancellationToken);

        return ToOperationResult(await _administrator.RefundPaymentAsync(
            identity,
            paymentId,
            cancellationToken));
    }

    [HttpPost("payments/{paymentId}/kill")]
    public async Task<ActionResult> KillPaymentAsync(
        string paymentId,
        [FromBody] Payments.Application.Models.KillPaymentRequest request,
        CancellationToken cancellationToken)
    {
        var identity = await ResolveIdentityAsync(_identityResolver, cancellationToken);

        return ToOperationResult(await _administrator.KillPaymentAsync(
            identity,
            paymentId,
            request?.Reason ?? string.Empty,
            cancellationToken));
    }

    [HttpPost("payments/{paymentId}/mark-distributed")]
    public async Task<ActionResult> MarkPaymentAsDistributedAsync(
        string paymentId,
        CancellationToken cancellationToken)
    {
        var identity = await ResolveIdentityAsync(_identityResolver, cancellationToken);

        return ToOperationResult(await _administrator.MarkPaymentAsDistributedAsync(
            identity,
            paymentId,
            cancellationToken));
    }

    [HttpDelete("payments/{paymentId}")]
    public async Task<ActionResult> DeletePaymentAsync(
        string paymentId,
        CancellationToken cancellationToken)
    {
        var identity = await ResolveIdentityAsync(_identityResolver, cancellationToken);

        return ToOperationResult(await _administrator.DeletePaymentAsync(
            identity,
            paymentId,
            cancellationToken));
    }

    [HttpPost("payments/{paymentId}/bind-operator")]
    public async Task<ActionResult> BindPaymentOperatorAsync(
        string paymentId,
        [FromBody] Payments.Application.Models.BindPaymentOperatorRequest request,
        CancellationToken cancellationToken)
    {
        var identity = await ResolveIdentityAsync(_identityResolver, cancellationToken);

        return ToOperationResult(await _administrator.BindPaymentOperatorAsync(
            identity,
            paymentId,
            request?.OperatorId ?? string.Empty,
            cancellationToken));
    }

    [HttpPost("payments/{paymentId}/bind-straw-man")]
    public async Task<ActionResult> BindPaymentStrawManAsync(
        string paymentId,
        [FromBody] Payments.Application.Models.BindPaymentStrawManRequest request,
        CancellationToken cancellationToken)
    {
        var identity = await ResolveIdentityAsync(_identityResolver, cancellationToken);

        return ToOperationResult(await _administrator.BindPaymentStrawManAsync(
            identity,
            paymentId,
            request?.StrawManId ?? string.Empty,
            cancellationToken));
    }

    [HttpPut("straw-men/{strawManId}/settings")]
    public async Task<ActionResult> UpsertStrawManSettingsAsync(
        string strawManId,
        [FromBody] UpdateStrawManSettingsRequest request,
        CancellationToken cancellationToken)
    {
        var identity = await ResolveIdentityAsync(_identityResolver, cancellationToken);

        return ToOperationResult(await _administrator.UpsertStrawManSettingsAsync(
            identity,
            strawManId,
            request?.MovementFeePercentage ?? 0m,
            cancellationToken));
    }

    private async Task<object> ToBankAccountResponseAsync(
        BankAccount account,
        CancellationToken cancellationToken)
    {
        var balances = await _bankBalances.AsQueryable()
            .Where(b => b.BankAccountId == account.Id)
            .ToArrayAsync();
        return BankAccountApiMapping.ToBankAccountResponse(account, balances);
    }

    private async Task<object[]> ToBankAccountResponsesAsync(
        IReadOnlyList<BankAccount> accounts,
        CancellationToken cancellationToken)
    {
        if (accounts.Count == 0)
            return Array.Empty<object>();

        var accountIds = accounts.Select(a => a.Id).ToArray();
        var balances = await _bankBalances.AsQueryable()
            .Where(b => accountIds.Contains(b.BankAccountId))
            .ToArrayAsync();
        var balancesByAccount = balances
            .GroupBy(b => b.BankAccountId, StringComparer.Ordinal)
            .ToDictionary(g => g.Key, g => (IReadOnlyList<BankBalance>)g.ToArray(), StringComparer.Ordinal);

        return accounts
            .Select(account => BankAccountApiMapping.ToBankAccountResponse(
                account,
                balancesByAccount.GetValueOrDefault(account.Id) ?? Array.Empty<BankBalance>()))
            .ToArray();
    }

    private async Task<object> ToCryptoWalletResponseAsync(
        CryptoWallet wallet,
        CancellationToken cancellationToken)
    {
        var balances = await _cryptoBalances.AsQueryable()
            .Where(b => b.CryptoWalletId == wallet.Id)
            .ToArrayAsync();
        return CryptoWalletApiMapping.ToCryptoWalletResponse(wallet, balances);
    }

    private async Task<object[]> ToCryptoWalletResponsesAsync(
        IReadOnlyList<CryptoWallet> wallets,
        CancellationToken cancellationToken)
    {
        if (wallets.Count == 0)
            return Array.Empty<object>();

        var walletIds = wallets.Select(w => w.Id).ToArray();
        var balances = await _cryptoBalances.AsQueryable()
            .Where(b => walletIds.Contains(b.CryptoWalletId))
            .ToArrayAsync();
        var balancesByWallet = balances
            .GroupBy(b => b.CryptoWalletId, StringComparer.Ordinal)
            .ToDictionary(g => g.Key, g => (IReadOnlyList<CryptoBalance>)g.ToArray(), StringComparer.Ordinal);

        return wallets
            .Select(wallet => CryptoWalletApiMapping.ToCryptoWalletResponse(
                wallet,
                balancesByWallet.GetValueOrDefault(wallet.Id) ?? Array.Empty<CryptoBalance>()))
            .ToArray();
    }
}

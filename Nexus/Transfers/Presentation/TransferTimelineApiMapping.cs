using Nexus.Transfers.Application.Models;

namespace Nexus.Transfers.Presentation;

public static class TransferTimelineApiMapping
{
    public static object ToTimelineResponse(TransferTimelineDetails details) => new
    {
        rootTransferId = details.RootTransferId,
        focusTransferId = details.FocusTransferId,
        strawMan = ToAccountSummary(details.StrawMan),
        steps = details.Steps.Select(ToStep).ToArray(),
        activeBalances = details.ActiveBalances.Select(ToActiveBalance).ToArray(),
    };

    private static object ToStep(TransferTimelineStepDetails step) => new
    {
        transferId = step.TransferId,
        type = step.Type.ToString(),
        createdAt = step.CreatedAt,
        isFocus = step.IsFocus,
        isCurrent = step.IsCurrent,
        title = step.Title,
        summary = step.Summary,
        transfer = ToTransfer(step.Transfer),
        balanceEffects = step.BalanceEffects.Select(ToBalanceEffect).ToArray(),
        payments = step.Payments.Select(ToPayment).ToArray(),
    };

    private static object ToTransfer(TransferEnrichedDetails transfer) => new
    {
        id = transfer.Id,
        type = transfer.Type.ToString(),
        onrampingMethod = transfer.OnrampingMethod,
        proof = transfer.Proof is null
            ? null
            : new
            {
                pixTransactionId = transfer.Proof.PixTransactionId,
                pixAuthenticationCode = transfer.Proof.PixAuthenticationCode,
                cryptoTransactionId = transfer.Proof.CryptoTransactionId,
            },
        source = ToNode(transfer.Source),
        destination = ToNode(transfer.Destination),
        sourceAmount = transfer.SourceAmount,
        producedAmount = transfer.ProducedAmount,
        producedAsset = transfer.ProducedAsset,
        producedChain = transfer.ProducedChain,
        paymentIds = transfer.PaymentIds,
        sourceBalanceId = transfer.SourceBalanceId,
        strawMan = ToAccountSummary(transfer.StrawMan),
        createdAt = transfer.CreatedAt,
    };

    private static object? ToAccountSummary(AccountSummaryDetails? summary)
    {
        if (summary is null)
            return null;

        return new
        {
            id = summary.Id,
            username = summary.Username,
        };
    }

    private static object? ToNode(EnrichedAccountNodeDetails? node)
    {
        if (node is null)
            return null;

        return new
        {
            kind = node.Kind,
            id = node.Id,
            displayName = node.DisplayName,
            label = node.Label,
            username = node.Username,
            bankSummary = node.BankSummary,
            cryptoSummary = node.CryptoSummary,
        };
    }

    private static object ToBalanceEffect(BalanceEffectDetails effect) => new
    {
        direction = effect.Direction,
        balanceId = effect.BalanceId,
        amount = effect.Amount,
        chain = effect.Chain,
        asset = effect.Asset,
        currency = effect.Currency,
        account = ToNode(effect.Account),
    };

    private static object ToPayment(PaymentSummaryDetails payment) => new
    {
        id = payment.Id,
        amount = payment.Amount,
        status = payment.Status,
        settlementStatus = payment.SettlementStatus,
        gateway = payment.Gateway,
        gatewayTransactionId = payment.GatewayTransactionId,
        operatorUsername = payment.OperatorUsername,
        createdAt = payment.CreatedAt,
    };

    private static object ToActiveBalance(ActiveBalanceDetails balance) => new
    {
        balanceId = balance.BalanceId,
        transferId = balance.TransferId,
        amount = balance.Amount,
        chain = balance.Chain,
        asset = balance.Asset,
        currency = balance.Currency,
        account = ToNode(balance.Account),
        canMove = balance.CanMove,
        canPayout = balance.CanPayout,
    };
}

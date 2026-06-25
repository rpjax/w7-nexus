using Nexus.Transfers.Aggregates;

namespace Nexus.Transfers.Application.Models;

public sealed class TransferTimelineDetails
{
    public required string RootTransferId { get; init; }
    public required string FocusTransferId { get; init; }
    public AccountSummaryDetails? StrawMan { get; init; }
    public required IReadOnlyList<TransferTimelineStepDetails> Steps { get; init; }
    public required IReadOnlyList<ActiveBalanceDetails> ActiveBalances { get; init; }
}

public sealed class TransferTimelineStepDetails
{
    public required string TransferId { get; init; }
    public required TransferType Type { get; init; }
    public required DateTime CreatedAt { get; init; }
    public required bool IsFocus { get; init; }
    public required bool IsCurrent { get; init; }
    public required string Title { get; init; }
    public required string Summary { get; init; }
    public required TransferEnrichedDetails Transfer { get; init; }
    public required IReadOnlyList<BalanceEffectDetails> BalanceEffects { get; init; }
    public required IReadOnlyList<PaymentSummaryDetails> Payments { get; init; }
}

public sealed class TransferEnrichedDetails
{
    public required string Id { get; init; }
    public required TransferType Type { get; init; }
    public string? OnrampingMethod { get; init; }
    public TransferProofDetails? Proof { get; init; }
    public TransferEndpointDetails? Source { get; init; }
    public TransferEndpointDetails? Destination { get; init; }
    public required decimal SourceAmount { get; init; }
    public decimal? ProducedAmount { get; init; }
    public string? ProducedAsset { get; init; }
    public string? ProducedChain { get; init; }
    public required IReadOnlyList<string> PaymentIds { get; init; }
    public string? SourceBalanceId { get; init; }
    public required AccountSummaryDetails StrawMan { get; init; }
    public required DateTime CreatedAt { get; init; }
}

public sealed class TransferProofDetails
{
    public string? PixTransactionId { get; init; }
    public string? PixAuthenticationCode { get; init; }
    public string? CryptoTransactionId { get; init; }
}

public sealed class AccountSummaryDetails
{
    public required string Id { get; init; }
    public required string Username { get; init; }
}

public sealed class TransferEndpointDetails
{
    public required string Kind { get; init; }
    public string? Id { get; init; }
    public required string DisplayName { get; init; }
    public string? Label { get; init; }
    public string? Username { get; init; }
    public string? BankSummary { get; init; }
    public string? CryptoSummary { get; init; }
}

public sealed class BalanceEffectDetails
{
    public required string Direction { get; init; }
    public required string BalanceId { get; init; }
    public required decimal Amount { get; init; }
    public string? Chain { get; init; }
    public string? Asset { get; init; }
    public required string Currency { get; init; }
    public required TransferEndpointDetails Account { get; init; }
}

public sealed class PaymentSummaryDetails
{
    public required string Id { get; init; }
    public required decimal Amount { get; init; }
    public required string Status { get; init; }
    public required string SettlementStatus { get; init; }
    public required string Gateway { get; init; }
    public required string GatewayTransactionId { get; init; }
    public string? OperatorUsername { get; init; }
    public required DateTime CreatedAt { get; init; }
}

public sealed class ActiveBalanceDetails
{
    public required string BalanceId { get; init; }
    public required string TransferId { get; init; }
    public required decimal Amount { get; init; }
    public string? Chain { get; init; }
    public string? Asset { get; init; }
    public required string Currency { get; init; }
    public required TransferEndpointDetails Account { get; init; }
    public required bool CanMove { get; init; }
    public required bool CanPayout { get; init; }
}

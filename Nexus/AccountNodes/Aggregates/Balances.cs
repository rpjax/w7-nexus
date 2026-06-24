using Aidan.Core.Errors;
using Aidan.Core.Patterns;
using Nexus.AccountNodes.Errors;

namespace Nexus.AccountNodes.Aggregates;

public enum SplitKind
{
    ProfitShare = 0,
    StrawManMovementFee,
}

public sealed class BalanceSplitSnapshot
{
    public string AccountId { get; }
    public decimal Percentage { get; }
    public decimal Amount { get; }
    public SplitKind SplitKind { get; }

    internal BalanceSplitSnapshot(string accountId, decimal percentage, decimal amount, SplitKind splitKind)
    {
        AccountId = accountId.Trim();
        Percentage = percentage;
        Amount = amount;
        SplitKind = splitKind;
    }

    public static IResult<BalanceSplitSnapshot> Create(
        string accountId,
        decimal percentage,
        decimal amount,
        SplitKind splitKind)
    {
        accountId = accountId?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(accountId))
            return Result<BalanceSplitSnapshot>.Failure(Error.Create()
                .WithCode(AccountNodeErrorCodes.SplitAccountIdInvalid)
                .WithMessage("O ID da conta no split é obrigatório.")
                .Build());

        if (percentage < 0 || percentage > 100)
            return Result<BalanceSplitSnapshot>.Failure(Error.Create()
                .WithCode(AccountNodeErrorCodes.SplitPercentageInvalid)
                .WithMessage("A porcentagem do split deve estar entre 0 e 100.")
                .Build());

        if (amount < 0)
            return Result<BalanceSplitSnapshot>.Failure(Error.Create()
                .WithCode(AccountNodeErrorCodes.SplitAmountInvalid)
                .WithMessage("O valor do split não pode ser negativo.")
                .Build());

        if (!Enum.IsDefined(splitKind))
            return Result<BalanceSplitSnapshot>.Failure(Error.Create()
                .WithCode(AccountNodeErrorCodes.SplitKindInvalid)
                .WithMessage("O tipo de split informado é inválido.")
                .Build());

        return Result<BalanceSplitSnapshot>.Success(
            new BalanceSplitSnapshot(accountId, percentage, amount, splitKind));
    }
}

public sealed class BalanceOriginSnapshot
{
    public string OperationId { get; }
    public string? OperatorId { get; }
    public string StrawManId { get; }

    internal BalanceOriginSnapshot(string operationId, string? operatorId, string strawManId)
    {
        OperationId = operationId.Trim();
        OperatorId = string.IsNullOrWhiteSpace(operatorId) ? null : operatorId.Trim();
        StrawManId = strawManId.Trim();
    }

    public static IResult<BalanceOriginSnapshot> Create(
        string operationId,
        string? operatorId,
        string strawManId)
    {
        operationId = operationId?.Trim() ?? string.Empty;
        strawManId = strawManId?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(operationId))
            return Result<BalanceOriginSnapshot>.Failure(Error.Create()
                .WithCode(AccountNodeErrorCodes.OriginOperationIdInvalid)
                .WithMessage("O ID da operação de origem é obrigatório.")
                .Build());

        if (string.IsNullOrWhiteSpace(strawManId))
            return Result<BalanceOriginSnapshot>.Failure(Error.Create()
                .WithCode(AccountNodeErrorCodes.OriginStrawManIdInvalid)
                .WithMessage("O ID do laranja de origem é obrigatório.")
                .Build());

        return Result<BalanceOriginSnapshot>.Success(
            new BalanceOriginSnapshot(operationId, operatorId, strawManId));
    }
}

public sealed class BankBalance
{
    public string Id { get; }
    public decimal AmountBrl { get; }
    public string TransferId { get; }
    public DateTime CreatedAt { get; }
    public IReadOnlyList<BalanceSplitSnapshot> SplitSnapshot { get; }
    public IReadOnlyList<string> AppliedStrawManFeeIds { get; }
    public BalanceOriginSnapshot OriginSnapshot { get; }

    internal BankBalance(
        string id,
        decimal amountBrl,
        string transferId,
        DateTime createdAt,
        IReadOnlyList<BalanceSplitSnapshot> splitSnapshot,
        IReadOnlyList<string> appliedStrawManFeeIds,
        BalanceOriginSnapshot originSnapshot)
    {
        Id = id;
        AmountBrl = amountBrl;
        TransferId = transferId;
        CreatedAt = createdAt;
        SplitSnapshot = splitSnapshot;
        AppliedStrawManFeeIds = appliedStrawManFeeIds;
        OriginSnapshot = originSnapshot;
    }

    public static IResult<BankBalance> Create(
        decimal amountBrl,
        string transferId,
        IReadOnlyList<BalanceSplitSnapshot> splitSnapshot,
        IReadOnlyList<string> appliedStrawManFeeIds,
        BalanceOriginSnapshot originSnapshot)
    {
        transferId = transferId?.Trim() ?? string.Empty;

        if (amountBrl <= 0)
            return Result<BankBalance>.Failure(Error.Create()
                .WithCode(AccountNodeErrorCodes.BalanceAmountInvalid)
                .WithMessage("O valor do saldo deve ser maior que zero.")
                .Build());

        if (string.IsNullOrWhiteSpace(transferId))
            return Result<BankBalance>.Failure(Error.Create()
                .WithCode(AccountNodeErrorCodes.BalanceTransferIdInvalid)
                .WithMessage("O ID da transferência do saldo é obrigatório.")
                .Build());

        if (splitSnapshot.Count == 0)
            return Result<BankBalance>.Failure(Error.Create()
                .WithCode(AccountNodeErrorCodes.BalanceSplitSnapshotRequired)
                .WithMessage("O snapshot de split do saldo é obrigatório.")
                .Build());

        var normalizedFeeIds = (appliedStrawManFeeIds ?? Array.Empty<string>())
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Select(id => id.Trim())
            .Distinct(StringComparer.Ordinal)
            .ToList();

        return Result<BankBalance>.Success(new BankBalance(
            id: Guid.NewGuid().ToString("N"),
            amountBrl: amountBrl,
            transferId: transferId,
            createdAt: DateTime.UtcNow,
            splitSnapshot: splitSnapshot,
            appliedStrawManFeeIds: normalizedFeeIds,
            originSnapshot: originSnapshot));
    }

    internal BankBalance WithId(string id) =>
        new(id, AmountBrl, TransferId, CreatedAt, SplitSnapshot, AppliedStrawManFeeIds, OriginSnapshot);

    internal BankBalance WithAmount(decimal amountBrl) =>
        new(Id, amountBrl, TransferId, CreatedAt, SplitSnapshot, AppliedStrawManFeeIds, OriginSnapshot);
}

public sealed class CryptoBalance
{
    public string Id { get; }
    public Chain Chain { get; }
    public CryptoAsset Asset { get; }
    public decimal Amount { get; }
    public string TransferId { get; }
    public DateTime CreatedAt { get; }
    public IReadOnlyList<BalanceSplitSnapshot> SplitSnapshot { get; }
    public IReadOnlyList<string> AppliedStrawManFeeIds { get; }
    public BalanceOriginSnapshot OriginSnapshot { get; }

    internal CryptoBalance(
        string id,
        Chain chain,
        CryptoAsset asset,
        decimal amount,
        string transferId,
        DateTime createdAt,
        IReadOnlyList<BalanceSplitSnapshot> splitSnapshot,
        IReadOnlyList<string> appliedStrawManFeeIds,
        BalanceOriginSnapshot originSnapshot)
    {
        Id = id;
        Chain = chain;
        Asset = asset;
        Amount = amount;
        TransferId = transferId;
        CreatedAt = createdAt;
        SplitSnapshot = splitSnapshot;
        AppliedStrawManFeeIds = appliedStrawManFeeIds;
        OriginSnapshot = originSnapshot;
    }

    public static IResult<CryptoBalance> Create(
        Chain chain,
        CryptoAsset asset,
        decimal amount,
        string transferId,
        IReadOnlyList<BalanceSplitSnapshot> splitSnapshot,
        IReadOnlyList<string> appliedStrawManFeeIds,
        BalanceOriginSnapshot originSnapshot)
    {
        transferId = transferId?.Trim() ?? string.Empty;

        if (!Enum.IsDefined(chain))
            return Result<CryptoBalance>.Failure(Error.Create()
                .WithCode(AccountNodeErrorCodes.BalanceChainInvalid)
                .WithMessage("A rede blockchain do saldo é inválida.")
                .Build());

        if (!Enum.IsDefined(asset))
            return Result<CryptoBalance>.Failure(Error.Create()
                .WithCode(AccountNodeErrorCodes.BalanceAssetInvalid)
                .WithMessage("O ativo crypto do saldo é inválido.")
                .Build());

        if (amount <= 0)
            return Result<CryptoBalance>.Failure(Error.Create()
                .WithCode(AccountNodeErrorCodes.BalanceAmountInvalid)
                .WithMessage("O valor do saldo deve ser maior que zero.")
                .Build());

        if (string.IsNullOrWhiteSpace(transferId))
            return Result<CryptoBalance>.Failure(Error.Create()
                .WithCode(AccountNodeErrorCodes.BalanceTransferIdInvalid)
                .WithMessage("O ID da transferência do saldo é obrigatório.")
                .Build());

        if (splitSnapshot.Count == 0)
            return Result<CryptoBalance>.Failure(Error.Create()
                .WithCode(AccountNodeErrorCodes.BalanceSplitSnapshotRequired)
                .WithMessage("O snapshot de split do saldo é obrigatório.")
                .Build());

        var normalizedFeeIds = (appliedStrawManFeeIds ?? Array.Empty<string>())
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Select(id => id.Trim())
            .Distinct(StringComparer.Ordinal)
            .ToList();

        return Result<CryptoBalance>.Success(new CryptoBalance(
            id: Guid.NewGuid().ToString("N"),
            chain: chain,
            asset: asset,
            amount: amount,
            transferId: transferId,
            createdAt: DateTime.UtcNow,
            splitSnapshot: splitSnapshot,
            appliedStrawManFeeIds: normalizedFeeIds,
            originSnapshot: originSnapshot));
    }

    internal CryptoBalance WithId(string id) =>
        new(id, Chain, Asset, Amount, TransferId, CreatedAt, SplitSnapshot, AppliedStrawManFeeIds, OriginSnapshot);

    internal CryptoBalance WithAmount(decimal amount) =>
        new(Id, Chain, Asset, amount, TransferId, CreatedAt, SplitSnapshot, AppliedStrawManFeeIds, OriginSnapshot);
}

public sealed class BankDebitPartialResult
{
    public BankBalance DebitedBalance { get; }
    public BankBalance? RemainderBalance { get; }

    internal BankDebitPartialResult(BankBalance debitedBalance, BankBalance? remainderBalance)
    {
        DebitedBalance = debitedBalance;
        RemainderBalance = remainderBalance;
    }
}

public sealed class CryptoDebitPartialResult
{
    public CryptoBalance DebitedBalance { get; }
    public CryptoBalance? RemainderBalance { get; }

    internal CryptoDebitPartialResult(CryptoBalance debitedBalance, CryptoBalance? remainderBalance)
    {
        DebitedBalance = debitedBalance;
        RemainderBalance = remainderBalance;
    }
}

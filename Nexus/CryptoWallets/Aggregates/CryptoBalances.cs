using Aidan.Core.Errors;
using Aidan.Core.Patterns;
using Nexus.CryptoWallets.Errors;

namespace Nexus.CryptoWallets.Aggregates;

public enum CryptoSplitKind
{
    ProfitShare = 0,
    StrawManMovementFee,
}

public sealed class CryptoBalanceSplit
{
    public string AccountId { get; }
    public decimal Percentage { get; }
    public decimal Amount { get; }
    public CryptoSplitKind SplitKind { get; }

    internal CryptoBalanceSplit(string accountId, decimal percentage, decimal amount, CryptoSplitKind splitKind)
    {
        AccountId = accountId.Trim();
        Percentage = percentage;
        Amount = amount;
        SplitKind = splitKind;
    }

    public static IResult<CryptoBalanceSplit> Create(
        string accountId,
        decimal percentage,
        decimal amount,
        CryptoSplitKind splitKind)
    {
        accountId = accountId?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(accountId))
            return Result<CryptoBalanceSplit>.Failure(Error.Create()
                .WithCode(CryptoWalletErrorCodes.SplitAccountIdInvalid)
                .WithMessage("O ID da conta no split é obrigatório.")
                .Build());

        if (percentage < 0 || percentage > 100)
            return Result<CryptoBalanceSplit>.Failure(Error.Create()
                .WithCode(CryptoWalletErrorCodes.SplitPercentageInvalid)
                .WithMessage("A porcentagem do split deve estar entre 0 e 100.")
                .Build());

        if (amount < 0)
            return Result<CryptoBalanceSplit>.Failure(Error.Create()
                .WithCode(CryptoWalletErrorCodes.SplitAmountInvalid)
                .WithMessage("O valor do split não pode ser negativo.")
                .Build());

        if (!Enum.IsDefined(splitKind))
            return Result<CryptoBalanceSplit>.Failure(Error.Create()
                .WithCode(CryptoWalletErrorCodes.SplitKindInvalid)
                .WithMessage("O tipo de split informado é inválido.")
                .Build());

        return Result<CryptoBalanceSplit>.Success(
            new CryptoBalanceSplit(accountId, percentage, amount, splitKind));
    }
}

public sealed class CryptoBalanceOrigin
{
    public string OperationId { get; }
    public string? OperatorId { get; }
    public string StrawManId { get; }

    internal CryptoBalanceOrigin(string operationId, string? operatorId, string strawManId)
    {
        OperationId = operationId.Trim();
        OperatorId = string.IsNullOrWhiteSpace(operatorId) ? null : operatorId.Trim();
        StrawManId = strawManId.Trim();
    }

    public static IResult<CryptoBalanceOrigin> Create(
        string operationId,
        string? operatorId,
        string strawManId)
    {
        operationId = operationId?.Trim() ?? string.Empty;
        strawManId = strawManId?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(operationId))
            return Result<CryptoBalanceOrigin>.Failure(Error.Create()
                .WithCode(CryptoWalletErrorCodes.OriginOperationIdInvalid)
                .WithMessage("O ID da operação de origem é obrigatório.")
                .Build());

        if (string.IsNullOrWhiteSpace(strawManId))
            return Result<CryptoBalanceOrigin>.Failure(Error.Create()
                .WithCode(CryptoWalletErrorCodes.OriginStrawManIdInvalid)
                .WithMessage("O ID do laranja de origem é obrigatório.")
                .Build());

        return Result<CryptoBalanceOrigin>.Success(
            new CryptoBalanceOrigin(operationId, operatorId, strawManId));
    }
}

public sealed class CryptoBalance
{
    public string Id { get; }
    public Chain Chain { get; }
    public CryptoAsset Asset { get; }
    public decimal Amount { get; }
    public string TransferId { get; }
    public DateTime CreatedAt { get; }
    public IReadOnlyList<CryptoBalanceSplit> Splits { get; }
    public IReadOnlyList<string> AppliedStrawManFeeIds { get; }
    public CryptoBalanceOrigin Origin { get; }

    internal CryptoBalance(
        string id,
        Chain chain,
        CryptoAsset asset,
        decimal amount,
        string transferId,
        DateTime createdAt,
        IReadOnlyList<CryptoBalanceSplit> splits,
        IReadOnlyList<string> appliedStrawManFeeIds,
        CryptoBalanceOrigin origin)
    {
        Id = id;
        Chain = chain;
        Asset = asset;
        Amount = amount;
        TransferId = transferId;
        CreatedAt = createdAt;
        Splits = splits;
        AppliedStrawManFeeIds = appliedStrawManFeeIds;
        Origin = origin;
    }

    public static IResult<CryptoBalance> Create(
        Chain chain,
        CryptoAsset asset,
        decimal amount,
        string transferId,
        IReadOnlyList<CryptoBalanceSplit> splits,
        IReadOnlyList<string> appliedStrawManFeeIds,
        CryptoBalanceOrigin origin)
    {
        transferId = transferId?.Trim() ?? string.Empty;

        if (!Enum.IsDefined(chain))
            return Result<CryptoBalance>.Failure(Error.Create()
                .WithCode(CryptoWalletErrorCodes.BalanceChainInvalid)
                .WithMessage("A rede blockchain do saldo é inválida.")
                .Build());

        if (!Enum.IsDefined(asset))
            return Result<CryptoBalance>.Failure(Error.Create()
                .WithCode(CryptoWalletErrorCodes.BalanceAssetInvalid)
                .WithMessage("O ativo crypto do saldo é inválido.")
                .Build());

        if (amount <= 0)
            return Result<CryptoBalance>.Failure(Error.Create()
                .WithCode(CryptoWalletErrorCodes.BalanceAmountInvalid)
                .WithMessage("O valor do saldo deve ser maior que zero.")
                .Build());

        if (string.IsNullOrWhiteSpace(transferId))
            return Result<CryptoBalance>.Failure(Error.Create()
                .WithCode(CryptoWalletErrorCodes.BalanceTransferIdInvalid)
                .WithMessage("O ID da transferência do saldo é obrigatório.")
                .Build());

        if (splits.Count == 0)
            return Result<CryptoBalance>.Failure(Error.Create()
                .WithCode(CryptoWalletErrorCodes.BalanceSplitsRequired)
                .WithMessage("Os splits do saldo são obrigatórios.")
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
            splits: splits,
            appliedStrawManFeeIds: normalizedFeeIds,
            origin: origin));
    }

    internal CryptoBalance WithId(string id) =>
        new(id, Chain, Asset, Amount, TransferId, CreatedAt, Splits, AppliedStrawManFeeIds, Origin);

    internal CryptoBalance WithAmount(decimal amount) =>
        new(Id, Chain, Asset, amount, TransferId, CreatedAt, Splits, AppliedStrawManFeeIds, Origin);
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

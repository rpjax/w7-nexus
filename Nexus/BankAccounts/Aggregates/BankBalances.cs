using Aidan.Core.Errors;
using Aidan.Core.Patterns;
using Nexus.BankAccounts.Errors;

namespace Nexus.BankAccounts.Aggregates;

public enum BankSplitKind
{
    ProfitShare = 0,
    StrawManMovementFee,
}

public sealed class BankBalanceSplit
{
    public string AccountId { get; }
    public decimal Percentage { get; }
    public decimal Amount { get; }
    public BankSplitKind SplitKind { get; }

    internal BankBalanceSplit(string accountId, decimal percentage, decimal amount, BankSplitKind splitKind)
    {
        AccountId = accountId.Trim();
        Percentage = percentage;
        Amount = amount;
        SplitKind = splitKind;
    }

    public static IResult<BankBalanceSplit> Create(
        string accountId,
        decimal percentage,
        decimal amount,
        BankSplitKind splitKind)
    {
        accountId = accountId?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(accountId))
            return Result<BankBalanceSplit>.Failure(Error.Create()
                .WithCode(BankAccountErrorCodes.SplitAccountIdInvalid)
                .WithMessage("O ID da conta no split é obrigatório.")
                .Build());

        if (percentage < 0 || percentage > 100)
            return Result<BankBalanceSplit>.Failure(Error.Create()
                .WithCode(BankAccountErrorCodes.SplitPercentageInvalid)
                .WithMessage("A porcentagem do split deve estar entre 0 e 100.")
                .Build());

        if (amount < 0)
            return Result<BankBalanceSplit>.Failure(Error.Create()
                .WithCode(BankAccountErrorCodes.SplitAmountInvalid)
                .WithMessage("O valor do split não pode ser negativo.")
                .Build());

        if (!Enum.IsDefined(splitKind))
            return Result<BankBalanceSplit>.Failure(Error.Create()
                .WithCode(BankAccountErrorCodes.SplitKindInvalid)
                .WithMessage("O tipo de split informado é inválido.")
                .Build());

        return Result<BankBalanceSplit>.Success(
            new BankBalanceSplit(accountId, percentage, amount, splitKind));
    }
}

public sealed class BankBalanceOrigin
{
    public string OperationId { get; }
    public string? OperatorId { get; }
    public string StrawManId { get; }

    internal BankBalanceOrigin(string operationId, string? operatorId, string strawManId)
    {
        OperationId = operationId.Trim();
        OperatorId = string.IsNullOrWhiteSpace(operatorId) ? null : operatorId.Trim();
        StrawManId = strawManId.Trim();
    }

    public static IResult<BankBalanceOrigin> Create(
        string operationId,
        string? operatorId,
        string strawManId)
    {
        operationId = operationId?.Trim() ?? string.Empty;
        strawManId = strawManId?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(operationId))
            return Result<BankBalanceOrigin>.Failure(Error.Create()
                .WithCode(BankAccountErrorCodes.OriginOperationIdInvalid)
                .WithMessage("O ID da operação de origem é obrigatório.")
                .Build());

        if (string.IsNullOrWhiteSpace(strawManId))
            return Result<BankBalanceOrigin>.Failure(Error.Create()
                .WithCode(BankAccountErrorCodes.OriginStrawManIdInvalid)
                .WithMessage("O ID do laranja de origem é obrigatório.")
                .Build());

        return Result<BankBalanceOrigin>.Success(
            new BankBalanceOrigin(operationId, operatorId, strawManId));
    }
}

public sealed class BankBalance
{
    public string Id { get; }
    public decimal AmountBrl { get; }
    public string TransferId { get; }
    public DateTime CreatedAt { get; }
    public IReadOnlyList<BankBalanceSplit> Splits { get; }
    public IReadOnlyList<string> AppliedStrawManFeeIds { get; }
    public BankBalanceOrigin Origin { get; }

    internal BankBalance(
        string id,
        decimal amountBrl,
        string transferId,
        DateTime createdAt,
        IReadOnlyList<BankBalanceSplit> splits,
        IReadOnlyList<string> appliedStrawManFeeIds,
        BankBalanceOrigin origin)
    {
        Id = id;
        AmountBrl = amountBrl;
        TransferId = transferId;
        CreatedAt = createdAt;
        Splits = splits;
        AppliedStrawManFeeIds = appliedStrawManFeeIds;
        Origin = origin;
    }

    public static IResult<BankBalance> Create(
        decimal amountBrl,
        string transferId,
        IReadOnlyList<BankBalanceSplit> splits,
        IReadOnlyList<string> appliedStrawManFeeIds,
        BankBalanceOrigin origin)
    {
        transferId = transferId?.Trim() ?? string.Empty;

        if (amountBrl <= 0)
            return Result<BankBalance>.Failure(Error.Create()
                .WithCode(BankAccountErrorCodes.BalanceAmountInvalid)
                .WithMessage("O valor do saldo deve ser maior que zero.")
                .Build());

        if (string.IsNullOrWhiteSpace(transferId))
            return Result<BankBalance>.Failure(Error.Create()
                .WithCode(BankAccountErrorCodes.BalanceTransferIdInvalid)
                .WithMessage("O ID da transferência do saldo é obrigatório.")
                .Build());

        if (splits.Count == 0)
            return Result<BankBalance>.Failure(Error.Create()
                .WithCode(BankAccountErrorCodes.BalanceSplitsRequired)
                .WithMessage("Os splits do saldo são obrigatórios.")
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
            splits: splits,
            appliedStrawManFeeIds: normalizedFeeIds,
            origin: origin));
    }

    internal BankBalance WithId(string id) =>
        new(id, AmountBrl, TransferId, CreatedAt, Splits, AppliedStrawManFeeIds, Origin);

    internal BankBalance WithAmount(decimal amountBrl) =>
        new(Id, amountBrl, TransferId, CreatedAt, Splits, AppliedStrawManFeeIds, Origin);
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

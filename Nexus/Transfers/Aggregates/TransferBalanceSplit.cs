using Aidan.Core.Errors;
using Aidan.Core.Patterns;
using Nexus.Transfers.Errors;

namespace Nexus.Transfers.Aggregates;

public enum TransferSplitKind
{
    ProfitShare = 0,
    StrawManFee,
}

public sealed class TransferBalanceSplit
{
    public string AccountId { get; }
    public decimal Percentage { get; }
    public decimal Amount { get; }
    public TransferSplitKind SplitKind { get; }

    internal TransferBalanceSplit(string accountId, decimal percentage, decimal amount, TransferSplitKind splitKind)
    {
        AccountId = accountId.Trim();
        Percentage = percentage;
        Amount = amount;
        SplitKind = splitKind;
    }

    public static IResult<TransferBalanceSplit> Create(
        string accountId,
        decimal percentage,
        decimal amount,
        TransferSplitKind splitKind)
    {
        accountId = accountId?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(accountId))
            return Result<TransferBalanceSplit>.Failure(Error.Create()
                .WithCode(TransferErrorCodes.SplitAccountIdInvalid)
                .WithMessage("O ID da conta no split é obrigatório.")
                .Build());

        if (percentage < 0 || percentage > 100)
            return Result<TransferBalanceSplit>.Failure(Error.Create()
                .WithCode(TransferErrorCodes.SplitPercentageInvalid)
                .WithMessage("A porcentagem do split deve estar entre 0 e 100.")
                .Build());

        if (amount < 0)
            return Result<TransferBalanceSplit>.Failure(Error.Create()
                .WithCode(TransferErrorCodes.SplitAmountInvalid)
                .WithMessage("O valor do split não pode ser negativo.")
                .Build());

        if (!Enum.IsDefined(splitKind))
            return Result<TransferBalanceSplit>.Failure(Error.Create()
                .WithCode(TransferErrorCodes.SplitKindInvalid)
                .WithMessage("O tipo de split informado é inválido.")
                .Build());

        return Result<TransferBalanceSplit>.Success(
            new TransferBalanceSplit(accountId, percentage, amount, splitKind));
    }
}

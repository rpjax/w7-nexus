using Aidan.Core.Errors;
using Aidan.Core.Patterns;
using Nexus.Withdrawals.Errors;

namespace Nexus.Withdrawals.Aggregates;

public enum WithdrawalType
{
    Pix = 0,
    Crypto,
}

public sealed class PixProof
{
    public const int MaxTransactionIdLength = 200;
    public const int MaxAuthenticationCodeLength = 200;

    public string? TransactionId { get; }
    public string? AuthenticationCode { get; }

    internal PixProof(string? transactionId, string? authenticationCode)
    {
        TransactionId = transactionId;
        AuthenticationCode = authenticationCode;
    }

    public static IResult<PixProof?> Create(string? transactionId, string? authenticationCode)
    {
        transactionId = string.IsNullOrWhiteSpace(transactionId) ? null : transactionId.Trim();
        authenticationCode = string.IsNullOrWhiteSpace(authenticationCode) ? null : authenticationCode.Trim();

        if (transactionId is null && authenticationCode is null)
            return Result<PixProof?>.Success(null);

        if (transactionId is not null && transactionId.Length > MaxTransactionIdLength)
            return Result<PixProof?>.Failure(Error.Create()
                .WithCode(WithdrawalErrorCodes.PixProofTransactionIdTooLong)
                .WithMessage($"O ID da transação PIX pode ter no máximo {MaxTransactionIdLength} caracteres.")
                .Build());

        if (authenticationCode is not null && authenticationCode.Length > MaxAuthenticationCodeLength)
            return Result<PixProof?>.Failure(Error.Create()
                .WithCode(WithdrawalErrorCodes.PixProofAuthenticationCodeTooLong)
                .WithMessage($"O código de autenticação PIX pode ter no máximo {MaxAuthenticationCodeLength} caracteres.")
                .Build());

        return Result<PixProof?>.Success(new PixProof(transactionId, authenticationCode));
    }
}

public sealed class CryptoProof
{
    public const int MaxTransactionIdLength = 256;

    public string? TransactionId { get; }

    internal CryptoProof(string? transactionId)
    {
        TransactionId = transactionId;
    }

    public static IResult<CryptoProof?> Create(string? transactionId)
    {
        transactionId = string.IsNullOrWhiteSpace(transactionId) ? null : transactionId.Trim();

        if (transactionId is null)
            return Result<CryptoProof?>.Success(null);

        if (transactionId.Length > MaxTransactionIdLength)
            return Result<CryptoProof?>.Failure(Error.Create()
                .WithCode(WithdrawalErrorCodes.CryptoProofTransactionIdTooLong)
                .WithMessage($"O ID da transação crypto pode ter no máximo {MaxTransactionIdLength} caracteres.")
                .Build());

        return Result<CryptoProof?>.Success(new CryptoProof(transactionId));
    }
}

public sealed class Withdrawal
{
    public const int MaxCostDescriptionLength = 500;

    public string Id { get; }
    public string OperationId { get; }
    public WithdrawalType Type { get; }
    public string StrawManAccountId { get; }
    public string? BankAccountId { get; }
    public string? CryptoWalletId { get; }
    public IReadOnlyList<string> PaymentIds { get; }
    public string? CostDescription { get; }
    public decimal CostAmount { get; }
    public PixProof? PixProof { get; }
    public CryptoProof? CryptoProof { get; }
    public decimal PaymentsTotalAmount { get; }
    public decimal NetAmount { get; }
    public DateTime CreatedAt { get; }

    internal Withdrawal(
        string Id,
        string OperationId,
        WithdrawalType Type,
        string StrawManAccountId,
        string? BankAccountId,
        string? CryptoWalletId,
        IReadOnlyList<string> PaymentIds,
        string? CostDescription,
        decimal CostAmount,
        PixProof? PixProof,
        CryptoProof? CryptoProof,
        decimal PaymentsTotalAmount,
        decimal NetAmount,
        DateTime CreatedAt)
    {
        this.Id = Id;
        this.OperationId = OperationId;
        this.Type = Type;
        this.StrawManAccountId = StrawManAccountId;
        this.BankAccountId = BankAccountId;
        this.CryptoWalletId = CryptoWalletId;
        this.PaymentIds = PaymentIds;
        this.CostDescription = CostDescription;
        this.CostAmount = CostAmount;
        this.PixProof = PixProof;
        this.CryptoProof = CryptoProof;
        this.PaymentsTotalAmount = PaymentsTotalAmount;
        this.NetAmount = NetAmount;
        this.CreatedAt = CreatedAt;
    }

    public static IResult<Withdrawal> Create(
        string operationId,
        WithdrawalType type,
        string strawManAccountId,
        string? bankAccountId,
        string? cryptoWalletId,
        IReadOnlyList<string> paymentIds,
        string? costDescription,
        decimal costAmount,
        PixProof? pixProof,
        CryptoProof? cryptoProof,
        decimal paymentsTotalAmount)
    {
        var builder = Result.Create<Withdrawal>();

        operationId = operationId?.Trim() ?? string.Empty;
        strawManAccountId = strawManAccountId?.Trim() ?? string.Empty;
        bankAccountId = string.IsNullOrWhiteSpace(bankAccountId) ? null : bankAccountId.Trim();
        cryptoWalletId = string.IsNullOrWhiteSpace(cryptoWalletId) ? null : cryptoWalletId.Trim();
        costDescription = string.IsNullOrWhiteSpace(costDescription) ? null : costDescription.Trim();

        var normalizedPaymentIds = (paymentIds ?? Array.Empty<string>())
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Select(id => id.Trim())
            .Distinct(StringComparer.Ordinal)
            .ToList();

        if (string.IsNullOrWhiteSpace(operationId))
            builder.WithError(Error.Create()
                .WithCode(WithdrawalErrorCodes.OperationIdInvalid)
                .WithMessage("O ID da operação é obrigatório.")
                .Build());

        if (!Enum.IsDefined(type))
            builder.WithError(Error.Create()
                .WithCode(WithdrawalErrorCodes.TypeInvalid)
                .WithMessage("O tipo de saque informado é inválido.")
                .Build());

        if (string.IsNullOrWhiteSpace(strawManAccountId))
            builder.WithError(Error.Create()
                .WithCode(WithdrawalErrorCodes.StrawManInvalid)
                .WithMessage("O ID do laranja é obrigatório.")
                .Build());

        if (normalizedPaymentIds.Count == 0)
            builder.WithError(Error.Create()
                .WithCode(WithdrawalErrorCodes.PaymentIdsRequired)
                .WithMessage("É necessário vincular ao menos um pagamento ao saque.")
                .Build());

        if (type == WithdrawalType.Pix)
        {
            if (string.IsNullOrWhiteSpace(bankAccountId))
                builder.WithError(Error.Create()
                    .WithCode(WithdrawalErrorCodes.BankAccountRequired)
                    .WithMessage("A conta bancária é obrigatória para saques PIX.")
                    .Build());

            if (cryptoWalletId is not null)
                builder.WithError(Error.Create()
                    .WithCode(WithdrawalErrorCodes.InvalidAggregateState)
                    .WithMessage("Saques PIX não podem vincular uma wallet crypto.")
                    .Build());
        }
        else if (type == WithdrawalType.Crypto)
        {
            if (string.IsNullOrWhiteSpace(cryptoWalletId))
                builder.WithError(Error.Create()
                    .WithCode(WithdrawalErrorCodes.CryptoWalletRequired)
                    .WithMessage("A wallet crypto é obrigatória para saques crypto.")
                    .Build());

            if (bankAccountId is not null)
                builder.WithError(Error.Create()
                    .WithCode(WithdrawalErrorCodes.InvalidAggregateState)
                    .WithMessage("Saques crypto não podem vincular uma conta bancária.")
                    .Build());
        }

        if (costAmount < 0)
            builder.WithError(Error.Create()
                .WithCode(WithdrawalErrorCodes.CostAmountInvalid)
                .WithMessage("O valor de custos não pode ser negativo.")
                .Build());

        if (costAmount > paymentsTotalAmount)
            builder.WithError(Error.Create()
                .WithCode(WithdrawalErrorCodes.CostAmountInvalid)
                .WithMessage("O valor de custos não pode ser maior que o total dos pagamentos.")
                .Build());

        if (costDescription is not null && costDescription.Length > MaxCostDescriptionLength)
            builder.WithError(Error.Create()
                .WithCode(WithdrawalErrorCodes.CostDescriptionTooLong)
                .WithMessage($"A descrição de custos pode ter no máximo {MaxCostDescriptionLength} caracteres.")
                .Build());

        if (builder.ContainsError)
            return builder.Build();

        var netAmount = paymentsTotalAmount - costAmount;

        return builder.WithValue(new Withdrawal(
            Id: string.Empty,
            OperationId: operationId,
            Type: type,
            StrawManAccountId: strawManAccountId,
            BankAccountId: bankAccountId,
            CryptoWalletId: cryptoWalletId,
            PaymentIds: normalizedPaymentIds,
            CostDescription: costDescription,
            CostAmount: costAmount,
            PixProof: pixProof,
            CryptoProof: cryptoProof,
            PaymentsTotalAmount: paymentsTotalAmount,
            NetAmount: netAmount,
            CreatedAt: DateTime.UtcNow)).Build();
    }
}

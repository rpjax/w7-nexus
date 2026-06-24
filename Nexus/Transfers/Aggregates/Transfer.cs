using Aidan.Core.Errors;
using Aidan.Core.Patterns;
using Nexus.Transfers.Errors;

namespace Nexus.Transfers.Aggregates;

public enum TransferType
{
    Withdrawal = 0,
    Movement,
    Payout,
}

public enum OnrampingMethod
{
    Pix = 0,
    GiftCard,
    CreditDebitCard,
}

public enum AccountNodeKind
{
    BankAccount = 0,
    CryptoWallet,
    Participant,
}

public sealed class TransferProof
{
    public const int MaxTransactionIdLength = 256;
    public const int MaxAuthenticationCodeLength = 200;

    public string? PixTransactionId { get; }
    public string? PixAuthenticationCode { get; }
    public string? CryptoTransactionId { get; }

    internal TransferProof(string? pixTransactionId, string? pixAuthenticationCode, string? cryptoTransactionId)
    {
        PixTransactionId = pixTransactionId;
        PixAuthenticationCode = pixAuthenticationCode;
        CryptoTransactionId = cryptoTransactionId;
    }

    public static IResult<TransferProof?> Create(
        string? pixTransactionId,
        string? pixAuthenticationCode,
        string? cryptoTransactionId,
        bool required)
    {
        pixTransactionId = string.IsNullOrWhiteSpace(pixTransactionId) ? null : pixTransactionId.Trim();
        pixAuthenticationCode = string.IsNullOrWhiteSpace(pixAuthenticationCode) ? null : pixAuthenticationCode.Trim();
        cryptoTransactionId = string.IsNullOrWhiteSpace(cryptoTransactionId) ? null : cryptoTransactionId.Trim();

        var hasProof = pixTransactionId is not null
            || pixAuthenticationCode is not null
            || cryptoTransactionId is not null;

        if (required && !hasProof)
            return Result<TransferProof?>.Failure(Error.Create()
                .WithCode(TransferErrorCodes.ProofRequired)
                .WithMessage("O comprovante da transferência é obrigatório.")
                .Build());

        if (!hasProof)
            return Result<TransferProof?>.Success(null);

        if (pixTransactionId is not null && pixTransactionId.Length > MaxTransactionIdLength)
            return Result<TransferProof?>.Failure(Error.Create()
                .WithCode(TransferErrorCodes.ProofTransactionIdTooLong)
                .WithMessage($"O ID da transação PIX pode ter no máximo {MaxTransactionIdLength} caracteres.")
                .Build());

        if (pixAuthenticationCode is not null && pixAuthenticationCode.Length > MaxAuthenticationCodeLength)
            return Result<TransferProof?>.Failure(Error.Create()
                .WithCode(TransferErrorCodes.ProofAuthenticationCodeTooLong)
                .WithMessage($"O código de autenticação PIX pode ter no máximo {MaxAuthenticationCodeLength} caracteres.")
                .Build());

        if (cryptoTransactionId is not null && cryptoTransactionId.Length > MaxTransactionIdLength)
            return Result<TransferProof?>.Failure(Error.Create()
                .WithCode(TransferErrorCodes.ProofTransactionIdTooLong)
                .WithMessage($"O ID da transação crypto pode ter no máximo {MaxTransactionIdLength} caracteres.")
                .Build());

        return Result<TransferProof?>.Success(
            new TransferProof(pixTransactionId, pixAuthenticationCode, cryptoTransactionId));
    }
}

public sealed class AccountNodeSnapshot
{
    public AccountNodeKind Kind { get; }
    public string? BankAccountId { get; }
    public string? CryptoWalletId { get; }
    public string? ParticipantAccountId { get; }
    public string StrawManId { get; }

    internal AccountNodeSnapshot(
        AccountNodeKind kind,
        string? bankAccountId,
        string? cryptoWalletId,
        string? participantAccountId,
        string strawManId)
    {
        Kind = kind;
        BankAccountId = bankAccountId;
        CryptoWalletId = cryptoWalletId;
        ParticipantAccountId = participantAccountId;
        StrawManId = strawManId;
    }

    public static IResult<AccountNodeSnapshot> ForBankAccount(string bankAccountId, string strawManId)
    {
        bankAccountId = bankAccountId?.Trim() ?? string.Empty;
        strawManId = strawManId?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(bankAccountId))
            return Result<AccountNodeSnapshot>.Failure(Error.Create()
                .WithCode(TransferErrorCodes.BankAccountRequired)
                .WithMessage("A conta bancária é obrigatória.")
                .Build());

        if (string.IsNullOrWhiteSpace(strawManId))
            return Result<AccountNodeSnapshot>.Failure(Error.Create()
                .WithCode(TransferErrorCodes.StrawManInvalid)
                .WithMessage("O ID do laranja é obrigatório.")
                .Build());

        return Result<AccountNodeSnapshot>.Success(
            new AccountNodeSnapshot(AccountNodeKind.BankAccount, bankAccountId, null, null, strawManId));
    }

    public static IResult<AccountNodeSnapshot> ForCryptoWallet(string cryptoWalletId, string strawManId)
    {
        cryptoWalletId = cryptoWalletId?.Trim() ?? string.Empty;
        strawManId = strawManId?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(cryptoWalletId))
            return Result<AccountNodeSnapshot>.Failure(Error.Create()
                .WithCode(TransferErrorCodes.CryptoWalletRequired)
                .WithMessage("A wallet crypto é obrigatória.")
                .Build());

        if (string.IsNullOrWhiteSpace(strawManId))
            return Result<AccountNodeSnapshot>.Failure(Error.Create()
                .WithCode(TransferErrorCodes.StrawManInvalid)
                .WithMessage("O ID do laranja é obrigatório.")
                .Build());

        return Result<AccountNodeSnapshot>.Success(
            new AccountNodeSnapshot(AccountNodeKind.CryptoWallet, null, cryptoWalletId, null, strawManId));
    }

    public static IResult<AccountNodeSnapshot> ForParticipant(string participantAccountId, string strawManId)
    {
        participantAccountId = participantAccountId?.Trim() ?? string.Empty;
        strawManId = strawManId?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(participantAccountId))
            return Result<AccountNodeSnapshot>.Failure(Error.Create()
                .WithCode(TransferErrorCodes.ParticipantAccountRequired)
                .WithMessage("A conta do participante é obrigatória.")
                .Build());

        if (string.IsNullOrWhiteSpace(strawManId))
            return Result<AccountNodeSnapshot>.Failure(Error.Create()
                .WithCode(TransferErrorCodes.StrawManInvalid)
                .WithMessage("O ID do laranja é obrigatório.")
                .Build());

        return Result<AccountNodeSnapshot>.Success(
            new AccountNodeSnapshot(AccountNodeKind.Participant, null, null, participantAccountId, strawManId));
    }
}

public sealed class Transfer
{
    public string Id { get; }
    public TransferType Type { get; }
    public OnrampingMethod? OnrampingMethod { get; }
    public TransferProof? Proof { get; }
    public AccountNodeSnapshot? Source { get; }
    public AccountNodeSnapshot? Destination { get; }
    public decimal SourceAmount { get; }
    public decimal? ProducedAmount { get; }
    public Nexus.AccountNodes.Aggregates.CryptoAsset? ProducedAsset { get; }
    public Nexus.AccountNodes.Aggregates.Chain? ProducedChain { get; }
    public IReadOnlyList<string> PaymentIds { get; }
    public string? SourceBalanceId { get; }
    public string StrawManId { get; }
    public DateTime CreatedAt { get; }

    internal Transfer(
        string id,
        TransferType type,
        OnrampingMethod? onrampingMethod,
        TransferProof? proof,
        AccountNodeSnapshot? source,
        AccountNodeSnapshot? destination,
        decimal sourceAmount,
        decimal? producedAmount,
        Nexus.AccountNodes.Aggregates.CryptoAsset? producedAsset,
        Nexus.AccountNodes.Aggregates.Chain? producedChain,
        IReadOnlyList<string> paymentIds,
        string? sourceBalanceId,
        string strawManId,
        DateTime createdAt)
    {
        Id = id;
        Type = type;
        OnrampingMethod = onrampingMethod;
        Proof = proof;
        Source = source;
        Destination = destination;
        SourceAmount = sourceAmount;
        ProducedAmount = producedAmount;
        ProducedAsset = producedAsset;
        ProducedChain = producedChain;
        PaymentIds = paymentIds;
        SourceBalanceId = string.IsNullOrWhiteSpace(sourceBalanceId) ? null : sourceBalanceId.Trim();
        StrawManId = strawManId;
        CreatedAt = createdAt;
    }

    public static IResult<Transfer> Create(
        TransferType type,
        OnrampingMethod? onrampingMethod,
        TransferProof? proof,
        AccountNodeSnapshot? source,
        AccountNodeSnapshot? destination,
        decimal sourceAmount,
        decimal? producedAmount,
        Nexus.AccountNodes.Aggregates.CryptoAsset? producedAsset,
        IReadOnlyList<string> paymentIds,
        string strawManId,
        string? sourceBalanceId = null,
        Nexus.AccountNodes.Aggregates.Chain? producedChain = null)
    {
        var builder = Result.Create<Transfer>();
        strawManId = strawManId?.Trim() ?? string.Empty;

        var normalizedPaymentIds = (paymentIds ?? Array.Empty<string>())
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Select(id => id.Trim())
            .Distinct(StringComparer.Ordinal)
            .ToList();

        if (!Enum.IsDefined(type))
            builder.WithError(Error.Create()
                .WithCode(TransferErrorCodes.TypeInvalid)
                .WithMessage("O tipo de transferência informado é inválido.")
                .Build());

        if (string.IsNullOrWhiteSpace(strawManId))
            builder.WithError(Error.Create()
                .WithCode(TransferErrorCodes.StrawManInvalid)
                .WithMessage("O ID do laranja é obrigatório.")
                .Build());

        if (sourceAmount <= 0)
            builder.WithError(Error.Create()
                .WithCode(TransferErrorCodes.SourceAmountInvalid)
                .WithMessage("O valor de origem deve ser maior que zero.")
                .Build());

        if (type == TransferType.Withdrawal)
        {
            if (normalizedPaymentIds.Count == 0)
                builder.WithError(Error.Create()
                    .WithCode(TransferErrorCodes.PaymentIdsRequired)
                    .WithMessage("É necessário vincular ao menos um pagamento à transferência de saque.")
                    .Build());

            if (destination is null)
                builder.WithError(Error.Create()
                    .WithCode(TransferErrorCodes.DestinationRequired)
                    .WithMessage("O destino é obrigatório para transferências de saque.")
                    .Build());
        }
        else if (type == TransferType.Movement)
        {
            if (source is null || destination is null)
                builder.WithError(Error.Create()
                    .WithCode(TransferErrorCodes.SourceDestinationRequired)
                    .WithMessage("Origem e destino são obrigatórios para movimentações.")
                    .Build());

            var isBankToCrypto = source?.Kind == AccountNodeKind.BankAccount
                && destination?.Kind == AccountNodeKind.CryptoWallet;

            if (isBankToCrypto && onrampingMethod is null)
                builder.WithError(Error.Create()
                    .WithCode(TransferErrorCodes.OnrampingMethodRequired)
                    .WithMessage("O método de onramping é obrigatório para movimentações banco→crypto.")
                    .Build());

            if (!isBankToCrypto && onrampingMethod is not null)
                builder.WithError(Error.Create()
                    .WithCode(TransferErrorCodes.OnrampingMethodInvalid)
                    .WithMessage("O método de onramping só se aplica a movimentações banco→crypto.")
                    .Build());
        }
        else if (type == TransferType.Payout)
        {
            if (source is null)
                builder.WithError(Error.Create()
                    .WithCode(TransferErrorCodes.SourceRequired)
                    .WithMessage("A origem é obrigatória para repasses.")
                    .Build());

            if (destination is null || destination.Kind != AccountNodeKind.Participant)
                builder.WithError(Error.Create()
                    .WithCode(TransferErrorCodes.ParticipantDestinationRequired)
                    .WithMessage("O destino do repasse deve ser uma conta participante.")
                    .Build());

            if (proof is null)
                builder.WithError(Error.Create()
                    .WithCode(TransferErrorCodes.ProofRequired)
                    .WithMessage("O comprovante é obrigatório para repasses.")
                    .Build());
        }

        if (builder.ContainsError)
            return builder.Build();

        return builder.WithValue(new Transfer(
            id: string.Empty,
            type: type,
            onrampingMethod: onrampingMethod,
            proof: proof,
            source: source,
            destination: destination,
            sourceAmount: sourceAmount,
            producedAmount: producedAmount,
            producedAsset: producedAsset,
            producedChain: producedChain,
            paymentIds: normalizedPaymentIds,
            sourceBalanceId: sourceBalanceId,
            strawManId: strawManId,
            createdAt: DateTime.UtcNow)).Build();
    }
}

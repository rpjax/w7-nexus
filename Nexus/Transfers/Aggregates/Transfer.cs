using Aidan.Core.Errors;
using Aidan.Core.Patterns;
using Nexus.CryptoWallets.Aggregates;
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

public sealed class Transfer
{
    public string Id { get; }
    public TransferType Type { get; }
    public OnrampingMethod? OnrampingMethod { get; }
    public TransferProof? Proof { get; }
    public TransferOriginType? OriginType { get; }
    public TransferOriginBankAccount? OriginBankAccount { get; }
    public TransferOriginCryptoWallet? OriginCryptoWallet { get; }
    public TransferDestinationType? DestinationType { get; }
    public TransferDestinationBankAccount? DestinationBankAccount { get; }
    public TransferDestinationCryptoWallet? DestinationCryptoWallet { get; }
    public decimal SourceAmount { get; }
    public decimal? ProducedAmount { get; }
    public CryptoAsset? ProducedAsset { get; }
    public Chain? ProducedChain { get; }
    public IReadOnlyList<string> PaymentIds { get; }
    public string? SourceBalanceId { get; }
    public string StrawManId { get; }
    public DateTime CreatedAt { get; }

    internal Transfer(
        string id,
        TransferType type,
        OnrampingMethod? onrampingMethod,
        TransferProof? proof,
        TransferOriginType? originType,
        TransferOriginBankAccount? originBankAccount,
        TransferOriginCryptoWallet? originCryptoWallet,
        TransferDestinationType? destinationType,
        TransferDestinationBankAccount? destinationBankAccount,
        TransferDestinationCryptoWallet? destinationCryptoWallet,
        decimal sourceAmount,
        decimal? producedAmount,
        CryptoAsset? producedAsset,
        Chain? producedChain,
        IReadOnlyList<string> paymentIds,
        string? sourceBalanceId,
        string strawManId,
        DateTime createdAt)
    {
        Id = id;
        Type = type;
        OnrampingMethod = onrampingMethod;
        Proof = proof;
        OriginType = originType;
        OriginBankAccount = originBankAccount;
        OriginCryptoWallet = originCryptoWallet;
        DestinationType = destinationType;
        DestinationBankAccount = destinationBankAccount;
        DestinationCryptoWallet = destinationCryptoWallet;
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
        TransferOriginType? originType,
        TransferOriginBankAccount? originBankAccount,
        TransferOriginCryptoWallet? originCryptoWallet,
        TransferDestinationType? destinationType,
        TransferDestinationBankAccount? destinationBankAccount,
        TransferDestinationCryptoWallet? destinationCryptoWallet,
        decimal sourceAmount,
        decimal? producedAmount,
        CryptoAsset? producedAsset,
        IReadOnlyList<string> paymentIds,
        string strawManId,
        string? sourceBalanceId = null,
        Chain? producedChain = null)
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

        ValidateOriginSide(builder, originType, originBankAccount, originCryptoWallet);
        ValidateDestinationSide(builder, destinationType, destinationBankAccount, destinationCryptoWallet);

        if (type == TransferType.Withdrawal)
        {
            if (normalizedPaymentIds.Count == 0)
                builder.WithError(Error.Create()
                    .WithCode(TransferErrorCodes.PaymentIdsRequired)
                    .WithMessage("É necessário vincular ao menos um pagamento à transferência de saque.")
                    .Build());

            if (destinationType is null)
                builder.WithError(Error.Create()
                    .WithCode(TransferErrorCodes.DestinationRequired)
                    .WithMessage("O destino é obrigatório para transferências de saque.")
                    .Build());

            if (originType is not null)
                builder.WithError(Error.Create()
                    .WithCode(TransferErrorCodes.SourceRequired)
                    .WithMessage("Saque não deve ter origem.")
                    .Build());
        }
        else if (type == TransferType.Movement)
        {
            if (originType is null || destinationType is null)
                builder.WithError(Error.Create()
                    .WithCode(TransferErrorCodes.SourceDestinationRequired)
                    .WithMessage("Origem e destino são obrigatórios para movimentações.")
                    .Build());

            var isBankToCrypto = originType == TransferOriginType.BankAccount
                && destinationType == TransferDestinationType.CryptoWallet;

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
            if (originType != TransferOriginType.BankAccount)
                builder.WithError(Error.Create()
                    .WithCode(TransferErrorCodes.SourceRequired)
                    .WithMessage("A origem do repasse deve ser uma conta bancária.")
                    .Build());

            if (destinationType is null)
                builder.WithError(Error.Create()
                    .WithCode(TransferErrorCodes.DestinationRequired)
                    .WithMessage("O destino é obrigatório para repasses.")
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
            originType: originType,
            originBankAccount: originBankAccount,
            originCryptoWallet: originCryptoWallet,
            destinationType: destinationType,
            destinationBankAccount: destinationBankAccount,
            destinationCryptoWallet: destinationCryptoWallet,
            sourceAmount: sourceAmount,
            producedAmount: producedAmount,
            producedAsset: producedAsset,
            producedChain: producedChain,
            paymentIds: normalizedPaymentIds,
            sourceBalanceId: sourceBalanceId,
            strawManId: strawManId,
            createdAt: DateTime.UtcNow)).Build();
    }

    private static void ValidateOriginSide(
        ResultBuilder<Transfer> builder,
        TransferOriginType? originType,
        TransferOriginBankAccount? originBankAccount,
        TransferOriginCryptoWallet? originCryptoWallet)
    {
        if (originType is null)
        {
            if (originBankAccount is not null || originCryptoWallet is not null)
                builder.WithError(Error.Create()
                    .WithCode(TransferErrorCodes.InvalidAggregateState)
                    .WithMessage("Origem inconsistente na transferência.")
                    .Build());
            return;
        }

        if (originType == TransferOriginType.BankAccount)
        {
            if (originBankAccount is null || originCryptoWallet is not null)
                builder.WithError(Error.Create()
                    .WithCode(TransferErrorCodes.InvalidAggregateState)
                    .WithMessage("Origem bancária inconsistente na transferência.")
                    .Build());
        }
        else if (originType == TransferOriginType.CryptoWallet)
        {
            if (originCryptoWallet is null || originBankAccount is not null)
                builder.WithError(Error.Create()
                    .WithCode(TransferErrorCodes.InvalidAggregateState)
                    .WithMessage("Origem crypto inconsistente na transferência.")
                    .Build());
        }
    }

    private static void ValidateDestinationSide(
        ResultBuilder<Transfer> builder,
        TransferDestinationType? destinationType,
        TransferDestinationBankAccount? destinationBankAccount,
        TransferDestinationCryptoWallet? destinationCryptoWallet)
    {
        if (destinationType is null)
        {
            if (destinationBankAccount is not null || destinationCryptoWallet is not null)
                builder.WithError(Error.Create()
                    .WithCode(TransferErrorCodes.InvalidAggregateState)
                    .WithMessage("Destino inconsistente na transferência.")
                    .Build());
            return;
        }

        if (destinationType == TransferDestinationType.BankAccount)
        {
            if (destinationBankAccount is null || destinationCryptoWallet is not null)
                builder.WithError(Error.Create()
                    .WithCode(TransferErrorCodes.InvalidAggregateState)
                    .WithMessage("Destino bancário inconsistente na transferência.")
                    .Build());
        }
        else if (destinationType == TransferDestinationType.CryptoWallet)
        {
            if (destinationCryptoWallet is null || destinationBankAccount is not null)
                builder.WithError(Error.Create()
                    .WithCode(TransferErrorCodes.InvalidAggregateState)
                    .WithMessage("Destino crypto inconsistente na transferência.")
                    .Build());
        }
    }
}

using Aidan.Core.Errors;
using Aidan.Core.Patterns;
using Nexus.Transfers.Errors;

namespace Nexus.Transfers.Aggregates;

public enum TransferOriginType
{
    BankAccount = 0,
    CryptoWallet,
}

public enum TransferDestinationType
{
    BankAccount = 0,
    CryptoWallet,
}

public sealed class TransferOriginBankAccount
{
    public string BankAccountId { get; }
    public string StrawManId { get; }

    internal TransferOriginBankAccount(string bankAccountId, string strawManId)
    {
        BankAccountId = bankAccountId;
        StrawManId = strawManId;
    }

    public static IResult<TransferOriginBankAccount> Create(string bankAccountId, string strawManId)
    {
        bankAccountId = bankAccountId?.Trim() ?? string.Empty;
        strawManId = strawManId?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(bankAccountId))
            return Result<TransferOriginBankAccount>.Failure(Error.Create()
                .WithCode(TransferErrorCodes.BankAccountRequired)
                .WithMessage("A conta bancária de origem é obrigatória.")
                .Build());

        if (string.IsNullOrWhiteSpace(strawManId))
            return Result<TransferOriginBankAccount>.Failure(Error.Create()
                .WithCode(TransferErrorCodes.StrawManInvalid)
                .WithMessage("O ID do laranja é obrigatório.")
                .Build());

        return Result<TransferOriginBankAccount>.Success(
            new TransferOriginBankAccount(bankAccountId, strawManId));
    }
}

public sealed class TransferOriginCryptoWallet
{
    public string CryptoWalletId { get; }
    public string StrawManId { get; }

    internal TransferOriginCryptoWallet(string cryptoWalletId, string strawManId)
    {
        CryptoWalletId = cryptoWalletId;
        StrawManId = strawManId;
    }

    public static IResult<TransferOriginCryptoWallet> Create(string cryptoWalletId, string strawManId)
    {
        cryptoWalletId = cryptoWalletId?.Trim() ?? string.Empty;
        strawManId = strawManId?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(cryptoWalletId))
            return Result<TransferOriginCryptoWallet>.Failure(Error.Create()
                .WithCode(TransferErrorCodes.CryptoWalletRequired)
                .WithMessage("A wallet crypto de origem é obrigatória.")
                .Build());

        if (string.IsNullOrWhiteSpace(strawManId))
            return Result<TransferOriginCryptoWallet>.Failure(Error.Create()
                .WithCode(TransferErrorCodes.StrawManInvalid)
                .WithMessage("O ID do laranja é obrigatório.")
                .Build());

        return Result<TransferOriginCryptoWallet>.Success(
            new TransferOriginCryptoWallet(cryptoWalletId, strawManId));
    }
}

public sealed class TransferDestinationBankAccount
{
    public string BankAccountId { get; }
    public string StrawManId { get; }

    internal TransferDestinationBankAccount(string bankAccountId, string strawManId)
    {
        BankAccountId = bankAccountId;
        StrawManId = strawManId;
    }

    public static IResult<TransferDestinationBankAccount> Create(string bankAccountId, string strawManId)
    {
        bankAccountId = bankAccountId?.Trim() ?? string.Empty;
        strawManId = strawManId?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(bankAccountId))
            return Result<TransferDestinationBankAccount>.Failure(Error.Create()
                .WithCode(TransferErrorCodes.BankAccountRequired)
                .WithMessage("A conta bancária de destino é obrigatória.")
                .Build());

        if (string.IsNullOrWhiteSpace(strawManId))
            return Result<TransferDestinationBankAccount>.Failure(Error.Create()
                .WithCode(TransferErrorCodes.StrawManInvalid)
                .WithMessage("O ID do laranja é obrigatório.")
                .Build());

        return Result<TransferDestinationBankAccount>.Success(
            new TransferDestinationBankAccount(bankAccountId, strawManId));
    }
}

public sealed class TransferDestinationCryptoWallet
{
    public string CryptoWalletId { get; }
    public string StrawManId { get; }

    internal TransferDestinationCryptoWallet(string cryptoWalletId, string strawManId)
    {
        CryptoWalletId = cryptoWalletId;
        StrawManId = strawManId;
    }

    public static IResult<TransferDestinationCryptoWallet> Create(string cryptoWalletId, string strawManId)
    {
        cryptoWalletId = cryptoWalletId?.Trim() ?? string.Empty;
        strawManId = strawManId?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(cryptoWalletId))
            return Result<TransferDestinationCryptoWallet>.Failure(Error.Create()
                .WithCode(TransferErrorCodes.CryptoWalletRequired)
                .WithMessage("A wallet crypto de destino é obrigatória.")
                .Build());

        if (string.IsNullOrWhiteSpace(strawManId))
            return Result<TransferDestinationCryptoWallet>.Failure(Error.Create()
                .WithCode(TransferErrorCodes.StrawManInvalid)
                .WithMessage("O ID do laranja é obrigatório.")
                .Build());

        return Result<TransferDestinationCryptoWallet>.Success(
            new TransferDestinationCryptoWallet(cryptoWalletId, strawManId));
    }
}

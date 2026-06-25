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
    public string OwnerId { get; }

    internal TransferOriginBankAccount(string bankAccountId, string ownerId)
    {
        BankAccountId = bankAccountId;
        OwnerId = ownerId;
    }

    public static IResult<TransferOriginBankAccount> Create(string bankAccountId, string ownerId)
    {
        bankAccountId = bankAccountId?.Trim() ?? string.Empty;
        ownerId = ownerId?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(bankAccountId))
            return Result<TransferOriginBankAccount>.Failure(Error.Create()
                .WithCode(TransferErrorCodes.BankAccountRequired)
                .WithMessage("A conta bancária de origem é obrigatória.")
                .Build());

        if (string.IsNullOrWhiteSpace(ownerId))
            return Result<TransferOriginBankAccount>.Failure(Error.Create()
                .WithCode(TransferErrorCodes.OwnerInvalid)
                .WithMessage("O ID do dono da conta de origem é obrigatório.")
                .Build());

        return Result<TransferOriginBankAccount>.Success(
            new TransferOriginBankAccount(bankAccountId, ownerId));
    }
}

public sealed class TransferOriginCryptoWallet
{
    public string CryptoWalletId { get; }
    public string OwnerId { get; }

    internal TransferOriginCryptoWallet(string cryptoWalletId, string ownerId)
    {
        CryptoWalletId = cryptoWalletId;
        OwnerId = ownerId;
    }

    public static IResult<TransferOriginCryptoWallet> Create(string cryptoWalletId, string ownerId)
    {
        cryptoWalletId = cryptoWalletId?.Trim() ?? string.Empty;
        ownerId = ownerId?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(cryptoWalletId))
            return Result<TransferOriginCryptoWallet>.Failure(Error.Create()
                .WithCode(TransferErrorCodes.CryptoWalletRequired)
                .WithMessage("A wallet crypto de origem é obrigatória.")
                .Build());

        if (string.IsNullOrWhiteSpace(ownerId))
            return Result<TransferOriginCryptoWallet>.Failure(Error.Create()
                .WithCode(TransferErrorCodes.OwnerInvalid)
                .WithMessage("O ID do dono da wallet de origem é obrigatório.")
                .Build());

        return Result<TransferOriginCryptoWallet>.Success(
            new TransferOriginCryptoWallet(cryptoWalletId, ownerId));
    }
}

public sealed class TransferDestinationBankAccount
{
    public string BankAccountId { get; }
    public string OwnerId { get; }

    internal TransferDestinationBankAccount(string bankAccountId, string ownerId)
    {
        BankAccountId = bankAccountId;
        OwnerId = ownerId;
    }

    public static IResult<TransferDestinationBankAccount> Create(string bankAccountId, string ownerId)
    {
        bankAccountId = bankAccountId?.Trim() ?? string.Empty;
        ownerId = ownerId?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(bankAccountId))
            return Result<TransferDestinationBankAccount>.Failure(Error.Create()
                .WithCode(TransferErrorCodes.BankAccountRequired)
                .WithMessage("A conta bancária de destino é obrigatória.")
                .Build());

        if (string.IsNullOrWhiteSpace(ownerId))
            return Result<TransferDestinationBankAccount>.Failure(Error.Create()
                .WithCode(TransferErrorCodes.OwnerInvalid)
                .WithMessage("O ID do dono da conta de destino é obrigatório.")
                .Build());

        return Result<TransferDestinationBankAccount>.Success(
            new TransferDestinationBankAccount(bankAccountId, ownerId));
    }
}

public sealed class TransferDestinationCryptoWallet
{
    public string CryptoWalletId { get; }
    public string OwnerId { get; }

    internal TransferDestinationCryptoWallet(string cryptoWalletId, string ownerId)
    {
        CryptoWalletId = cryptoWalletId;
        OwnerId = ownerId;
    }

    public static IResult<TransferDestinationCryptoWallet> Create(string cryptoWalletId, string ownerId)
    {
        cryptoWalletId = cryptoWalletId?.Trim() ?? string.Empty;
        ownerId = ownerId?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(cryptoWalletId))
            return Result<TransferDestinationCryptoWallet>.Failure(Error.Create()
                .WithCode(TransferErrorCodes.CryptoWalletRequired)
                .WithMessage("A wallet crypto de destino é obrigatória.")
                .Build());

        if (string.IsNullOrWhiteSpace(ownerId))
            return Result<TransferDestinationCryptoWallet>.Failure(Error.Create()
                .WithCode(TransferErrorCodes.OwnerInvalid)
                .WithMessage("O ID do dono da wallet de destino é obrigatório.")
                .Build());

        return Result<TransferDestinationCryptoWallet>.Success(
            new TransferDestinationCryptoWallet(cryptoWalletId, ownerId));
    }
}

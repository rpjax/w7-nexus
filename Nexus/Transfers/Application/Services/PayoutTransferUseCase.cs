using Aidan.Core.Errors;
using Aidan.Core.Patterns;
using Nexus.Accounts.Application.Contracts;
using Nexus.Authorization;
using Nexus.BankAccounts.Application.Contracts;
using Nexus.CryptoWallets.Application.Contracts;
using Nexus.Transfers.Aggregates;
using Nexus.Transfers.Application.Contracts;
using Nexus.Transfers.Errors;

namespace Nexus.Transfers.Application.Services;

public sealed class PayoutTransferUseCase : IPayoutTransferUseCase
{
    private readonly IAccountRepository _accounts;
    private readonly IBankAccountRepository _bankAccounts;
    private readonly ICryptoWalletRepository _cryptoWallets;
    private readonly ITransferRepository _transfers;

    public PayoutTransferUseCase(
        IAccountRepository accounts,
        IBankAccountRepository bankAccounts,
        ICryptoWalletRepository cryptoWallets,
        ITransferRepository transfers)
    {
        _accounts = accounts;
        _bankAccounts = bankAccounts;
        _cryptoWallets = cryptoWallets;
        _transfers = transfers;
    }

    public async Task<IResult<Transfer>> ExecuteAsync(
        PayoutTransferRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var strawManId = request.StrawManId?.Trim() ?? string.Empty;
        var balanceId = request.SourceBalanceId?.Trim() ?? string.Empty;

        var strawManValidation = ValidateStrawMan(strawManId);
        if (strawManValidation is not null)
            return Result<Transfer>.Failure(strawManValidation.Errors);

        if (string.IsNullOrWhiteSpace(balanceId))
            return Result<Transfer>.Failure(Error.Create()
                .WithCode(TransferErrorCodes.BalanceIdRequired)
                .WithMessage("O ID do saldo de origem é obrigatório.")
                .Build());

        if (request.SourceAmount <= 0)
            return Result<Transfer>.Failure(Error.Create()
                .WithCode(TransferErrorCodes.SourceAmountInvalid)
                .WithMessage("O valor de origem deve ser maior que zero.")
                .Build());

        var hasBankDest = !string.IsNullOrWhiteSpace(request.DestinationBankAccountId);
        var hasCryptoDest = !string.IsNullOrWhiteSpace(request.DestinationCryptoWalletId);

        if (hasBankDest == hasCryptoDest)
            return Result<Transfer>.Failure(Error.Create()
                .WithCode(TransferErrorCodes.DestinationRequired)
                .WithMessage("Informe exatamente um destino: conta bancária ou wallet crypto.")
                .Build());

        var proofResult = TransferProof.Create(
            request.PixTransactionId,
            request.PixAuthenticationCode,
            request.CryptoTransactionId,
            required: true);

        if (proofResult.IsFailure)
            return Result<Transfer>.Failure(proofResult.Errors);

        if (string.IsNullOrWhiteSpace(request.SourceBankAccountId))
            return Result<Transfer>.Failure(Error.Create()
                .WithCode(TransferErrorCodes.SourceRequired)
                .WithMessage("Informe a conta bancária de origem para o repasse.")
                .Build());

        var sourceAccount = _bankAccounts.AsQueryable()
            .FirstOrDefault(a => a.Id == request.SourceBankAccountId.Trim());

        if (sourceAccount is null)
            return Result<Transfer>.Failure(Error.Create()
                .WithCode(TransferErrorCodes.BankAccountNotFound)
                .WithMessage($"A conta bancária '{request.SourceBankAccountId}' não foi encontrada.")
                .Build());

        if (!string.Equals(sourceAccount.OwnerId, strawManId, StringComparison.Ordinal))
            return Result<Transfer>.Failure(Error.Create()
                .WithCode(TransferErrorCodes.BankAccountMismatch)
                .WithMessage("A conta bancária de origem não pertence ao laranja informado.")
                .Build());

        var debitResult = sourceAccount.DebitPartialBalance(balanceId, request.SourceAmount);
        if (debitResult.IsFailure)
            return Result<Transfer>.Failure(debitResult.Errors);

        await _bankAccounts.UpdateAsync(sourceAccount);

        var originResult = TransferOriginBankAccount.Create(sourceAccount.Id, sourceAccount.OwnerId);
        if (originResult.IsFailure)
            return Result<Transfer>.Failure(originResult.Errors);

        TransferDestinationType destinationType;
        TransferDestinationBankAccount? destinationBankAccount = null;
        TransferDestinationCryptoWallet? destinationCryptoWallet = null;

        if (hasBankDest)
        {
            destinationType = TransferDestinationType.BankAccount;

            var destinationAccount = _bankAccounts.AsQueryable()
                .FirstOrDefault(a => a.Id == request.DestinationBankAccountId!.Trim());

            if (destinationAccount is null)
                return Result<Transfer>.Failure(Error.Create()
                    .WithCode(TransferErrorCodes.BankAccountNotFound)
                    .WithMessage($"A conta bancária '{request.DestinationBankAccountId}' não foi encontrada.")
                    .Build());

            var destResult = TransferDestinationBankAccount.Create(
                destinationAccount.Id,
                destinationAccount.OwnerId);
            if (destResult.IsFailure)
                return Result<Transfer>.Failure(destResult.Errors);
            destinationBankAccount = destResult.Value;
        }
        else
        {
            destinationType = TransferDestinationType.CryptoWallet;

            var destinationWallet = _cryptoWallets.AsQueryable()
                .FirstOrDefault(w => w.Id == request.DestinationCryptoWalletId!.Trim());

            if (destinationWallet is null)
                return Result<Transfer>.Failure(Error.Create()
                    .WithCode(TransferErrorCodes.CryptoWalletNotFound)
                    .WithMessage($"A wallet crypto '{request.DestinationCryptoWalletId}' não foi encontrada.")
                    .Build());

            var destResult = TransferDestinationCryptoWallet.Create(
                destinationWallet.Id,
                destinationWallet.OwnerId);
            if (destResult.IsFailure)
                return Result<Transfer>.Failure(destResult.Errors);
            destinationCryptoWallet = destResult.Value;
        }

        var transferResult = Transfer.Create(
            TransferType.Payout,
            onrampingMethod: null,
            proofResult.Value,
            TransferOriginType.BankAccount,
            originResult.Value,
            originCryptoWallet: null,
            destinationType,
            destinationBankAccount,
            destinationCryptoWallet,
            request.SourceAmount,
            producedAmount: null,
            producedAsset: null,
            Array.Empty<string>(),
            strawManId,
            sourceBalanceId: balanceId);

        if (transferResult.IsFailure)
            return transferResult;

        var persisted = await _transfers.CreateAsync(transferResult.Value!);
        return Result<Transfer>.Success(persisted);
    }

    private IResult? ValidateStrawMan(string strawManId)
    {
        if (string.IsNullOrWhiteSpace(strawManId))
            return Result.Failure(Error.Create()
                .WithCode(TransferErrorCodes.StrawManInvalid)
                .WithMessage("O ID do laranja é obrigatório.")
                .Build());

        var account = _accounts.AsQueryable().FirstOrDefault(a => a.Id == strawManId);
        if (account is null)
            return Result.Failure(Error.Create()
                .WithCode(TransferErrorCodes.StrawManNotFound)
                .WithMessage($"A conta laranja '{strawManId}' não foi encontrada.")
                .Build());

        if (!account.Roles.Contains(Roles.StrawMan, StringComparer.Ordinal))
            return Result.Failure(Error.Create()
                .WithCode(TransferErrorCodes.StrawManRoleRequired)
                .WithMessage($"A conta '{strawManId}' não possui o perfil de laranja.")
                .Build());

        return null;
    }
}

using Aidan.Core.Errors;
using Aidan.Core.Patterns;
using Nexus.AccountNodes.Application.Contracts;
using Nexus.Accounts.Application.Contracts;
using Nexus.Authorization;
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

        var participantAccountId = request.ParticipantAccountId?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(participantAccountId))
            return Result<Transfer>.Failure(Error.Create()
                .WithCode(TransferErrorCodes.ParticipantAccountRequired)
                .WithMessage("A conta do participante é obrigatória.")
                .Build());

        var participant = _accounts.AsQueryable().FirstOrDefault(a => a.Id == participantAccountId);
        if (participant is null)
            return Result<Transfer>.Failure(Error.Create()
                .WithCode(TransferErrorCodes.ParticipantAccountNotFound)
                .WithMessage($"A conta participante '{participantAccountId}' não foi encontrada.")
                .Build());

        var proofResult = TransferProof.Create(
            request.PixTransactionId,
            request.PixAuthenticationCode,
            request.CryptoTransactionId,
            required: true);

        if (proofResult.IsFailure)
            return Result<Transfer>.Failure(proofResult.Errors);

        AccountNodeSnapshot? sourceSnapshot = null;

        if (!string.IsNullOrWhiteSpace(request.SourceBankAccountId))
        {
            var sourceAccount = _bankAccounts.AsQueryable()
                .FirstOrDefault(a => a.Id == request.SourceBankAccountId.Trim());

            if (sourceAccount is null)
                return Result<Transfer>.Failure(Error.Create()
                    .WithCode(TransferErrorCodes.BankAccountNotFound)
                    .WithMessage($"A conta bancária '{request.SourceBankAccountId}' não foi encontrada.")
                    .Build());

            if (!string.Equals(sourceAccount.StrawManId, strawManId, StringComparison.Ordinal))
                return Result<Transfer>.Failure(Error.Create()
                    .WithCode(TransferErrorCodes.BankAccountMismatch)
                    .WithMessage("A conta bancária de origem não pertence ao laranja informado.")
                    .Build());

            var debitResult = sourceAccount.DebitPartialBalance(balanceId, request.SourceAmount);
            if (debitResult.IsFailure)
                return Result<Transfer>.Failure(debitResult.Errors);

            await _bankAccounts.UpdateAsync(sourceAccount);

            var sourceResult = AccountNodeSnapshot.ForBankAccount(sourceAccount.Id, strawManId);
            if (sourceResult.IsFailure)
                return Result<Transfer>.Failure(sourceResult.Errors);
            sourceSnapshot = sourceResult.Value;
        }
        else
        {
            return Result<Transfer>.Failure(Error.Create()
                .WithCode(TransferErrorCodes.SourceRequired)
                .WithMessage("Informe a conta bancária de origem para o repasse.")
                .Build());
        }

        var destResult = AccountNodeSnapshot.ForParticipant(participantAccountId, strawManId);
        if (destResult.IsFailure)
            return Result<Transfer>.Failure(destResult.Errors);

        var transferResult = Transfer.Create(
            TransferType.Payout,
            onrampingMethod: null,
            proofResult.Value,
            sourceSnapshot,
            destResult.Value,
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

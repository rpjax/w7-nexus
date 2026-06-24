using Aidan.Core.Errors;
using Aidan.Core.Patterns;
using Nexus.AccountNodes.Aggregates;
using Nexus.AccountNodes.Application.Contracts;
using Nexus.Accounts.Application.Contracts;
using Nexus.Authorization;
using Nexus.Transfers.Aggregates;
using Nexus.Transfers.Application.Contracts;
using Nexus.Transfers.Errors;

namespace Nexus.Transfers.Application.Services;

public sealed class MovementTransferUseCase : IMovementTransferUseCase
{
    private readonly IAccountRepository _accounts;
    private readonly IBankAccountRepository _bankAccounts;
    private readonly ICryptoWalletRepository _cryptoWallets;
    private readonly ITransferRepository _transfers;
    private readonly IBalanceSplitCalculationService _splitCalculation;

    public MovementTransferUseCase(
        IAccountRepository accounts,
        IBankAccountRepository bankAccounts,
        ICryptoWalletRepository cryptoWallets,
        ITransferRepository transfers,
        IBalanceSplitCalculationService splitCalculation)
    {
        _accounts = accounts;
        _bankAccounts = bankAccounts;
        _cryptoWallets = cryptoWallets;
        _transfers = transfers;
        _splitCalculation = splitCalculation;
    }

    public async Task<IResult<Transfer>> ExecuteAsync(
        MovementTransferRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var builder = Result.Create<Transfer>();
        var strawManId = request.StrawManId?.Trim() ?? string.Empty;
        var balanceId = request.SourceBalanceId?.Trim() ?? string.Empty;

        var strawManValidation = ValidateStrawMan(strawManId);
        if (strawManValidation is not null)
            return Result<Transfer>.Failure(strawManValidation.Errors);

        if (string.IsNullOrWhiteSpace(balanceId))
            builder.WithError(Error.Create()
                .WithCode(TransferErrorCodes.BalanceIdRequired)
                .WithMessage("O ID do saldo de origem é obrigatório.")
                .Build());

        if (request.SourceAmount <= 0)
            builder.WithError(Error.Create()
                .WithCode(TransferErrorCodes.SourceAmountInvalid)
                .WithMessage("O valor de origem deve ser maior que zero.")
                .Build());

        var hasBankSource = !string.IsNullOrWhiteSpace(request.SourceBankAccountId);
        var hasCryptoSource = !string.IsNullOrWhiteSpace(request.SourceCryptoWalletId);

        if (hasBankSource == hasCryptoSource)
        {
            builder.WithError(Error.Create()
                .WithCode(TransferErrorCodes.SourceRequired)
                .WithMessage("Informe exatamente uma origem: conta bancária ou wallet crypto.")
                .Build());
        }

        var hasBankDest = !string.IsNullOrWhiteSpace(request.DestinationBankAccountId);
        var hasCryptoDest = !string.IsNullOrWhiteSpace(request.DestinationCryptoWalletId);

        if (hasBankDest == hasCryptoDest)
        {
            builder.WithError(Error.Create()
                .WithCode(TransferErrorCodes.DestinationRequired)
                .WithMessage("Informe exatamente um destino: conta bancária ou wallet crypto.")
                .Build());
        }

        if (builder.ContainsError)
            return builder.Build();

        AccountNodeSnapshot? sourceSnapshot = null;
        AccountNodeSnapshot? destSnapshot = null;
        BankBalance? debitedBankBalance = null;
        CryptoBalance? debitedCryptoBalance = null;

        if (hasBankSource)
        {
            var sourceAccount = _bankAccounts.AsQueryable()
                .FirstOrDefault(a => a.Id == request.SourceBankAccountId!.Trim());

            if (sourceAccount is null)
                return Result<Transfer>.Failure(NotFoundBank(request.SourceBankAccountId!).Errors);

            if (!string.Equals(sourceAccount.StrawManId, strawManId, StringComparison.Ordinal))
                return Result<Transfer>.Failure(MismatchBank().Errors);

            var debitResult = sourceAccount.DebitPartialBalance(balanceId, request.SourceAmount);
            if (debitResult.IsFailure)
                return Result<Transfer>.Failure(debitResult.Errors);

            debitedBankBalance = debitResult.Value!.DebitedBalance;
            await _bankAccounts.UpdateAsync(sourceAccount);

            var sourceResult = AccountNodeSnapshot.ForBankAccount(sourceAccount.Id, sourceAccount.StrawManId);
            if (sourceResult.IsFailure)
                return Result<Transfer>.Failure(sourceResult.Errors);
            sourceSnapshot = sourceResult.Value;
        }
        else
        {
            var sourceWallet = _cryptoWallets.AsQueryable()
                .FirstOrDefault(w => w.Id == request.SourceCryptoWalletId!.Trim());

            if (sourceWallet is null)
                return Result<Transfer>.Failure(NotFoundCrypto(request.SourceCryptoWalletId!).Errors);

            if (!string.Equals(sourceWallet.StrawManId, strawManId, StringComparison.Ordinal))
                return Result<Transfer>.Failure(MismatchCrypto().Errors);

            var debitResult = sourceWallet.DebitPartialBalance(balanceId, request.SourceAmount);
            if (debitResult.IsFailure)
                return Result<Transfer>.Failure(debitResult.Errors);

            debitedCryptoBalance = debitResult.Value!.DebitedBalance;
            await _cryptoWallets.UpdateAsync(sourceWallet);

            var sourceResult = AccountNodeSnapshot.ForCryptoWallet(sourceWallet.Id, sourceWallet.StrawManId);
            if (sourceResult.IsFailure)
                return Result<Transfer>.Failure(sourceResult.Errors);
            sourceSnapshot = sourceResult.Value;
        }

        var isBankToCrypto = hasBankSource && hasCryptoDest;
        var isCryptoToBank = hasCryptoSource && hasBankDest;
        var isCryptoToCrypto = hasCryptoSource && hasCryptoDest;
        OnrampingMethod? onrampingMethod = request.OnrampingMethod;
        Chain? producedChain = request.ProducedChain;

        if (isBankToCrypto)
        {
            if (onrampingMethod is null)
                return Result<Transfer>.Failure(Error.Create()
                    .WithCode(TransferErrorCodes.OnrampingMethodRequired)
                    .WithMessage("O método de onramping é obrigatório para movimentações banco→crypto.")
                    .Build());

            if (request.ProducedAmount is null or <= 0)
                return Result<Transfer>.Failure(Error.Create()
                    .WithCode(TransferErrorCodes.ProducedAmountRequired)
                    .WithMessage("O valor produzido é obrigatório para movimentações banco→crypto.")
                    .Build());

            if (request.ProducedAsset is null)
                return Result<Transfer>.Failure(Error.Create()
                    .WithCode(TransferErrorCodes.ProducedAssetRequired)
                    .WithMessage("O ativo produzido é obrigatório para movimentações banco→crypto.")
                    .Build());

            if (producedChain is null)
                return Result<Transfer>.Failure(Error.Create()
                    .WithCode(TransferErrorCodes.ProducedChainRequired)
                    .WithMessage("A rede blockchain produzida é obrigatória para movimentações banco→crypto.")
                    .Build());

            if (!Enum.IsDefined(producedChain.Value))
                return Result<Transfer>.Failure(Error.Create()
                    .WithCode(TransferErrorCodes.ProducedChainInvalid)
                    .WithMessage("A rede blockchain produzida é inválida.")
                    .Build());

            if (!request.ProducedAsset.Value.IsSupportedOnChain(producedChain.Value))
                return Result<Transfer>.Failure(Error.Create()
                    .WithCode(TransferErrorCodes.AssetChainMismatch)
                    .WithMessage("O ativo produzido não é suportado na rede informada.")
                    .Build());
        }
        else if (isCryptoToBank)
        {
            if (request.ProducedAmount is null or <= 0)
                return Result<Transfer>.Failure(Error.Create()
                    .WithCode(TransferErrorCodes.ProducedAmountRequired)
                    .WithMessage("O valor produzido em BRL é obrigatório para movimentações crypto→banco.")
                    .Build());

            onrampingMethod = null;
            producedChain = null;
        }
        else if (isCryptoToCrypto)
        {
            producedChain = debitedCryptoBalance!.Chain;
            onrampingMethod = null;
        }
        else if (onrampingMethod is not null || producedChain is not null)
        {
            return Result<Transfer>.Failure(Error.Create()
                .WithCode(TransferErrorCodes.OnrampingMethodInvalid)
                .WithMessage("O método de onramping e a rede produzida só se aplicam a movimentações banco→crypto.")
                .Build());
        }

        var proofResult = TransferProof.Create(
            request.PixTransactionId,
            request.PixAuthenticationCode,
            request.CryptoTransactionId,
            required: false);

        if (proofResult.IsFailure)
            return Result<Transfer>.Failure(proofResult.Errors);

        BankAccount? destinationBankAccount = null;
        CryptoWallet? destinationCryptoWallet = null;

        if (hasBankDest)
        {
            destinationBankAccount = _bankAccounts.AsQueryable()
                .FirstOrDefault(a => a.Id == request.DestinationBankAccountId!.Trim());

            if (destinationBankAccount is null)
                return Result<Transfer>.Failure(NotFoundBank(request.DestinationBankAccountId!).Errors);

            var destSnapResult = AccountNodeSnapshot.ForBankAccount(
                destinationBankAccount.Id,
                destinationBankAccount.StrawManId);
            if (destSnapResult.IsFailure)
                return Result<Transfer>.Failure(destSnapResult.Errors);
            destSnapshot = destSnapResult.Value;
        }
        else
        {
            destinationCryptoWallet = _cryptoWallets.AsQueryable()
                .FirstOrDefault(w => w.Id == request.DestinationCryptoWalletId!.Trim());

            if (destinationCryptoWallet is null)
                return Result<Transfer>.Failure(NotFoundCrypto(request.DestinationCryptoWalletId!).Errors);

            if (isBankToCrypto
                && producedChain is not null
                && !destinationCryptoWallet.HasAddressForNamespace(producedChain.Value.GetNamespace()))
            {
                return Result<Transfer>.Failure(Error.Create()
                    .WithCode(TransferErrorCodes.ProducedChainNamespaceMismatch)
                    .WithMessage("A wallet de destino não possui endereço para o namespace da rede informada.")
                    .Build());
            }

            var destSnapResult = AccountNodeSnapshot.ForCryptoWallet(
                destinationCryptoWallet.Id,
                destinationCryptoWallet.StrawManId);
            if (destSnapResult.IsFailure)
                return Result<Transfer>.Failure(destSnapResult.Errors);
            destSnapshot = destSnapResult.Value;
        }

        var transferResult = Transfer.Create(
            TransferType.Movement,
            onrampingMethod,
            proofResult.Value,
            sourceSnapshot,
            destSnapshot,
            request.SourceAmount,
            request.ProducedAmount,
            request.ProducedAsset,
            Array.Empty<string>(),
            strawManId,
            sourceBalanceId: balanceId,
            producedChain: producedChain);

        if (transferResult.IsFailure)
            return transferResult;

        var persistedTransfer = await _transfers.CreateAsync(transferResult.Value!);

        IReadOnlyList<string> appliedFeeIds;
        IReadOnlyList<BalanceSplitSnapshot> originalSplits;
        BalanceOriginSnapshot balanceOrigin;

        if (debitedBankBalance is not null)
        {
            appliedFeeIds = debitedBankBalance.AppliedStrawManFeeIds;
            originalSplits = debitedBankBalance.SplitSnapshot;
            balanceOrigin = debitedBankBalance.OriginSnapshot;
        }
        else
        {
            appliedFeeIds = debitedCryptoBalance!.AppliedStrawManFeeIds;
            originalSplits = debitedCryptoBalance.SplitSnapshot;
            balanceOrigin = debitedCryptoBalance.OriginSnapshot;
        }

        var destinationStrawManId = hasBankDest
            ? destinationBankAccount!.StrawManId
            : destinationCryptoWallet!.StrawManId;

        var creditBaseAmount = isBankToCrypto
            ? request.ProducedAmount!.Value
            : isCryptoToBank
                ? request.ProducedAmount!.Value
                : request.SourceAmount;

        var splitResult = await _splitCalculation.CalculateForCreditAsync(
            destinationStrawManId,
            creditBaseAmount,
            originalSplits,
            appliedFeeIds,
            cancellationToken);

        if (splitResult.IsFailure)
            return Result<Transfer>.Failure(splitResult.Errors);

        if (hasBankDest)
        {
            var destAccount = destinationBankAccount!;

            var balanceResult = BankBalance.Create(
                creditBaseAmount,
                persistedTransfer.Id,
                    splitResult.Value!.SplitSnapshot,
                    splitResult.Value.AppliedStrawManFeeIds,
                    balanceOrigin);

            if (balanceResult.IsFailure)
                return Result<Transfer>.Failure(balanceResult.Errors);

            var creditResult = destAccount.CreditBalance(balanceResult.Value!);
            if (creditResult.IsFailure)
                return Result<Transfer>.Failure(creditResult.Errors);

            await _bankAccounts.UpdateAsync(destAccount);
        }
        else
        {
            var destWallet = destinationCryptoWallet!;

            var creditAmount = isBankToCrypto ? request.ProducedAmount!.Value : request.SourceAmount;
            var creditAsset = isBankToCrypto
                ? request.ProducedAsset!.Value
                : debitedCryptoBalance!.Asset;
            var creditChain = isBankToCrypto
                ? producedChain!.Value
                : debitedCryptoBalance!.Chain;

            var balanceResult = CryptoBalance.Create(
                creditChain,
                creditAsset,
                creditAmount,
                persistedTransfer.Id,
                    splitResult.Value!.SplitSnapshot,
                    splitResult.Value.AppliedStrawManFeeIds,
                    balanceOrigin);

            if (balanceResult.IsFailure)
                return Result<Transfer>.Failure(balanceResult.Errors);

            var creditResult = destWallet.CreditBalance(balanceResult.Value!);
            if (creditResult.IsFailure)
                return Result<Transfer>.Failure(creditResult.Errors);

            await _cryptoWallets.UpdateAsync(destWallet);
        }

        return Result<Transfer>.Success(persistedTransfer);
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

    private static IResult NotFoundBank(string id) =>
        Result.Failure(Error.Create()
            .WithCode(TransferErrorCodes.BankAccountNotFound)
            .WithMessage($"A conta bancária '{id}' não foi encontrada.")
            .Build());

    private static IResult NotFoundCrypto(string id) =>
        Result.Failure(Error.Create()
            .WithCode(TransferErrorCodes.CryptoWalletNotFound)
            .WithMessage($"A wallet crypto '{id}' não foi encontrada.")
            .Build());

    private static IResult MismatchBank() =>
        Result.Failure(Error.Create()
            .WithCode(TransferErrorCodes.BankAccountMismatch)
            .WithMessage("A conta bancária não pertence ao laranja informado.")
            .Build());

    private static IResult MismatchCrypto() =>
        Result.Failure(Error.Create()
            .WithCode(TransferErrorCodes.CryptoWalletMismatch)
            .WithMessage("A wallet crypto não pertence ao laranja informado.")
            .Build());
}

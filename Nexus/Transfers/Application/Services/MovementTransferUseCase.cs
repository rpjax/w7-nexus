using Aidan.Core.Errors;
using Aidan.Core.Patterns;
using Nexus.Accounts.Application.Contracts;
using Nexus.Authorization;
using Nexus.BankAccounts.Aggregates;
using Nexus.BankAccounts.Application.Contracts;
using Nexus.CryptoWallets.Aggregates;
using Nexus.CryptoWallets.Application.Contracts;
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

        TransferOriginType originType;
        TransferOriginBankAccount? originBankAccount = null;
        TransferOriginCryptoWallet? originCryptoWallet = null;
        BankBalance? debitedBankBalance = null;
        CryptoBalance? debitedCryptoBalance = null;

        if (hasBankSource)
        {
            originType = TransferOriginType.BankAccount;

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

            var sourceResult = TransferOriginBankAccount.Create(sourceAccount.Id, sourceAccount.StrawManId);
            if (sourceResult.IsFailure)
                return Result<Transfer>.Failure(sourceResult.Errors);
            originBankAccount = sourceResult.Value;
        }
        else
        {
            originType = TransferOriginType.CryptoWallet;

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

            var sourceResult = TransferOriginCryptoWallet.Create(sourceWallet.Id, sourceWallet.StrawManId);
            if (sourceResult.IsFailure)
                return Result<Transfer>.Failure(sourceResult.Errors);
            originCryptoWallet = sourceResult.Value;
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

        TransferDestinationType destinationType;
        TransferDestinationBankAccount? destinationBankAccount = null;
        TransferDestinationCryptoWallet? destinationCryptoWallet = null;
        BankAccount? destinationBankAccountEntity = null;
        CryptoWallet? destinationCryptoWalletEntity = null;

        if (hasBankDest)
        {
            destinationType = TransferDestinationType.BankAccount;

            destinationBankAccountEntity = _bankAccounts.AsQueryable()
                .FirstOrDefault(a => a.Id == request.DestinationBankAccountId!.Trim());

            if (destinationBankAccountEntity is null)
                return Result<Transfer>.Failure(NotFoundBank(request.DestinationBankAccountId!).Errors);

            var destResult = TransferDestinationBankAccount.Create(
                destinationBankAccountEntity.Id,
                destinationBankAccountEntity.StrawManId);
            if (destResult.IsFailure)
                return Result<Transfer>.Failure(destResult.Errors);
            destinationBankAccount = destResult.Value;
        }
        else
        {
            destinationType = TransferDestinationType.CryptoWallet;

            destinationCryptoWalletEntity = _cryptoWallets.AsQueryable()
                .FirstOrDefault(w => w.Id == request.DestinationCryptoWalletId!.Trim());

            if (destinationCryptoWalletEntity is null)
                return Result<Transfer>.Failure(NotFoundCrypto(request.DestinationCryptoWalletId!).Errors);

            if (isBankToCrypto
                && producedChain is not null
                && !destinationCryptoWalletEntity.HasAddressForNamespace(producedChain.Value.GetNamespace()))
            {
                return Result<Transfer>.Failure(Error.Create()
                    .WithCode(TransferErrorCodes.ProducedChainNamespaceMismatch)
                    .WithMessage("A wallet de destino não possui endereço para o namespace da rede informada.")
                    .Build());
            }

            var destResult = TransferDestinationCryptoWallet.Create(
                destinationCryptoWalletEntity.Id,
                destinationCryptoWalletEntity.StrawManId);
            if (destResult.IsFailure)
                return Result<Transfer>.Failure(destResult.Errors);
            destinationCryptoWallet = destResult.Value;
        }

        var transferResult = Transfer.Create(
            TransferType.Movement,
            onrampingMethod,
            proofResult.Value,
            originType,
            originBankAccount,
            originCryptoWallet,
            destinationType,
            destinationBankAccount,
            destinationCryptoWallet,
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
        IReadOnlyList<TransferBalanceSplit> originalSplits;

        if (debitedBankBalance is not null)
        {
            appliedFeeIds = debitedBankBalance.AppliedStrawManFeeIds;
            originalSplits = BalanceSplitMapping.FromBankSplits(debitedBankBalance.Splits);
        }
        else
        {
            appliedFeeIds = debitedCryptoBalance!.AppliedStrawManFeeIds;
            originalSplits = BalanceSplitMapping.FromCryptoSplits(debitedCryptoBalance.Splits);
        }

        var destinationStrawManId = hasBankDest
            ? destinationBankAccountEntity!.StrawManId
            : destinationCryptoWalletEntity!.StrawManId;

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
            var destAccount = destinationBankAccountEntity!;
            IResult<BankBalanceOrigin> bankOriginResult = debitedBankBalance is not null
                ? Result<BankBalanceOrigin>.Success(debitedBankBalance.Origin)
                : CreateBankOriginFromCrypto(debitedCryptoBalance!.Origin);

            if (bankOriginResult.IsFailure)
                return Result<Transfer>.Failure(bankOriginResult.Errors);

            var balanceResult = BankBalance.Create(
                creditBaseAmount,
                persistedTransfer.Id,
                BalanceSplitMapping.ToBankSplits(splitResult.Value!.Splits),
                splitResult.Value.AppliedStrawManFeeIds,
                bankOriginResult.Value!);

            if (balanceResult.IsFailure)
                return Result<Transfer>.Failure(balanceResult.Errors);

            var creditResult = destAccount.CreditBalance(balanceResult.Value!);
            if (creditResult.IsFailure)
                return Result<Transfer>.Failure(creditResult.Errors);

            await _bankAccounts.UpdateAsync(destAccount);
        }
        else
        {
            var destWallet = destinationCryptoWalletEntity!;

            var creditAmount = isBankToCrypto ? request.ProducedAmount!.Value : request.SourceAmount;
            var creditAsset = isBankToCrypto
                ? request.ProducedAsset!.Value
                : debitedCryptoBalance!.Asset;
            var creditChain = isBankToCrypto
                ? producedChain!.Value
                : debitedCryptoBalance!.Chain;

            IResult<CryptoBalanceOrigin> cryptoOriginResult = debitedCryptoBalance is not null
                ? Result<CryptoBalanceOrigin>.Success(debitedCryptoBalance.Origin)
                : CreateCryptoOriginFromBank(debitedBankBalance!.Origin);

            if (cryptoOriginResult.IsFailure)
                return Result<Transfer>.Failure(cryptoOriginResult.Errors);

            var balanceResult = CryptoBalance.Create(
                creditChain,
                creditAsset,
                creditAmount,
                persistedTransfer.Id,
                BalanceSplitMapping.ToCryptoSplits(splitResult.Value!.Splits),
                splitResult.Value.AppliedStrawManFeeIds,
                cryptoOriginResult.Value!);

            if (balanceResult.IsFailure)
                return Result<Transfer>.Failure(balanceResult.Errors);

            var creditResult = destWallet.CreditBalance(balanceResult.Value!);
            if (creditResult.IsFailure)
                return Result<Transfer>.Failure(creditResult.Errors);

            await _cryptoWallets.UpdateAsync(destWallet);
        }

        return Result<Transfer>.Success(persistedTransfer);
    }

    private static IResult<BankBalanceOrigin> CreateBankOriginFromCrypto(CryptoBalanceOrigin origin) =>
        BankBalanceOrigin.Create(origin.OperationId, origin.OperatorId, origin.StrawManId);

    private static IResult<CryptoBalanceOrigin> CreateCryptoOriginFromBank(BankBalanceOrigin origin) =>
        CryptoBalanceOrigin.Create(origin.OperationId, origin.OperatorId, origin.StrawManId);

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

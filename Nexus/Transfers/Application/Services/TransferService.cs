using Aidan.Core.Errors;
using Aidan.Core.Linq.Extensions;
using Aidan.Core.Patterns;
using Nexus.Accounts.Application.Contracts;
using Nexus.Authorization;
using Nexus.BankAccounts.Aggregates;
using Nexus.BankAccounts.Application.Contracts;
using Nexus.CryptoWallets.Aggregates;
using Nexus.CryptoWallets.Application.Contracts;
using Nexus.Payments.Aggregates;
using Nexus.Payments.Application.Contracts;
using Nexus.Transfers.Aggregates;
using Nexus.Transfers.Application.Contracts;
using Nexus.Transfers.Application.Models;
using Nexus.Transfers.Application.Requests;
using Nexus.Transfers.Errors;

namespace Nexus.Transfers.Application.Services;

public sealed class TransferService : ITransferService
{
    private readonly IAccountRepository _accounts;
    private readonly IPaymentRepository _payments;
    private readonly IBankAccountRepository _bankAccounts;
    private readonly ICryptoWalletRepository _cryptoWallets;
    private readonly IBankBalanceService _bankBalances;
    private readonly ICryptoBalanceService _cryptoBalances;
    private readonly ITransferRepository _transfers;
    private readonly IBalanceSplitCalculationService _splitCalculation;
    private readonly ITransferTimelineQueryService _timeline;

    public TransferService(
        IAccountRepository accounts,
        IPaymentRepository payments,
        IBankAccountRepository bankAccounts,
        ICryptoWalletRepository cryptoWallets,
        IBankBalanceService bankBalances,
        ICryptoBalanceService cryptoBalances,
        ITransferRepository transfers,
        IBalanceSplitCalculationService splitCalculation,
        ITransferTimelineQueryService timeline)
    {
        _accounts = accounts;
        _payments = payments;
        _bankAccounts = bankAccounts;
        _cryptoWallets = cryptoWallets;
        _bankBalances = bankBalances;
        _cryptoBalances = cryptoBalances;
        _transfers = transfers;
        _splitCalculation = splitCalculation;
        _timeline = timeline;
    }

    public async Task<IResult<Transfer>> ExecuteBankAccountMovementAsync(
        BankAccountMovementRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var balanceId = request.SourceBalanceId?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(balanceId))
            return Result<Transfer>.Failure(Error.Create()
                .WithCode(TransferErrorCodes.BalanceIdRequired)
                .WithMessage("O ID do saldo de origem é obrigatório.")
                .Build());

        if (request.Amount <= 0)
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

        var sourceBalanceResult = await _bankBalances.GetByIdAsync(balanceId);
        if (sourceBalanceResult.IsFailure)
            return Result<Transfer>.Failure(sourceBalanceResult.Errors);

        var sourceBalance = sourceBalanceResult.Value!;

        var sourceAccount = await _bankAccounts.AsQueryable()
            .Where(a => a.Id == sourceBalance.BankAccountId)
            .FirstOrDefaultAsync();
        if (sourceAccount is null)
            return Result<Transfer>.Failure(NotFoundBank(sourceBalance.BankAccountId).Errors);

        var strawManId = sourceAccount.OwnerId;

        var strawManValidation = await ValidateStrawManAsync(strawManId);
        if (strawManValidation is not null)
            return Result<Transfer>.Failure(strawManValidation.Errors);

        var debitResult = await _bankBalances.DebitPartialAsync(balanceId, request.Amount);
        if (debitResult.IsFailure)
            return Result<Transfer>.Failure(debitResult.Errors);

        var debitedBankBalance = debitResult.Value!.DebitedBalance;

        var originResult = TransferOriginBankAccount.Create(sourceAccount.Id, sourceAccount.OwnerId);
        if (originResult.IsFailure)
            return Result<Transfer>.Failure(originResult.Errors);

        OnrampingMethod? onrampingMethod = request.OnrampingMethod;
        Chain? producedChain = request.ProducedChain;
        decimal? producedAmount = request.ProducedAmount;
        CryptoAsset? producedAsset = request.ProducedAsset;

        if (hasCryptoDest)
        {
            if (onrampingMethod is null)
                return Result<Transfer>.Failure(Error.Create()
                    .WithCode(TransferErrorCodes.OnrampingMethodRequired)
                    .WithMessage("O método de onramping é obrigatório para movimentações banco→crypto.")
                    .Build());

            if (producedAmount is null or <= 0)
                return Result<Transfer>.Failure(Error.Create()
                    .WithCode(TransferErrorCodes.ProducedAmountRequired)
                    .WithMessage("O valor produzido é obrigatório para movimentações banco→crypto.")
                    .Build());

            if (producedAsset is null)
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

            if (!producedAsset.Value.IsSupportedOnChain(producedChain.Value))
                return Result<Transfer>.Failure(Error.Create()
                    .WithCode(TransferErrorCodes.AssetChainMismatch)
                    .WithMessage("O ativo produzido não é suportado na rede informada.")
                    .Build());
        }
        else if (onrampingMethod is not null || producedChain is not null || producedAmount is not null || producedAsset is not null)
        {
            return Result<Transfer>.Failure(Error.Create()
                .WithCode(TransferErrorCodes.OnrampingMethodInvalid)
                .WithMessage("Campos de onramping só se aplicam a movimentações banco→crypto.")
                .Build());
        }

        var proof = request.Proof ?? new TransferProofRequest();
        var proofResult = TransferProof.Create(
            proof.PixTransactionId,
            proof.PixAuthenticationCode,
            proof.CryptoTransactionId,
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
            destinationBankAccountEntity = await _bankAccounts.AsQueryable()
                .Where(a => a.Id == request.DestinationBankAccountId!.Trim())
                .FirstOrDefaultAsync();
            if (destinationBankAccountEntity is null)
                return Result<Transfer>.Failure(NotFoundBank(request.DestinationBankAccountId!).Errors);

            var destResult = TransferDestinationBankAccount.Create(
                destinationBankAccountEntity.Id,
                destinationBankAccountEntity.OwnerId);
            if (destResult.IsFailure)
                return Result<Transfer>.Failure(destResult.Errors);
            destinationBankAccount = destResult.Value;
        }
        else
        {
            destinationType = TransferDestinationType.CryptoWallet;
            destinationCryptoWalletEntity = await _cryptoWallets.AsQueryable()
                .Where(w => w.Id == request.DestinationCryptoWalletId!.Trim())
                .FirstOrDefaultAsync();
            if (destinationCryptoWalletEntity is null)
                return Result<Transfer>.Failure(NotFoundCrypto(request.DestinationCryptoWalletId!).Errors);

            if (producedChain is not null
                && !destinationCryptoWalletEntity.HasAddressForNamespace(producedChain.Value.GetNamespace()))
            {
                return Result<Transfer>.Failure(Error.Create()
                    .WithCode(TransferErrorCodes.ProducedChainNamespaceMismatch)
                    .WithMessage("A wallet de destino não possui endereço para o namespace da rede informada.")
                    .Build());
            }

            var destResult = TransferDestinationCryptoWallet.Create(
                destinationCryptoWalletEntity.Id,
                destinationCryptoWalletEntity.OwnerId);
            if (destResult.IsFailure)
                return Result<Transfer>.Failure(destResult.Errors);
            destinationCryptoWallet = destResult.Value;
        }

        var transferResult = Transfer.Create(
            TransferType.Movement,
            onrampingMethod,
            proofResult.Value,
            TransferOriginType.BankAccount,
            originResult.Value,
            originCryptoWallet: null,
            destinationType,
            destinationBankAccount,
            destinationCryptoWallet,
            request.Amount,
            producedAmount,
            producedAsset,
            Array.Empty<string>(),
            strawManId,
            sourceBalanceId: balanceId,
            producedChain: producedChain);

        if (transferResult.IsFailure)
            return transferResult;

        var persistedTransfer = await _transfers.CreateAsync(transferResult.Value!);
        var originalSplits = BalanceSplitMapping.FromBankSplits(debitedBankBalance.Splits);

        var destinationStrawManId = hasBankDest
            ? destinationBankAccountEntity!.OwnerId
            : destinationCryptoWalletEntity!.OwnerId;

        var creditBaseAmount = hasCryptoDest ? producedAmount!.Value : request.Amount;

        var splitResult = await _splitCalculation.CalculateForCreditAsync(
            destinationStrawManId,
            creditBaseAmount,
            originalSplits,
            cancellationToken);

        if (splitResult.IsFailure)
            return Result<Transfer>.Failure(splitResult.Errors);

        if (hasBankDest)
        {
            var balanceResult = BankBalance.Create(
                destinationBankAccountEntity!.Id,
                creditBaseAmount,
                persistedTransfer.Id,
                BalanceSplitMapping.ToBankSplits(splitResult.Value!.Splits),
                debitedBankBalance.Origin);

            if (balanceResult.IsFailure)
                return Result<Transfer>.Failure(balanceResult.Errors);

            var creditResult = await _bankBalances.CreditAsync(
                destinationBankAccountEntity.Id,
                balanceResult.Value!);
            if (creditResult.IsFailure)
                return Result<Transfer>.Failure(creditResult.Errors);
        }
        else
        {
            var balanceResult = CryptoBalance.Create(
                destinationCryptoWalletEntity!.Id,
                producedChain!.Value,
                producedAsset!.Value,
                producedAmount!.Value,
                persistedTransfer.Id,
                BalanceSplitMapping.ToCryptoSplits(splitResult.Value!.Splits),
                CryptoBalanceOrigin.Create(
                    debitedBankBalance.Origin.OperationId,
                    debitedBankBalance.Origin.OperatorId).Value!);

            if (balanceResult.IsFailure)
                return Result<Transfer>.Failure(balanceResult.Errors);

            var creditResult = await _cryptoBalances.CreditAsync(
                destinationCryptoWalletEntity.Id,
                balanceResult.Value!);
            if (creditResult.IsFailure)
                return Result<Transfer>.Failure(creditResult.Errors);
        }

        if (debitResult.Value!.RemainderBalance is not null)
        {
            var deleteDebited = await _bankBalances.DeleteAsync(debitedBankBalance.Id);
            if (deleteDebited.IsFailure)
                return Result<Transfer>.Failure(deleteDebited.Errors);
        }

        return Result<Transfer>.Success(persistedTransfer);
    }

    public async Task<IResult<Transfer>> ExecuteCryptoWalletMovementAsync(
        CryptoWalletMovementRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var balanceId = request.SourceBalanceId?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(balanceId))
            return Result<Transfer>.Failure(Error.Create()
                .WithCode(TransferErrorCodes.BalanceIdRequired)
                .WithMessage("O ID do saldo de origem é obrigatório.")
                .Build());

        if (request.Amount <= 0)
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

        var sourceBalanceResult = await _cryptoBalances.GetByIdAsync(balanceId);
        if (sourceBalanceResult.IsFailure)
            return Result<Transfer>.Failure(sourceBalanceResult.Errors);

        var sourceBalance = sourceBalanceResult.Value!;

        var sourceWallet = await _cryptoWallets.AsQueryable()
            .Where(w => w.Id == sourceBalance.CryptoWalletId)
            .FirstOrDefaultAsync();
        if (sourceWallet is null)
            return Result<Transfer>.Failure(NotFoundCrypto(sourceBalance.CryptoWalletId).Errors);

        var strawManId = sourceWallet.OwnerId;

        var strawManValidation = await ValidateStrawManAsync(strawManId);
        if (strawManValidation is not null)
            return Result<Transfer>.Failure(strawManValidation.Errors);

        var debitResult = await _cryptoBalances.DebitPartialAsync(balanceId, request.Amount);
        if (debitResult.IsFailure)
            return Result<Transfer>.Failure(debitResult.Errors);

        var debitedCryptoBalance = debitResult.Value!.DebitedBalance;

        var originResult = TransferOriginCryptoWallet.Create(sourceWallet.Id, sourceWallet.OwnerId);
        if (originResult.IsFailure)
            return Result<Transfer>.Failure(originResult.Errors);

        Chain? producedChain = null;
        decimal? producedAmount = request.ProducedAmount;

        if (hasBankDest)
        {
            if (producedAmount is null or <= 0)
                return Result<Transfer>.Failure(Error.Create()
                    .WithCode(TransferErrorCodes.ProducedAmountRequired)
                    .WithMessage("O valor produzido em BRL é obrigatório para movimentações crypto→banco.")
                    .Build());
        }
        else if (producedAmount is not null)
        {
            return Result<Transfer>.Failure(Error.Create()
                .WithCode(TransferErrorCodes.OnrampingMethodInvalid)
                .WithMessage("O valor produzido em BRL só se aplica a movimentações crypto→banco.")
                .Build());
        }

        var proof = request.Proof ?? new TransferProofRequest();
        var proofResult = TransferProof.Create(
            proof.PixTransactionId,
            proof.PixAuthenticationCode,
            proof.CryptoTransactionId,
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
            destinationBankAccountEntity = await _bankAccounts.AsQueryable()
                .Where(a => a.Id == request.DestinationBankAccountId!.Trim())
                .FirstOrDefaultAsync();
            if (destinationBankAccountEntity is null)
                return Result<Transfer>.Failure(NotFoundBank(request.DestinationBankAccountId!).Errors);

            var destResult = TransferDestinationBankAccount.Create(
                destinationBankAccountEntity.Id,
                destinationBankAccountEntity.OwnerId);
            if (destResult.IsFailure)
                return Result<Transfer>.Failure(destResult.Errors);
            destinationBankAccount = destResult.Value;
        }
        else
        {
            destinationType = TransferDestinationType.CryptoWallet;
            producedChain = debitedCryptoBalance.Chain;

            destinationCryptoWalletEntity = await _cryptoWallets.AsQueryable()
                .Where(w => w.Id == request.DestinationCryptoWalletId!.Trim())
                .FirstOrDefaultAsync();
            if (destinationCryptoWalletEntity is null)
                return Result<Transfer>.Failure(NotFoundCrypto(request.DestinationCryptoWalletId!).Errors);

            var destResult = TransferDestinationCryptoWallet.Create(
                destinationCryptoWalletEntity.Id,
                destinationCryptoWalletEntity.OwnerId);
            if (destResult.IsFailure)
                return Result<Transfer>.Failure(destResult.Errors);
            destinationCryptoWallet = destResult.Value;
        }

        var transferResult = Transfer.Create(
            TransferType.Movement,
            onrampingMethod: null,
            proofResult.Value,
            TransferOriginType.CryptoWallet,
            originBankAccount: null,
            originResult.Value,
            destinationType,
            destinationBankAccount,
            destinationCryptoWallet,
            request.Amount,
            producedAmount,
            producedAsset: hasBankDest ? null : debitedCryptoBalance.Asset,
            Array.Empty<string>(),
            strawManId,
            sourceBalanceId: balanceId,
            producedChain: producedChain);

        if (transferResult.IsFailure)
            return transferResult;

        var persistedTransfer = await _transfers.CreateAsync(transferResult.Value!);
        var originalSplits = BalanceSplitMapping.FromCryptoSplits(debitedCryptoBalance.Splits);

        var destinationStrawManId = hasBankDest
            ? destinationBankAccountEntity!.OwnerId
            : destinationCryptoWalletEntity!.OwnerId;

        var creditBaseAmount = hasBankDest ? producedAmount!.Value : request.Amount;

        var splitResult = await _splitCalculation.CalculateForCreditAsync(
            destinationStrawManId,
            creditBaseAmount,
            originalSplits,
            cancellationToken);

        if (splitResult.IsFailure)
            return Result<Transfer>.Failure(splitResult.Errors);

        if (hasBankDest)
        {
            var bankOriginResult = BankBalanceOrigin.Create(
                debitedCryptoBalance.Origin.OperationId,
                debitedCryptoBalance.Origin.OperatorId);
            if (bankOriginResult.IsFailure)
                return Result<Transfer>.Failure(bankOriginResult.Errors);

            var balanceResult = BankBalance.Create(
                destinationBankAccountEntity!.Id,
                creditBaseAmount,
                persistedTransfer.Id,
                BalanceSplitMapping.ToBankSplits(splitResult.Value!.Splits),
                bankOriginResult.Value!);

            if (balanceResult.IsFailure)
                return Result<Transfer>.Failure(balanceResult.Errors);

            var creditResult = await _bankBalances.CreditAsync(
                destinationBankAccountEntity.Id,
                balanceResult.Value!);
            if (creditResult.IsFailure)
                return Result<Transfer>.Failure(creditResult.Errors);
        }
        else
        {
            var balanceResult = CryptoBalance.Create(
                destinationCryptoWalletEntity!.Id,
                debitedCryptoBalance.Chain,
                debitedCryptoBalance.Asset,
                request.Amount,
                persistedTransfer.Id,
                BalanceSplitMapping.ToCryptoSplits(splitResult.Value!.Splits),
                debitedCryptoBalance.Origin);

            if (balanceResult.IsFailure)
                return Result<Transfer>.Failure(balanceResult.Errors);

            var creditResult = await _cryptoBalances.CreditAsync(
                destinationCryptoWalletEntity.Id,
                balanceResult.Value!);
            if (creditResult.IsFailure)
                return Result<Transfer>.Failure(creditResult.Errors);
        }

        if (debitResult.Value!.RemainderBalance is not null)
        {
            var deleteDebited = await _cryptoBalances.DeleteAsync(debitedCryptoBalance.Id);
            if (deleteDebited.IsFailure)
                return Result<Transfer>.Failure(deleteDebited.Errors);
        }

        return Result<Transfer>.Success(persistedTransfer);
    }

    public async Task<IResult<Transfer>> ExecuteWithdrawalAsync(
        WithdrawalTransferRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var builder = Result.Create<Transfer>();

        var paymentIds = (request.PaymentIds ?? Array.Empty<string>())
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Select(id => id.Trim())
            .Distinct(StringComparer.Ordinal)
            .ToList();

        if (paymentIds.Count == 0)
            builder.WithError(Error.Create()
                .WithCode(TransferErrorCodes.PaymentIdsRequired)
                .WithMessage("É necessário vincular ao menos um pagamento à transferência de saque.")
                .Build());

        TransferDestinationType? destinationType = null;
        TransferDestinationBankAccount? destinationBankAccount = null;
        TransferDestinationCryptoWallet? destinationCryptoWallet = null;
        BankAccount? bankAccount = null;
        CryptoWallet? cryptoWallet = null;

        if (!string.IsNullOrWhiteSpace(request.DestinationBankAccountId))
        {
            destinationType = TransferDestinationType.BankAccount;

            bankAccount = await _bankAccounts.AsQueryable()
                .Where(a => a.Id == request.DestinationBankAccountId.Trim())
                .FirstOrDefaultAsync();

            if (bankAccount is null)
            {
                builder.WithError(Error.Create()
                    .WithCode(TransferErrorCodes.BankAccountNotFound)
                    .WithMessage($"A conta bancária '{request.DestinationBankAccountId}' não foi encontrada.")
                    .Build());
            }
            else
            {
                var destResult = TransferDestinationBankAccount.Create(bankAccount.Id, bankAccount.OwnerId);
                if (destResult.IsFailure)
                    return Result<Transfer>.Failure(destResult.Errors);
                destinationBankAccount = destResult.Value;
            }
        }
        else if (!string.IsNullOrWhiteSpace(request.DestinationCryptoWalletId))
        {
            destinationType = TransferDestinationType.CryptoWallet;

            cryptoWallet = await _cryptoWallets.AsQueryable()
                .Where(w => w.Id == request.DestinationCryptoWalletId.Trim())
                .FirstOrDefaultAsync();

            if (cryptoWallet is null)
            {
                builder.WithError(Error.Create()
                    .WithCode(TransferErrorCodes.CryptoWalletNotFound)
                    .WithMessage($"A wallet crypto '{request.DestinationCryptoWalletId}' não foi encontrada.")
                    .Build());
            }
            else
            {
                var destResult = TransferDestinationCryptoWallet.Create(cryptoWallet.Id, cryptoWallet.OwnerId);
                if (destResult.IsFailure)
                    return Result<Transfer>.Failure(destResult.Errors);
                destinationCryptoWallet = destResult.Value;
            }
        }
        else
        {
            builder.WithError(Error.Create()
                .WithCode(TransferErrorCodes.DestinationRequired)
                .WithMessage("Informe uma conta bancária ou wallet crypto de destino.")
                .Build());
        }

        var proof = request.Proof ?? new TransferProofRequest();
        var proofResult = TransferProof.Create(
            proof.PixTransactionId,
            proof.PixAuthenticationCode,
            proof.CryptoTransactionId,
            required: false);

        if (proofResult.IsFailure)
            return Result<Transfer>.Failure(proofResult.Errors);

        if (cryptoWallet is not null)
        {
            if (request.OnrampingMethod is null)
                builder.WithError(Error.Create()
                    .WithCode(TransferErrorCodes.OnrampingMethodRequired)
                    .WithMessage("O método de onramping é obrigatório para saque em wallet crypto.")
                    .Build());

            if (request.ProducedAmount is null or <= 0)
                builder.WithError(Error.Create()
                    .WithCode(TransferErrorCodes.ProducedAmountRequired)
                    .WithMessage("A quantidade produzida em crypto é obrigatória para saque em wallet.")
                    .Build());

            if (request.ProducedAsset is null)
                builder.WithError(Error.Create()
                    .WithCode(TransferErrorCodes.ProducedAssetRequired)
                    .WithMessage("O ativo produzido é obrigatório para saque em wallet crypto.")
                    .Build());

            if (request.ProducedChain is null)
                builder.WithError(Error.Create()
                    .WithCode(TransferErrorCodes.ProducedChainRequired)
                    .WithMessage("A rede blockchain produzida é obrigatória para saque em wallet crypto.")
                    .Build());
            else if (!Enum.IsDefined(request.ProducedChain.Value))
                builder.WithError(Error.Create()
                    .WithCode(TransferErrorCodes.ProducedChainInvalid)
                    .WithMessage("A rede blockchain produzida é inválida.")
                    .Build());
            else if (request.ProducedAsset is not null
                && !request.ProducedAsset.Value.IsSupportedOnChain(request.ProducedChain.Value))
                builder.WithError(Error.Create()
                    .WithCode(TransferErrorCodes.AssetChainMismatch)
                    .WithMessage("O ativo produzido não é suportado na rede informada.")
                    .Build());
            else if (!cryptoWallet!.HasAddressForNamespace(request.ProducedChain.Value.GetNamespace()))
                builder.WithError(Error.Create()
                    .WithCode(TransferErrorCodes.ProducedChainNamespaceMismatch)
                    .WithMessage("A wallet de destino não possui endereço para o namespace da rede informada.")
                    .Build());
        }
        else if (request.OnrampingMethod is not null
            || request.ProducedAmount is not null
            || request.ProducedAsset is not null
            || request.ProducedChain is not null)
        {
            builder.WithError(Error.Create()
                .WithCode(TransferErrorCodes.OnrampingMethodInvalid)
                .WithMessage("Onramping e valores produzidos só se aplicam a saque em wallet crypto.")
                .Build());
        }

        if (builder.ContainsError)
            return builder.Build();

        var paymentsById = (await _payments.AsQueryable()
            .Where(p => paymentIds.Contains(p.Id))
            .ToArrayAsync())
            .ToDictionary(p => p.Id, StringComparer.Ordinal);

        var alreadyLinkedIds = (await _transfers.AsQueryable()
            .Where(t => t.Type == TransferType.Withdrawal)
            .SelectMany(t => t.PaymentIds)
            .Where(id => paymentIds.Contains(id))
            .ToArrayAsync())
            .ToHashSet(StringComparer.Ordinal);

        var loadedPayments = new List<Payment>(paymentIds.Count);

        foreach (var paymentId in paymentIds)
        {
            if (!paymentsById.TryGetValue(paymentId, out var payment))
            {
                builder.WithError(Error.Create()
                    .WithCode(TransferErrorCodes.PaymentNotFound)
                    .WithMessage($"O pagamento '{paymentId}' não foi encontrado.")
                    .Build());
                continue;
            }

            if (string.IsNullOrWhiteSpace(payment.StrawManId))
            {
                builder.WithError(Error.Create()
                    .WithCode(TransferErrorCodes.PaymentStrawManNotBound)
                    .WithMessage($"O pagamento '{paymentId}' não possui laranja vinculado.")
                    .Build());
            }

            if (payment.Status != PaymentStatus.Paid)
            {
                builder.WithError(Error.Create()
                    .WithCode(TransferErrorCodes.PaymentNotPaid)
                    .WithMessage($"O pagamento '{paymentId}' não está pago.")
                    .Build());
            }

            if (payment.SettlementStatus != PaymentSettlementStatus.Unsettled)
            {
                builder.WithError(Error.Create()
                    .WithCode(TransferErrorCodes.PaymentAlreadyWithdrawn)
                    .WithMessage($"O pagamento '{paymentId}' já foi liquidado.")
                    .Build());
            }

            if (alreadyLinkedIds.Contains(paymentId))
            {
                builder.WithError(Error.Create()
                    .WithCode(TransferErrorCodes.PaymentAlreadyLinked)
                    .WithMessage($"O pagamento '{paymentId}' já está vinculado a outra transferência.")
                    .Build());
            }

            loadedPayments.Add(payment);
        }

        if (builder.ContainsError)
            return builder.Build();

        var strawManIds = loadedPayments
            .Select(p => p.StrawManId?.Trim())
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Distinct(StringComparer.Ordinal)
            .ToList();

        if (strawManIds.Count != 1)
        {
            return Result<Transfer>.Failure(Error.Create()
                .WithCode(TransferErrorCodes.PaymentStrawManMismatch)
                .WithMessage("Todos os pagamentos devem pertencer ao mesmo laranja.")
                .Build());
        }

        var strawManId = strawManIds[0]!;

        var strawManValidation = await ValidateStrawManAsync(strawManId);
        if (strawManValidation is not null)
            return Result<Transfer>.Failure(strawManValidation.Errors);

        if (bankAccount is not null && !string.Equals(bankAccount.OwnerId, strawManId, StringComparison.Ordinal))
        {
            return Result<Transfer>.Failure(Error.Create()
                .WithCode(TransferErrorCodes.BankAccountMismatch)
                .WithMessage("A conta bancária não pertence ao laranja dos pagamentos.")
                .Build());
        }

        if (cryptoWallet is not null && !string.Equals(cryptoWallet.OwnerId, strawManId, StringComparison.Ordinal))
        {
            return Result<Transfer>.Failure(Error.Create()
                .WithCode(TransferErrorCodes.CryptoWalletMismatch)
                .WithMessage("A wallet crypto não pertence ao laranja dos pagamentos.")
                .Build());
        }

        var groups = loadedPayments
            .GroupBy(p => BuildSplitSignature(p.Splits))
            .ToList();

        var totalAmount = loadedPayments.Sum(p => p.Amount);

        var transferResult = Transfer.Create(
            TransferType.Withdrawal,
            onrampingMethod: cryptoWallet is not null ? request.OnrampingMethod : null,
            proofResult.Value,
            originType: null,
            originBankAccount: null,
            originCryptoWallet: null,
            destinationType,
            destinationBankAccount,
            destinationCryptoWallet,
            sourceAmount: totalAmount,
            producedAmount: cryptoWallet is not null ? request.ProducedAmount : null,
            producedAsset: cryptoWallet is not null ? request.ProducedAsset : null,
            paymentIds,
            strawManId,
            producedChain: cryptoWallet is not null ? request.ProducedChain : null);

        if (transferResult.IsFailure)
            return transferResult;

        var transfer = transferResult.Value!;
        var persistedTransfer = await _transfers.CreateAsync(transfer);

        foreach (var group in groups)
        {
            var groupPayments = group.ToList();
            var groupAmount = groupPayments.Sum(p => p.Amount);
            var referencePayment = groupPayments[0];

            var originalSplits = new List<TransferBalanceSplit>();
            foreach (var s in referencePayment.Splits)
            {
                var splitResult = TransferBalanceSplit.Create(
                    s.AccountId, s.Percentage, s.Amount, TransferSplitKind.ProfitShare);
                if (splitResult.IsFailure)
                    return Result<Transfer>.Failure(splitResult.Errors);
                originalSplits.Add(splitResult.Value!);
            }

            var calculatedSplits = await _splitCalculation.CalculateForCreditAsync(
                strawManId,
                groupAmount,
                originalSplits,
                cancellationToken);

            if (calculatedSplits.IsFailure)
                return Result<Transfer>.Failure(calculatedSplits.Errors);

            if (bankAccount is not null)
            {
                var originResult = BankBalanceOrigin.Create(
                    referencePayment.OperationId,
                    referencePayment.OperatorId);

                if (originResult.IsFailure)
                    return Result<Transfer>.Failure(originResult.Errors);

                var balanceResult = BankBalance.Create(
                    bankAccount.Id,
                    groupAmount,
                    persistedTransfer.Id,
                    BalanceSplitMapping.ToBankSplits(calculatedSplits.Value!.Splits),
                    originResult.Value!);

                if (balanceResult.IsFailure)
                    return Result<Transfer>.Failure(balanceResult.Errors);

                var creditResult = await _bankBalances.CreditAsync(bankAccount.Id, balanceResult.Value!);
                if (creditResult.IsFailure)
                    return Result<Transfer>.Failure(creditResult.Errors);
            }
            else if (cryptoWallet is not null)
            {
                var originResult = CryptoBalanceOrigin.Create(
                    referencePayment.OperationId,
                    referencePayment.OperatorId);

                if (originResult.IsFailure)
                    return Result<Transfer>.Failure(originResult.Errors);

                var cryptoAmount = totalAmount == 0
                    ? request.ProducedAmount!.Value
                    : Math.Round(
                        request.ProducedAmount!.Value * groupAmount / totalAmount,
                        8,
                        MidpointRounding.AwayFromZero);

                var balanceResult = CryptoBalance.Create(
                    cryptoWallet.Id,
                    request.ProducedChain!.Value,
                    request.ProducedAsset!.Value,
                    cryptoAmount,
                    persistedTransfer.Id,
                    BalanceSplitMapping.ToCryptoSplits(calculatedSplits.Value!.Splits),
                    originResult.Value!);

                if (balanceResult.IsFailure)
                    return Result<Transfer>.Failure(balanceResult.Errors);

                var creditResult = await _cryptoBalances.CreditAsync(cryptoWallet.Id, balanceResult.Value!);
                if (creditResult.IsFailure)
                    return Result<Transfer>.Failure(creditResult.Errors);
            }
        }

        foreach (var payment in loadedPayments)
        {
            var markResult = payment.MarkAsWithdrawn();
            if (markResult.IsFailure)
                return Result<Transfer>.Failure(markResult.Errors);

            await _payments.UpdateAsync(payment);
        }

        return Result<Transfer>.Success(persistedTransfer);
    }

    public async Task<IResult<Transfer>> ExecutePayoutAsync(
        PayoutTransferRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var balanceId = request.SourceBalanceId?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(balanceId))
            return Result<Transfer>.Failure(Error.Create()
                .WithCode(TransferErrorCodes.BalanceIdRequired)
                .WithMessage("O ID do saldo de origem é obrigatório.")
                .Build());

        if (request.Amount <= 0)
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

        var proof = request.Proof ?? new TransferProofRequest();
        var proofResult = TransferProof.Create(
            proof.PixTransactionId,
            proof.PixAuthenticationCode,
            proof.CryptoTransactionId,
            required: true);

        if (proofResult.IsFailure)
            return Result<Transfer>.Failure(proofResult.Errors);

        var sourceBalanceResult = await _bankBalances.GetByIdAsync(balanceId);
        if (sourceBalanceResult.IsFailure)
            return Result<Transfer>.Failure(sourceBalanceResult.Errors);

        var sourceBalance = sourceBalanceResult.Value!;

        var sourceAccount = await _bankAccounts.AsQueryable()
            .Where(a => a.Id == sourceBalance.BankAccountId)
            .FirstOrDefaultAsync();

        if (sourceAccount is null)
            return Result<Transfer>.Failure(Error.Create()
                .WithCode(TransferErrorCodes.BankAccountNotFound)
                .WithMessage($"A conta bancária '{sourceBalance.BankAccountId}' não foi encontrada.")
                .Build());

        var strawManId = sourceAccount.OwnerId;

        var strawManValidation = await ValidateStrawManAsync(strawManId);
        if (strawManValidation is not null)
            return Result<Transfer>.Failure(strawManValidation.Errors);

        var debitResult = await _bankBalances.DebitPartialAsync(balanceId, request.Amount);
        if (debitResult.IsFailure)
            return Result<Transfer>.Failure(debitResult.Errors);

        if (debitResult.Value!.RemainderBalance is not null)
        {
            var deleteDebited = await _bankBalances.DeleteAsync(debitResult.Value.DebitedBalance.Id);
            if (deleteDebited.IsFailure)
                return Result<Transfer>.Failure(deleteDebited.Errors);
        }

        var originResult = TransferOriginBankAccount.Create(sourceAccount.Id, sourceAccount.OwnerId);
        if (originResult.IsFailure)
            return Result<Transfer>.Failure(originResult.Errors);

        TransferDestinationType destinationType;
        TransferDestinationBankAccount? destinationBankAccount = null;
        TransferDestinationCryptoWallet? destinationCryptoWallet = null;

        if (hasBankDest)
        {
            destinationType = TransferDestinationType.BankAccount;

            var destinationAccount = await _bankAccounts.AsQueryable()
                .Where(a => a.Id == request.DestinationBankAccountId!.Trim())
                .FirstOrDefaultAsync();

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

            var destinationWallet = await _cryptoWallets.AsQueryable()
                .Where(w => w.Id == request.DestinationCryptoWalletId!.Trim())
                .FirstOrDefaultAsync();

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
            request.Amount,
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

    public async Task<IResult<Transfer>> GetByIdAsync(string transferId)
    {
        if (string.IsNullOrWhiteSpace(transferId))
        {
            return Result<Transfer>.Failure(Error.Create()
                .WithCode(TransferErrorCodes.TransferIdInvalid)
                .WithMessage("O ID da transferência é obrigatório.")
                .Build());
        }

        var transfer = await _transfers.AsQueryable()
            .Where(t => t.Id == transferId.Trim())
            .FirstOrDefaultAsync();

        if (transfer is null)
        {
            return Result<Transfer>.Failure(Error.Create()
                .WithCode(TransferErrorCodes.TransferNotFound)
                .WithMessage($"A transferência '{transferId}' não foi encontrada.")
                .Build());
        }

        return Result<Transfer>.Success(transfer);
    }

    public async Task<IResult<SearchTransfersResponse>> SearchAsync(
        SearchTransfersRequest? request,
        CancellationToken cancellationToken = default)
    {
        request ??= new SearchTransfersRequest();
        var query = _transfers.AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.StrawManId))
            query = query.Where(t => t.StrawManId == request.StrawManId.Trim());

        if (request.Type.HasValue)
            query = query.Where(t => t.Type == request.Type.Value);

        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(t => t.CreatedAt)
            .Skip(Math.Max(0, request.Offset))
            .Take(NormalizeLimit(request.Limit))
            .ToArrayAsync();

        return Result<SearchTransfersResponse>.Success(new SearchTransfersResponse
        {
            Total = (int)total,
            Items = items,
        });
    }

    public Task<IResult<TransferTimelineDetails>> GetTimelineAsync(
        string transferId,
        CancellationToken cancellationToken = default) =>
        _timeline.GetTimelineAsync(transferId);

    private static int NormalizeLimit(int limit) => limit <= 0 ? 30 : Math.Min(limit, 999);

    private async Task<IResult?> ValidateStrawManAsync(string strawManId)
    {
        if (string.IsNullOrWhiteSpace(strawManId))
            return Result.Failure(Error.Create()
                .WithCode(TransferErrorCodes.StrawManInvalid)
                .WithMessage("O ID do laranja é obrigatório.")
                .Build());

        var account = await _accounts.AsQueryable()
            .Where(a => a.Id == strawManId.Trim())
            .FirstOrDefaultAsync();

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

    private static string BuildSplitSignature(IReadOnlyList<PaymentSplit> splits) =>
        string.Join("|", splits
            .OrderBy(s => s.AccountId, StringComparer.Ordinal)
            .Select(s => $"{s.AccountId}:{s.Percentage:F4}"));

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
            .WithMessage("A conta bancária não pertence ao dono esperado.")
            .Build());

    private static IResult MismatchCrypto() =>
        Result.Failure(Error.Create()
            .WithCode(TransferErrorCodes.CryptoWalletMismatch)
            .WithMessage("A wallet crypto não pertence ao dono esperado.")
            .Build());
}

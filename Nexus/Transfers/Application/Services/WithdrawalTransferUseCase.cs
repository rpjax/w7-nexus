using Aidan.Core.Errors;
using Aidan.Core.Patterns;
using Nexus.AccountNodes.Aggregates;
using Nexus.AccountNodes.Application.Contracts;
using Nexus.AccountNodes.Errors;
using Nexus.Accounts.Application.Contracts;
using Nexus.Authorization;
using Nexus.Payments.Aggregates;
using Nexus.Payments.Application.Contracts;
using Nexus.Transfers.Aggregates;
using Nexus.Transfers.Application.Contracts;
using Nexus.Transfers.Errors;

namespace Nexus.Transfers.Application.Services;

public sealed class WithdrawalTransferUseCase : IWithdrawalTransferUseCase
{
    private readonly IAccountRepository _accounts;
    private readonly IPaymentRepository _payments;
    private readonly IBankAccountRepository _bankAccounts;
    private readonly ICryptoWalletRepository _cryptoWallets;
    private readonly ITransferRepository _transfers;
    private readonly IBalanceSplitCalculationService _splitCalculation;

    public WithdrawalTransferUseCase(
        IAccountRepository accounts,
        IPaymentRepository payments,
        IBankAccountRepository bankAccounts,
        ICryptoWalletRepository cryptoWallets,
        ITransferRepository transfers,
        IBalanceSplitCalculationService splitCalculation)
    {
        _accounts = accounts;
        _payments = payments;
        _bankAccounts = bankAccounts;
        _cryptoWallets = cryptoWallets;
        _transfers = transfers;
        _splitCalculation = splitCalculation;
    }

    public async Task<IResult<Transfer>> ExecuteAsync(
        WithdrawalTransferRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var builder = Result.Create<Transfer>();
        var strawManId = request.StrawManId?.Trim() ?? string.Empty;

        var strawManValidation = ValidateStrawMan(strawManId);
        if (strawManValidation is not null)
            return Result<Transfer>.Failure(strawManValidation.Errors);

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

        AccountNodeSnapshot? destination = null;
        BankAccount? bankAccount = null;
        CryptoWallet? cryptoWallet = null;

        if (!string.IsNullOrWhiteSpace(request.BankAccountId))
        {
            bankAccount = _bankAccounts.AsQueryable()
                .FirstOrDefault(a => a.Id == request.BankAccountId.Trim());

            if (bankAccount is null)
            {
                builder.WithError(Error.Create()
                    .WithCode(TransferErrorCodes.BankAccountNotFound)
                    .WithMessage($"A conta bancária '{request.BankAccountId}' não foi encontrada.")
                    .Build());
            }
            else if (!string.Equals(bankAccount.StrawManId, strawManId, StringComparison.Ordinal))
            {
                builder.WithError(Error.Create()
                    .WithCode(TransferErrorCodes.BankAccountMismatch)
                    .WithMessage("A conta bancária não pertence ao laranja informado.")
                    .Build());
            }
            else
            {
                var destResult = AccountNodeSnapshot.ForBankAccount(bankAccount.Id, strawManId);
                if (destResult.IsFailure)
                    return Result<Transfer>.Failure(destResult.Errors);
                destination = destResult.Value;
            }
        }
        else if (!string.IsNullOrWhiteSpace(request.CryptoWalletId))
        {
            cryptoWallet = _cryptoWallets.AsQueryable()
                .FirstOrDefault(w => w.Id == request.CryptoWalletId.Trim());

            if (cryptoWallet is null)
            {
                builder.WithError(Error.Create()
                    .WithCode(TransferErrorCodes.CryptoWalletNotFound)
                    .WithMessage($"A wallet crypto '{request.CryptoWalletId}' não foi encontrada.")
                    .Build());
            }
            else if (!string.Equals(cryptoWallet.StrawManId, strawManId, StringComparison.Ordinal))
            {
                builder.WithError(Error.Create()
                    .WithCode(TransferErrorCodes.CryptoWalletMismatch)
                    .WithMessage("A wallet crypto não pertence ao laranja informado.")
                    .Build());
            }
            else
            {
                var destResult = AccountNodeSnapshot.ForCryptoWallet(cryptoWallet.Id, strawManId);
                if (destResult.IsFailure)
                    return Result<Transfer>.Failure(destResult.Errors);
                destination = destResult.Value;
            }
        }
        else
        {
            builder.WithError(Error.Create()
                .WithCode(TransferErrorCodes.DestinationRequired)
                .WithMessage("Informe uma conta bancária ou wallet crypto de destino.")
                .Build());
        }

        var proofResult = TransferProof.Create(
            request.PixTransactionId,
            request.PixAuthenticationCode,
            request.CryptoTransactionId,
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

        var loadedPayments = new List<Payment>(paymentIds.Count);

        foreach (var paymentId in paymentIds)
        {
            var payment = _payments.AsQueryable().FirstOrDefault(p => p.Id == paymentId);
            if (payment is null)
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
            else if (!string.Equals(payment.StrawManId, strawManId, StringComparison.Ordinal))
            {
                builder.WithError(Error.Create()
                    .WithCode(TransferErrorCodes.PaymentStrawManMismatch)
                    .WithMessage($"O pagamento '{paymentId}' não pertence ao laranja informado.")
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

            var alreadyLinked = _transfers.AsQueryable()
                .Any(t => t.Type == TransferType.Withdrawal && t.PaymentIds.Contains(paymentId));

            if (alreadyLinked)
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

        var groups = loadedPayments
            .GroupBy(p => BuildSplitSignature(p.Splits))
            .ToList();

        var totalAmount = loadedPayments.Sum(p => p.Amount);

        var transferResult = Transfer.Create(
            TransferType.Withdrawal,
            onrampingMethod: cryptoWallet is not null ? request.OnrampingMethod : null,
            proofResult.Value,
            source: null,
            destination,
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

            var originalSplits = new List<BalanceSplitSnapshot>();
            foreach (var s in referencePayment.Splits)
            {
                var splitResult = BalanceSplitSnapshot.Create(
                    s.AccountId, s.Percentage, s.Amount, SplitKind.ProfitShare);
                if (splitResult.IsFailure)
                    return Result<Transfer>.Failure(splitResult.Errors);
                originalSplits.Add(splitResult.Value!);
            }

            var originResult = BalanceOriginSnapshot.Create(
                referencePayment.OperationId,
                referencePayment.OperatorId,
                referencePayment.StrawManId);

            if (originResult.IsFailure)
                return Result<Transfer>.Failure(originResult.Errors);

            var calculatedSplits = await _splitCalculation.CalculateForCreditAsync(
                strawManId,
                groupAmount,
                originalSplits,
                Array.Empty<string>(),
                cancellationToken);

            if (calculatedSplits.IsFailure)
                return Result<Transfer>.Failure(calculatedSplits.Errors);

            if (bankAccount is not null)
            {
                var balanceResult = BankBalance.Create(
                    groupAmount,
                    persistedTransfer.Id,
                    calculatedSplits.Value!.SplitSnapshot,
                    calculatedSplits.Value.AppliedStrawManFeeIds,
                    originResult.Value!);

                if (balanceResult.IsFailure)
                    return Result<Transfer>.Failure(balanceResult.Errors);

                var creditResult = bankAccount.CreditBalance(balanceResult.Value!);
                if (creditResult.IsFailure)
                    return Result<Transfer>.Failure(creditResult.Errors);
            }
            else if (cryptoWallet is not null)
            {
                var cryptoAmount = totalAmount == 0
                    ? request.ProducedAmount!.Value
                    : Math.Round(
                        request.ProducedAmount!.Value * groupAmount / totalAmount,
                        8,
                        MidpointRounding.AwayFromZero);

                var balanceResult = CryptoBalance.Create(
                    request.ProducedChain!.Value,
                    request.ProducedAsset!.Value,
                    cryptoAmount,
                    persistedTransfer.Id,
                    calculatedSplits.Value!.SplitSnapshot,
                    calculatedSplits.Value.AppliedStrawManFeeIds,
                    originResult.Value!);

                if (balanceResult.IsFailure)
                    return Result<Transfer>.Failure(balanceResult.Errors);

                var creditResult = cryptoWallet.CreditBalance(balanceResult.Value!);
                if (creditResult.IsFailure)
                    return Result<Transfer>.Failure(creditResult.Errors);
            }
        }

        if (bankAccount is not null)
            await _bankAccounts.UpdateAsync(bankAccount);
        else if (cryptoWallet is not null)
            await _cryptoWallets.UpdateAsync(cryptoWallet);

        foreach (var payment in loadedPayments)
        {
            var markResult = payment.MarkAsWithdrawn();
            if (markResult.IsFailure)
                return Result<Transfer>.Failure(markResult.Errors);

            await _payments.UpdateAsync(payment);
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

    private static string BuildSplitSignature(IReadOnlyList<PaymentSplit> splits) =>
        string.Join("|", splits
            .OrderBy(s => s.AccountId, StringComparer.Ordinal)
            .Select(s => $"{s.AccountId}:{s.Percentage:F4}"));
}

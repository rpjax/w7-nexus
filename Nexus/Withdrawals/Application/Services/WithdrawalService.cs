using Aidan.Core.Errors;
using Aidan.Core.Patterns;
using Nexus.Accounts.Application.Contracts;
using Nexus.Operations.Application.Contracts;
using Nexus.Payments.Aggregates;
using Nexus.Payments.Application.Contracts;
using Nexus.Withdrawals.Aggregates;
using Nexus.Withdrawals.Application.Contracts;
using Nexus.Withdrawals.Errors;

namespace Nexus.Withdrawals.Application.Services;

public sealed class WithdrawalService : IWithdrawalService
{
    private readonly IAccountRepository _accounts;
    private readonly IOperationRepository _operations;
    private readonly IPaymentRepository _payments;
    private readonly IBankAccountRepository _bankAccounts;
    private readonly ICryptoWalletRepository _cryptoWallets;
    private readonly IWithdrawalRepository _withdrawals;

    public WithdrawalService(
        IAccountRepository accounts,
        IOperationRepository operations,
        IPaymentRepository payments,
        IBankAccountRepository bankAccounts,
        ICryptoWalletRepository cryptoWallets,
        IWithdrawalRepository withdrawals)
    {
        _accounts = accounts;
        _operations = operations;
        _payments = payments;
        _bankAccounts = bankAccounts;
        _cryptoWallets = cryptoWallets;
        _withdrawals = withdrawals;
    }

    public async Task<IResult<Withdrawal>> CreateWithdrawalAsync(CreateWithdrawalRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var builder = Result.Create<Withdrawal>();
        var operationId = request.OperationId?.Trim() ?? string.Empty;
        var strawManAccountId = request.StrawManAccountId?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(operationId))
            builder.WithError(Error.Create()
                .WithCode(WithdrawalErrorCodes.OperationIdInvalid)
                .WithMessage("O ID da operação é obrigatório.")
                .Build());

        var strawManValidation = StrawManValidation.ValidateStrawManAccount(
            _accounts,
            strawManAccountId,
            WithdrawalErrorCodes.StrawManInvalid,
            WithdrawalErrorCodes.StrawManNotFound,
            WithdrawalErrorCodes.StrawManRoleRequired);

        if (strawManValidation is not null)
            return Result<Withdrawal>.Failure(strawManValidation.Errors);

        var operation = _operations.AsQueryable()
            .FirstOrDefault(o => o.Id == operationId);

        if (!string.IsNullOrWhiteSpace(operationId) && operation is null)
            builder.WithError(Error.Create()
                .WithCode(WithdrawalErrorCodes.OperationNotFound)
                .WithMessage($"A operação '{operationId}' não foi encontrada.")
                .Build());

        if (operation is not null
            && !string.IsNullOrWhiteSpace(strawManAccountId)
            && !operation.StrawManIds.Contains(strawManAccountId, StringComparer.Ordinal))
        {
            builder.WithError(Error.Create()
                .WithCode(WithdrawalErrorCodes.StrawManNotOnOperation)
                .WithMessage($"O laranja '{strawManAccountId}' não está vinculado à operação '{operationId}'.")
                .Build());
        }

        var paymentIds = (request.PaymentIds ?? Array.Empty<string>())
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Select(id => id.Trim())
            .Distinct(StringComparer.Ordinal)
            .ToList();

        if (paymentIds.Count == 0)
            builder.WithError(Error.Create()
                .WithCode(WithdrawalErrorCodes.PaymentIdsRequired)
                .WithMessage("É necessário vincular ao menos um pagamento ao saque.")
                .Build());

        BankAccount? bankAccount = null;
        CryptoWallet? cryptoWallet = null;

        if (request.Type == WithdrawalType.Pix)
        {
            if (string.IsNullOrWhiteSpace(request.BankAccountId))
            {
                builder.WithError(Error.Create()
                    .WithCode(WithdrawalErrorCodes.BankAccountRequired)
                    .WithMessage("A conta bancária é obrigatória para saques PIX.")
                    .Build());
            }
            else
            {
                bankAccount = _bankAccounts.AsQueryable()
                    .FirstOrDefault(a => a.Id == request.BankAccountId.Trim());

                if (bankAccount is null)
                {
                    builder.WithError(Error.Create()
                        .WithCode(BankAccountErrorCodes.BankAccountNotFound)
                        .WithMessage($"A conta bancária '{request.BankAccountId}' não foi encontrada.")
                        .Build());
                }
                else if (!string.IsNullOrWhiteSpace(strawManAccountId)
                         && !string.Equals(bankAccount.StrawManAccountId, strawManAccountId, StringComparison.Ordinal))
                {
                    builder.WithError(Error.Create()
                        .WithCode(WithdrawalErrorCodes.BankAccountMismatch)
                        .WithMessage("A conta bancária não pertence ao laranja informado.")
                        .Build());
                }
            }
        }
        else if (request.Type == WithdrawalType.Crypto)
        {
            if (string.IsNullOrWhiteSpace(request.CryptoWalletId))
            {
                builder.WithError(Error.Create()
                    .WithCode(WithdrawalErrorCodes.CryptoWalletRequired)
                    .WithMessage("A wallet crypto é obrigatória para saques crypto.")
                    .Build());
            }
            else
            {
                cryptoWallet = _cryptoWallets.AsQueryable()
                    .FirstOrDefault(w => w.Id == request.CryptoWalletId.Trim());

                if (cryptoWallet is null)
                {
                    builder.WithError(Error.Create()
                        .WithCode(CryptoWalletErrorCodes.CryptoWalletNotFound)
                        .WithMessage($"A wallet crypto '{request.CryptoWalletId}' não foi encontrada.")
                        .Build());
                }
                else if (!string.IsNullOrWhiteSpace(strawManAccountId)
                         && !string.Equals(cryptoWallet.StrawManAccountId, strawManAccountId, StringComparison.Ordinal))
                {
                    builder.WithError(Error.Create()
                        .WithCode(WithdrawalErrorCodes.CryptoWalletMismatch)
                        .WithMessage("A wallet crypto não pertence ao laranja informado.")
                        .Build());
                }
            }
        }
        else
        {
            builder.WithError(Error.Create()
                .WithCode(WithdrawalErrorCodes.TypeInvalid)
                .WithMessage("O tipo de saque informado é inválido.")
                .Build());
        }

        var pixProofResult = PixProof.Create(request.PixTransactionId, request.PixAuthenticationCode);
        if (pixProofResult.IsFailure)
            return Result<Withdrawal>.Failure(pixProofResult.Errors);

        var cryptoProofResult = CryptoProof.Create(request.CryptoTransactionId);
        if (cryptoProofResult.IsFailure)
            return Result<Withdrawal>.Failure(cryptoProofResult.Errors);

        if (builder.ContainsError)
            return builder.Build();

        var loadedPayments = new List<Payment>(paymentIds.Count);
        decimal paymentsTotal = 0m;

        foreach (var paymentId in paymentIds)
        {
            var payment = _payments.AsQueryable().FirstOrDefault(p => p.Id == paymentId);
            if (payment is null)
            {
                builder.WithError(Error.Create()
                    .WithCode(WithdrawalErrorCodes.PaymentNotFound)
                    .WithMessage($"O pagamento '{paymentId}' não foi encontrado.")
                    .Build());
                continue;
            }

            if (!string.Equals(payment.OperationId, operationId, StringComparison.Ordinal))
            {
                builder.WithError(Error.Create()
                    .WithCode(WithdrawalErrorCodes.PaymentOperationMismatch)
                    .WithMessage($"O pagamento '{paymentId}' não pertence à operação informada.")
                    .Build());
            }

            if (payment.Status != PaymentStatus.Paid)
            {
                builder.WithError(Error.Create()
                    .WithCode(WithdrawalErrorCodes.PaymentNotPaid)
                    .WithMessage($"O pagamento '{paymentId}' não está pago.")
                    .Build());
            }

            if (payment.SettlementStatus == PaymentSettlementStatus.Withdrawn)
            {
                builder.WithError(Error.Create()
                    .WithCode(WithdrawalErrorCodes.PaymentAlreadyWithdrawn)
                    .WithMessage($"O pagamento '{paymentId}' já foi sacado.")
                    .Build());
            }

            var alreadyLinked = _withdrawals.AsQueryable()
                .Any(w => w.PaymentIds.Contains(paymentId));

            if (alreadyLinked)
            {
                builder.WithError(Error.Create()
                    .WithCode(WithdrawalErrorCodes.PaymentAlreadyLinked)
                    .WithMessage($"O pagamento '{paymentId}' já está vinculado a outro saque.")
                    .Build());
            }

            loadedPayments.Add(payment);
            paymentsTotal += payment.Amount;
        }

        if (builder.ContainsError)
            return builder.Build();

        var withdrawalResult = Withdrawal.Create(
            operationId,
            request.Type,
            strawManAccountId,
            bankAccount?.Id,
            cryptoWallet?.Id,
            paymentIds,
            request.CostDescription,
            request.CostAmount,
            pixProofResult.Value,
            cryptoProofResult.Value,
            paymentsTotal);

        if (withdrawalResult.IsFailure)
            return withdrawalResult;

        foreach (var payment in loadedPayments)
        {
            var markResult = payment.MarkAsWithdrawn();
            if (markResult.IsFailure)
            {
                return Result<Withdrawal>.Failure(markResult.Errors);
            }

            await _payments.UpdateAsync(payment);
        }

        var persisted = await _withdrawals.CreateAsync(withdrawalResult.Value!);
        return Result<Withdrawal>.Success(persisted);
    }

    public Task<IResult<Withdrawal>> GetByIdAsync(string withdrawalId)
    {
        if (string.IsNullOrWhiteSpace(withdrawalId))
        {
            return Task.FromResult<IResult<Withdrawal>>(Result<Withdrawal>.Failure(Error.Create()
                .WithCode(WithdrawalErrorCodes.WithdrawalIdInvalid)
                .WithMessage("O ID do saque é obrigatório.")
                .Build()));
        }

        var withdrawal = _withdrawals.AsQueryable()
            .FirstOrDefault(w => w.Id == withdrawalId.Trim());

        if (withdrawal is null)
        {
            return Task.FromResult<IResult<Withdrawal>>(Result<Withdrawal>.Failure(Error.Create()
                .WithCode(WithdrawalErrorCodes.WithdrawalNotFound)
                .WithMessage($"O saque '{withdrawalId}' não foi encontrado.")
                .Build()));
        }

        return Task.FromResult<IResult<Withdrawal>>(Result<Withdrawal>.Success(withdrawal));
    }
}

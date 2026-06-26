using Aidan.Core.Errors;
using Nexus.Payments.Application.Contracts;
using Nexus.Operations.Application.Contracts;
using Aidan.Core.Patterns;
using Nexus.Operations.Aggregates;
using Nexus.Payments.Aggregates;
using Nexus.Payments.Application.Models;
using Nexus.Accounts.Application.Contracts;
using Nexus.Authorization;
using Nexus.Payments.Errors;
using Nexus.Database.Models;

namespace Nexus.Payments.Application.Services;

public sealed class PaymentService : IPaymentService
{
    private IAccountRepository _accountRepository { get; }
    private IPaymentRepository _paymentRepository { get; }
    private IOperationRepository _operationRepository { get; }
    private ITeamRepository _teamRepository { get; }
    private IPaymentSplitCalculationService _splitCalculation { get; }

    public PaymentService(
        IAccountRepository accountRepository,
        IPaymentRepository pixPaymentRepository,
        IOperationRepository operationRepository,
        ITeamRepository teamRepository,
        IPaymentSplitCalculationService splitCalculation)
    {
        _accountRepository = accountRepository;
        _paymentRepository = pixPaymentRepository;
        _operationRepository = operationRepository;
        _teamRepository = teamRepository;
        _splitCalculation = splitCalculation;
    }

    public async Task<IResult<Payment>> CreatePaymentAsync(CreatePaymentRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var amount = request.Amount;
        var operationId = request.OperationId?.Trim();
        var gateway = request.Gateway;
        var gatewayPaymentId = request.GatewayPaymentId?.Trim();
        var operatorId = request.OperatorId?.Trim();
        var strawManId = request.StrawManId?.Trim();

        var builder = Result.Create<Payment>();

        if (string.IsNullOrWhiteSpace(operationId))
            builder.WithError(Error.Create()
                .WithCode(PixPaymentErrorCodes.OperationIdInvalid)
                .WithMessage("O ID da operação é obrigatório.")
                .Build());

        if (amount <= 0m)
            builder.WithError(Error.Create()
                .WithCode(PixPaymentErrorCodes.AmountInvalid)
                .WithMessage("O valor deve ser maior que zero.")
                .Build());

        if (string.IsNullOrWhiteSpace(gatewayPaymentId))
            builder.WithError(Error.Create()
                .WithCode(PixPaymentErrorCodes.GatewayPaymentIdInvalid)
                .WithMessage("O ID do pagamento no gateway é obrigatório.")
                .Build());

        if (gateway == PaymentGateway.None)
            builder.WithError(Error.Create()
                .WithCode(PixPaymentErrorCodes.GatewayInvalid)
                .WithMessage("O gateway informado é inválido.")
                .Build());

        if (operatorId is not null && string.IsNullOrWhiteSpace(operatorId))
            builder.WithError(Error.Create()
                .WithCode(PixPaymentErrorCodes.OperatorInvalid)
                .WithMessage("O ID do operador não pode estar vazio quando informado.")
                .Build());

        if (builder.ContainsError)
            return builder.Build();

        var explicitPaymentId = request.ExplicitPaymentId?.Trim();
        if (!string.IsNullOrWhiteSpace(explicitPaymentId))
        {
            var idTaken = await _paymentRepository.AsQueryable()
                .AnyAsync(p => p.Id == explicitPaymentId);
            if (idTaken)
                builder.WithError(Error.Create()
                    .WithCode(PixPaymentErrorCodes.PaymentIdAlreadyExists)
                    .WithMessage($"O ID de pagamento '{explicitPaymentId}' já está em uso.")
                    .Build());
        }

        if (builder.ContainsError)
            return builder.Build();

        var operation = _operationRepository.AsQueryable()
            .FirstOrDefault(o => o.Id == operationId);

        if (operation is null)
            builder.WithError(Error.Create()
                .WithCode(PixPaymentErrorCodes.OperationNotFound)
                .WithMessage($"A operação '{operationId}' não foi encontrada.")
                .Build());

        if (operatorId is not null)
        {
            var operatorExists = _accountRepository.AsQueryable()
                .Any(a => a.Id == operatorId);
            if (!operatorExists)
                builder.WithError(Error.Create()
                    .WithCode(PixPaymentErrorCodes.OperatorNotFound)
                    .WithMessage($"A conta do operador '{operatorId}' não foi encontrada.")
                    .Build());
        }

        if (!string.IsNullOrWhiteSpace(strawManId))
        {
            var strawManAccount = _accountRepository.AsQueryable()
                .FirstOrDefault(a => a.Id == strawManId);
            if (strawManAccount is null)
                builder.WithError(Error.Create()
                    .WithCode(PixPaymentErrorCodes.StrawManNotFound)
                    .WithMessage($"A conta laranja '{strawManId}' não foi encontrada.")
                    .Build());
            else if (!strawManAccount.Roles.Contains(Roles.StrawMan, StringComparer.Ordinal))
                builder.WithError(Error.Create()
                    .WithCode(PixPaymentErrorCodes.StrawManRoleRequired)
                    .WithMessage($"A conta '{strawManId}' não possui o perfil de laranja.")
                    .Build());
        }

        IReadOnlyList<PaymentSplit> splits = Array.Empty<PaymentSplit>();

        if (operatorId is not null)
        {
            var matchingTeams = _teamRepository.AsQueryable()
                .Where(t =>
                    t.OperationId == operationId &&
                    t.OperatorIds.Contains(operatorId))
                .ToList();

            if (matchingTeams.Count == 0)
            {
                builder.WithError(Error.Create()
                    .WithCode(PixPaymentErrorCodes.TeamNotFound)
                    .WithMessage($"Não há equipe na operação '{operationId}' com o operador informado.")
                    .Build());
            }
            else if (matchingTeams.Count > 1)
            {
                builder.WithError(Error.Create()
                    .WithCode(PixPaymentErrorCodes.TeamAmbiguous)
                    .WithMessage("Há mais de uma equipe compatível com o operador informado.")
                    .Build());
            }
            else
            {
                var team = matchingTeams[0];
                var rule = team.OperatorProfitShareRules
                    .FirstOrDefault(r => string.Equals(r.OperatorId, operatorId, StringComparison.Ordinal));

                if (rule is null || rule.Cuts.Count == 0)
                {
                    builder.WithError(Error.Create()
                        .WithCode(PixPaymentErrorCodes.ProfitShareRuleNotFound)
                        .WithMessage($"Não há regra de repasse configurada para o operador '{operatorId}'.")
                        .Build());
                }
                else
                {
                    var normalizedCuts = ProfitSharePercentageRules.NormalizeCuts(rule.Cuts);
                    splits = PaymentSplit.AllocateFromCuts(
                        amount,
                        normalizedCuts
                            .Select(cut => (cut.AccountId, cut.Percentage))
                            .ToList());
                }
            }
        }
        else if (operation is not null)
        {
            var recipientIds = operation.AdministratorIds.ToArray();
            if (recipientIds.Length == 0)
            {
                recipientIds = _accountRepository.AsQueryable()
                    .Where(a => a.Roles.Contains(Roles.Administrator))
                    .Select(a => a.Id)
                    .ToArray();
            }

            if (recipientIds.Length == 0)
            {
                builder.WithError(Error.Create()
                    .WithCode(PixPaymentErrorCodes.ProfitShareRecipientsNotFound)
                    .WithMessage("Não há administradores da operação nem administradores de sistema para definir o repasse.")
                    .Build());
            }
            else
            {
                splits = BuildEqualSplits(amount, recipientIds);
            }
        }

        if (builder.ContainsError)
            return builder.Build();

        if (!string.IsNullOrWhiteSpace(strawManId) && splits.Count > 0)
        {
            splits = await _splitCalculation.ApplyStrawManFeeAsync(
                amount,
                splits,
                strawManId);
        }

        var validatedGatewayPaymentId = gatewayPaymentId!;
        var id = string.IsNullOrWhiteSpace(explicitPaymentId) ? string.Empty : explicitPaymentId!;
        var createdAt = DateTime.UtcNow;
        var payment = new Payment(
            id,
            operationId!,
            gateway,
            validatedGatewayPaymentId,
            amount,
            splits,
            PaymentStatus.Pending,
            PaymentSettlementStatus.Unsettled,
            PaymentDistributionStatus.Pending,
            OperatorId: null,
            strawManId: strawManId ?? string.Empty,
            createdAt,
            PaidAt: null,
            RefundedAt: null,
            KilledAt: null,
            KillReason: null,
            WithdrawnAt: null,
            DistributedAt: null);

        if (operatorId is not null)
        {
            var bindOperatorResult = payment.BindToOperator(operatorId);
            if (bindOperatorResult.IsFailure)
                return Result.Create<Payment>().WithErrors(bindOperatorResult.Errors).Build();
        }

        payment = await _paymentRepository.CreateAsync(payment);
        return builder.WithValue(payment).Build();
    }

    public async Task<IResult<Payment>> GetByIdAsync(string paymentId)
    {
        if (string.IsNullOrWhiteSpace(paymentId))
            return Result<Payment>.Failure(Error.Create()
                .WithCode(PixPaymentErrorCodes.PaymentIdInvalid)
                .WithMessage("O ID do pagamento é obrigatório.")
                .Build());

        var payment = _paymentRepository.AsQueryable()
            .FirstOrDefault(p => p.Id == paymentId);

        if (payment is null)
            return Result<Payment>.Failure(Error.Create()
                .WithCode(PixPaymentErrorCodes.PaymentNotFound)
                .WithMessage($"O pagamento '{paymentId}' não foi encontrado.")
                .Build());

        return Result<Payment>.Success(payment);
    }

    public async Task<IResult<Payment>> BindOperatorAsync(string paymentId, string operatorId)
    {
        var paymentResult = await GetByIdAsync(paymentId);
        if (paymentResult.IsFailure)
            return paymentResult;

        var payment = paymentResult.Value!;
        var bindResult = payment.BindToOperator(operatorId);
        if (bindResult.IsFailure)
            return Result<Payment>.Failure(bindResult.Errors);

        await _paymentRepository.UpdateAsync(payment);
        return Result<Payment>.Success(payment);
    }

    public async Task<IResult<Payment>> BindStrawManAsync(string paymentId, string strawManId)
    {
        var paymentResult = await GetByIdAsync(paymentId);
        if (paymentResult.IsFailure)
            return paymentResult;

        var payment = paymentResult.Value!;
        var bindResult = payment.BindToStrawMan(strawManId);
        if (bindResult.IsFailure)
            return Result<Payment>.Failure(bindResult.Errors);

        if (payment.Splits.Count > 0)
        {
            var profitShareSplits = payment.Splits
                .Where(s => s.SplitKind == PaymentSplitKind.ProfitShare)
                .ToList();

            var recalculated = await _splitCalculation.ApplyStrawManFeeAsync(
                payment.Amount,
                profitShareSplits,
                strawManId);

            var replaceResult = payment.ReplaceSplits(recalculated);
            if (replaceResult.IsFailure)
                return Result<Payment>.Failure(replaceResult.Errors);
        }

        await _paymentRepository.UpdateAsync(payment);
        return Result<Payment>.Success(payment);
    }

    public async Task<IResult> PayAsync(string paymentId)
    {
        if (string.IsNullOrWhiteSpace(paymentId))
            return Result.Failure(Error.Create()
                .WithCode(PixPaymentErrorCodes.PaymentIdInvalid)
                .WithMessage("O ID do pagamento é obrigatório.")
                .Build());

        var payment = _paymentRepository.AsQueryable()
            .FirstOrDefault(p => p.Id == paymentId);

        if (payment is null)
            return Result.Failure(Error.Create()
                .WithCode(PixPaymentErrorCodes.PaymentNotFound)
                .WithMessage($"O pagamento '{paymentId}' não foi encontrado.")
                .Build());

        if (payment.OperatorId is not null)
        {
            var operatorExists = _accountRepository.AsQueryable()
                .Any(a => a.Id == payment.OperatorId);
            if (!operatorExists)
                return Result.Failure(Error.Create()
                    .WithCode(PixPaymentErrorCodes.OperatorNotFound)
                    .WithMessage($"A conta do operador '{payment.OperatorId}' não foi encontrada.")
                    .Build());
        }

        var result = payment.MarkAsPaid();
        if (result.IsFailure)
            return result;

        await _paymentRepository.UpdateAsync(payment);
        return Result.Success();
    }

    public async Task<IResult> RefundAsync(string paymentId)
    {
        if (string.IsNullOrWhiteSpace(paymentId))
            return Result.Failure(Error.Create()
                .WithCode(PixPaymentErrorCodes.PaymentIdInvalid)
                .WithMessage("O ID do pagamento é obrigatório.")
                .Build());

        var payment = _paymentRepository.AsQueryable()
            .FirstOrDefault(p => p.Id == paymentId);

        if (payment is null)
            return Result.Failure(Error.Create()
                .WithCode(PixPaymentErrorCodes.PaymentNotFound)
                .WithMessage($"O pagamento '{paymentId}' não foi encontrado.")
                .Build());

        var result = payment.Refund();
        if (result.IsFailure)
            return result;

        await _paymentRepository.UpdateAsync(payment);
        return Result.Success();
    }

    public async Task<IResult> KillAsync(string paymentId, string reason)
    {
        if (string.IsNullOrWhiteSpace(paymentId))
            return Result.Failure(Error.Create()
                .WithCode(PixPaymentErrorCodes.PaymentIdInvalid)
                .WithMessage("O ID do pagamento é obrigatório.")
                .Build());

        var payment = _paymentRepository.AsQueryable()
            .FirstOrDefault(p => p.Id == paymentId);

        if (payment is null)
            return Result.Failure(Error.Create()
                .WithCode(PixPaymentErrorCodes.PaymentNotFound)
                .WithMessage($"O pagamento '{paymentId}' não foi encontrado.")
                .Build());

        var result = payment.Kill(reason);
        if (result.IsFailure)
            return result;

        await _paymentRepository.UpdateAsync(payment);
        return Result.Success();
    }

    public async Task<IResult> MarkAsDistributedAsync(string paymentId)
    {
        if (string.IsNullOrWhiteSpace(paymentId))
            return Result.Failure(Error.Create()
                .WithCode(PixPaymentErrorCodes.PaymentIdInvalid)
                .WithMessage("O ID do pagamento é obrigatório.")
                .Build());

        var payment = _paymentRepository.AsQueryable()
            .FirstOrDefault(p => p.Id == paymentId);

        if (payment is null)
            return Result.Failure(Error.Create()
                .WithCode(PixPaymentErrorCodes.PaymentNotFound)
                .WithMessage($"O pagamento '{paymentId}' não foi encontrado.")
                .Build());

        var result = payment.MarkAsDistributed();
        if (result.IsFailure)
            return result;

        await _paymentRepository.UpdateAsync(payment);
        return Result.Success();
    }

    public async Task<IResult> DeletePaymentAsync(string paymentId)
    {
        if (string.IsNullOrWhiteSpace(paymentId))
            return Result.Failure(Error.Create()
                .WithCode(PixPaymentErrorCodes.PaymentIdInvalid)
                .WithMessage("O ID do pagamento é obrigatório.")
                .Build());

        var payment = _paymentRepository.AsQueryable()
            .FirstOrDefault(p => p.Id == paymentId);

        if (payment is null)
            return Result.Failure(Error.Create()
                .WithCode(PixPaymentErrorCodes.PaymentNotFound)
                .WithMessage($"O pagamento '{paymentId}' não foi encontrado.")
                .Build());

        await _paymentRepository.DeleteAsync(payment);
        return Result.Success();
    }

    private static IReadOnlyList<PaymentSplit> BuildEqualSplits(decimal amount, IReadOnlyList<string> accountIds)
    {
        if (accountIds.Count == 0)
            return Array.Empty<PaymentSplit>();

        var basePercentage = ProfitSharePercentageRules.Round(100m / accountIds.Count);
        var cuts = accountIds
            .Select(id => new ProfitSplitRecord
            {
                AccountId = id,
                Percentage = basePercentage,
            })
            .ToList();

        var normalized = ProfitSharePercentageRules.NormalizeCuts(cuts);
        return PaymentSplit.AllocateFromCuts(
            amount,
            normalized.Select(cut => (cut.AccountId, cut.Percentage)).ToList());
    }
}

using Aidan.Core.Errors;
using Nexus.Payments.Application.Contracts;
using Nexus.Operations.Application.Contracts;
using Aidan.Core.Patterns;
using Nexus.Operations.Aggregates;
using Nexus.Payments.Aggregates;
using Nexus.Payments.Application.Models;
using Nexus.Accounts.Application.Contracts;
using Nexus.Payments.Errors;
using Nexus.Database.Models;

namespace Nexus.Payments.Application.Services;

public sealed class PaymentService : IPaymentService
{
    private IAccountRepository _accountRepository { get; }
    private IPaymentRepository _paymentRepository { get; }
    private IOperationRepository _operationRepository { get; }
    private ITeamRepository _teamRepository { get; }

    public PaymentService(
        IAccountRepository accountRepository,
        IPaymentRepository pixPaymentRepository,
        IOperationRepository operationRepository,
        ITeamRepository teamRepository)
    {
        _accountRepository = accountRepository;
        _paymentRepository = pixPaymentRepository;
        _operationRepository = operationRepository;
        _teamRepository = teamRepository;
    }

    public async Task<IResult<Payment>> CreatePaymentAsync(CreatePaymentRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        // Normalize inputs to avoid accepting "   " values and to prevent nullability warnings later.
        var amount = request.Amount;
        var operationId = request.OperationId?.Trim();
        var teamId = request.TeamId?.Trim();
        var gateway = request.Gateway;
        var gatewayPaymentId = request.GatewayPaymentId?.Trim();
        var operatorAccountId = request.OperatorAccountId?.Trim();
        var strawManAccountId = request.StrawManAccountId?.Trim();

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

        if (operatorAccountId is not null && string.IsNullOrWhiteSpace(operatorAccountId))
            builder.WithError(Error.Create()
                .WithCode(PixPaymentErrorCodes.OperatorInvalid)
                .WithMessage("O ID do operador não pode estar vazio quando informado.")
                .Build());

        if (strawManAccountId is not null && string.IsNullOrWhiteSpace(strawManAccountId))
            builder.WithError(Error.Create()
                .WithCode(PixPaymentErrorCodes.StrawManInvalid)
                .WithMessage("O ID da conta laranja não pode estar vazio quando informado.")
                .Build());

        if (operatorAccountId is not null && string.IsNullOrWhiteSpace(teamId))
            builder.WithError(Error.Create()
                .WithCode(PixPaymentErrorCodes.TeamIdRequired)
                .WithMessage("O ID da equipe é obrigatório quando o operador é informado.")
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

        var operationExists = _operationRepository.AsQueryable()
            .Any(o => o.Id == operationId);
        if (!operationExists)
            builder.WithError(Error.Create()
                .WithCode(PixPaymentErrorCodes.OperationNotFound)
                .WithMessage($"A operação '{operationId}' não foi encontrada.")
                .Build());

        if (operatorAccountId is not null)
        {
            var operatorExists = _accountRepository.AsQueryable()
                .Any(a => a.Id == operatorAccountId);
            if (!operatorExists)
                builder.WithError(Error.Create()
                    .WithCode(PixPaymentErrorCodes.OperatorAccountNotFound)
                    .WithMessage($"A conta do operador '{operatorAccountId}' não foi encontrada.")
                    .Build());
        }

        if (strawManAccountId is not null)
        {
            var strawManExists = _accountRepository.AsQueryable()
                .Any(a => a.Id == strawManAccountId);
            if (!strawManExists)
                builder.WithError(Error.Create()
                    .WithCode(PixPaymentErrorCodes.StrawManAccountNotFound)
                    .WithMessage($"A conta laranja '{strawManAccountId}' não foi encontrada.")
                    .Build());
        }

        IReadOnlyList<PaymentSplit> splits = Array.Empty<PaymentSplit>();
        var resolvedTeamId = teamId ?? string.Empty;

        if (operatorAccountId is not null)
        {
            var team = _teamRepository.AsQueryable()
                .FirstOrDefault(t => t.Id == teamId);

            if (team is null)
            {
                builder.WithError(Error.Create()
                    .WithCode(PixPaymentErrorCodes.TeamNotFound)
                    .WithMessage($"A equipe '{teamId}' não foi encontrada.")
                    .Build());
            }
            else if (!string.Equals(team.OperationId, operationId, StringComparison.Ordinal))
            {
                builder.WithError(Error.Create()
                    .WithCode(PixPaymentErrorCodes.TeamNotFound)
                    .WithMessage($"A equipe '{teamId}' não pertence à operação '{operationId}'.")
                    .Build());
            }
            else if (!team.OperatorIds.Contains(operatorAccountId))
            {
                builder.WithError(Error.Create()
                    .WithCode(PixPaymentErrorCodes.OperatorNotOnTeam)
                    .WithMessage($"O operador '{operatorAccountId}' não pertence à equipe '{teamId}'.")
                    .Build());
            }
            else
            {
                var rule = team.OperatorProfitShareRules
                    .FirstOrDefault(r => string.Equals(r.OperatorId, operatorAccountId, StringComparison.Ordinal));

                if (rule is null || rule.Cuts.Count == 0)
                {
                    builder.WithError(Error.Create()
                        .WithCode(PixPaymentErrorCodes.ProfitShareRuleNotFound)
                        .WithMessage($"Não há regra de repasse configurada para o operador '{operatorAccountId}'.")
                        .Build());
                }
                else
                {
                    var normalizedCuts = ProfitSharePercentageRules.NormalizeCuts(rule.Cuts);
                    splits = PaymentSplit.CreateSnapshot(
                        amount,
                        normalizedCuts
                            .Select(cut => (cut.AccountId, cut.Percentage))
                            .ToList());
                    resolvedTeamId = team.Id;
                }
            }
        }

        if (builder.ContainsError)
            return builder.Build();

        // At this point, gatewayPaymentId is validated (not null/empty/whitespace).
        var validatedGatewayPaymentId = gatewayPaymentId!;

        var id = string.IsNullOrWhiteSpace(explicitPaymentId) ? string.Empty : explicitPaymentId!;
        var createdAt = DateTime.UtcNow;
        var payment = new Payment(
            id,
            operationId!,
            resolvedTeamId,
            gateway,
            validatedGatewayPaymentId,
            amount,
            splits,
            PaymentStatus.Pending,
            PaymentSettlementStatus.Unsettled,
            OperatorAccountId: null,
            StrawManAccountId: null,
            createdAt,
            PaidAt: null,
            RefundedAt: null,
            DiedAt: null,
            DeathReason: null,
            WithdrawnAt: null);

        if (strawManAccountId is not null)
        {
            var bindStrawManResult = payment.BindToStrawMan(strawManAccountId);
            if (bindStrawManResult.IsFailure)
                return Result.Create<Payment>().WithErrors(bindStrawManResult.Errors).Build();
        }

        if (operatorAccountId is not null)
        {
            var bindOperatorResult = payment.BindToOperator(operatorAccountId);
            if (bindOperatorResult.IsFailure)
                return Result.Create<Payment>().WithErrors(bindOperatorResult.Errors).Build();
        }

        payment = await _paymentRepository.CreateAsync(payment);
        return builder.WithValue(payment).Build();
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

        if (payment.OperatorAccountId is not null)
        {
            var operatorExists = _accountRepository.AsQueryable()
                .Any(a => a.Id == payment.OperatorAccountId);
            if (!operatorExists)
                return Result.Failure(Error.Create()
                    .WithCode(PixPaymentErrorCodes.OperatorAccountNotFound)
                    .WithMessage($"A conta do operador '{payment.OperatorAccountId}' não foi encontrada.")
                    .Build());
        }

        var result = payment.MarkAsPaid();
        if (result.IsFailure)
            return result;

        await _paymentRepository.UpdateAsync(payment);
        return Result.Success();
    }

    public async Task<IResult> MarkAsWithdrawnAsync(string paymentId)
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

        var result = payment.MarkAsWithdrawn();
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

        var result = payment.Die(reason);
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
}

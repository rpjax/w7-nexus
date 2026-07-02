using Aidan.Core.Errors;
using Aidan.Core.Patterns;
using Nexus.Accounts.Application.Contracts;
using Nexus.Authorization;
using Nexus.Charges.Application.Contracts;
using Nexus.Database.Models;
using Nexus.Operations.Aggregates;
using Nexus.Operations.Application.Contracts;
using Nexus.Payments.Aggregates;
using Nexus.Payments.Errors;

namespace Nexus.Charges.Application.Services;

public sealed class ChargeProfitShareResolver : IChargeProfitShareResolver
{
    private readonly IAccountRepository _accountRepository;
    private readonly IOperationRepository _operationRepository;
    private readonly ITeamRepository _teamRepository;

    public ChargeProfitShareResolver(
        IAccountRepository accountRepository,
        IOperationRepository operationRepository,
        ITeamRepository teamRepository)
    {
        _accountRepository = accountRepository;
        _operationRepository = operationRepository;
        _teamRepository = teamRepository;
    }

    public Task<IResult<IReadOnlyList<PaymentSplit>>> ResolveSplitsAsync(
        string operationId,
        string? operatorId,
        decimal amount,
        CancellationToken cancellationToken = default)
    {
        operationId = operationId?.Trim() ?? string.Empty;
        operatorId = operatorId?.Trim();

        var builder = Result.Create<IReadOnlyList<PaymentSplit>>();

        if (string.IsNullOrWhiteSpace(operationId))
        {
            builder.WithError(Error.Create()
                .WithCode(PixPaymentErrorCodes.OperationIdInvalid)
                .WithMessage("O ID da operação é obrigatório.")
                .Build());
            return Task.FromResult<IResult<IReadOnlyList<PaymentSplit>>>(builder.Build());
        }

        var operation = _operationRepository.AsQueryable()
            .FirstOrDefault(o => o.Id == operationId);

        if (operation is null)
        {
            builder.WithError(Error.Create()
                .WithCode(PixPaymentErrorCodes.OperationNotFound)
                .WithMessage($"A operação '{operationId}' não foi encontrada.")
                .Build());
            return Task.FromResult<IResult<IReadOnlyList<PaymentSplit>>>(builder.Build());
        }

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
                return Task.FromResult<IResult<IReadOnlyList<PaymentSplit>>>(builder.Build());
            }

            if (matchingTeams.Count > 1)
            {
                builder.WithError(Error.Create()
                    .WithCode(PixPaymentErrorCodes.TeamAmbiguous)
                    .WithMessage("Há mais de uma equipe compatível com o operador informado.")
                    .Build());
                return Task.FromResult<IResult<IReadOnlyList<PaymentSplit>>>(builder.Build());
            }

            var team = matchingTeams[0];
            var rule = team.OperatorProfitShareRules
                .FirstOrDefault(r => string.Equals(r.OperatorId, operatorId, StringComparison.Ordinal));

            if (rule is null || rule.Cuts.Count == 0)
            {
                builder.WithError(Error.Create()
                    .WithCode(PixPaymentErrorCodes.ProfitShareRuleNotFound)
                    .WithMessage($"Não há regra de repasse configurada para o operador '{operatorId}'.")
                    .Build());
                return Task.FromResult<IResult<IReadOnlyList<PaymentSplit>>>(builder.Build());
            }

            var normalizedCuts = ProfitSharePercentageRules.NormalizeCuts(rule.Cuts);
            var splits = PaymentSplit.AllocateFromCuts(
                amount,
                normalizedCuts
                    .Select(cut => (cut.AccountId, cut.Percentage))
                    .ToList());

            return Task.FromResult<IResult<IReadOnlyList<PaymentSplit>>>(builder.WithValue(splits).Build());
        }

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
            return Task.FromResult<IResult<IReadOnlyList<PaymentSplit>>>(builder.Build());
        }

        return Task.FromResult<IResult<IReadOnlyList<PaymentSplit>>>(
            builder.WithValue(BuildEqualSplits(amount, recipientIds)).Build());
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

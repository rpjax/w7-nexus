using Aidan.Core.Errors;
using Aidan.Core.Linq.Extensions;
using Aidan.Core.Patterns;
using Nexus.Authorization.Application.Models;
using Nexus.Operators.Application.Contracts;
using Nexus.Operators.Application.Services;
using Nexus.Operations.Aggregates;
using Nexus.Payments.Aggregates;
using Nexus.Payments.Application.Contracts;
using Nexus.Payments.Application.Mapping;
using Nexus.Payments.Application.Models;
using Nexus.Payments.Application.Services;
using Nexus.Payments.Errors;
using Nexus.Operations.Application.Contracts;

namespace Nexus.Operators.Application.Services;

public sealed class OperatorPaymentSearchService : IOperatorPaymentSearchService
{
    private IPaymentRepository _payments { get; }
    private ITeamRepository _teams { get; }
    private IPaymentDetailsEnrichmentService _enrichment { get; }

    public OperatorPaymentSearchService(
        IPaymentRepository payments,
        ITeamRepository teams,
        IPaymentDetailsEnrichmentService enrichment)
    {
        _payments = payments;
        _teams = teams;
        _enrichment = enrichment;
    }

    public async Task<IResult<SearchPaymentsResponse>> SearchPaymentsAsync(
        RequesterIdentity identity,
        SearchPaymentsRequest? request)
    {
        var validation = PaymentSearchValidator.Validate(request);
        if (validation.IsFailure)
            return Result<SearchPaymentsResponse>.Failure(validation.Errors);

        var (limit, offset, keyword) = validation.Value;
        request ??= new SearchPaymentsRequest();
        var accountId = identity.AccountId.Trim();

        var assignedTeams = await OperatorOperationResolver.ResolveAssignedTeamsAsync(accountId, _teams);
        var scoped = await LoadScopedPaymentsAsync(accountId, assignedTeams);
        var filtered = ApplyKeywordFilter(scoped, keyword);

        var ordered = filtered
            .OrderByDescending(p => p.CreatedAt)
            .ToList();

        var total = ordered.Count;
        var page = ordered
            .Skip(offset)
            .Take(limit)
            .ToArray();

        return Result<SearchPaymentsResponse>.Success(new SearchPaymentsResponse
        {
            Offset = offset,
            Limit = limit,
            Total = total,
            Items = await _enrichment.EnrichManyAsync(PaymentDetailsMapper.MapMany(page)),
        });
    }

    public async Task<IResult<PaymentDetails>> GetPaymentAsync(
        RequesterIdentity identity,
        string paymentId)
    {
        if (string.IsNullOrWhiteSpace(paymentId))
        {
            return Result<PaymentDetails>.Failure(Error.Create()
                .WithCode(PixPaymentErrorCodes.PaymentIdInvalid)
                .WithMessage("O ID do pagamento é obrigatório.")
                .Build());
        }

        var payment = await _payments.AsQueryable()
            .Where(p => p.Id == paymentId)
            .FirstOrDefaultAsync();

        if (payment is null)
        {
            return Result<PaymentDetails>.Failure(Error.Create()
                .WithCode(PixPaymentErrorCodes.PaymentNotFound)
                .WithMessage($"O pagamento '{paymentId}' não foi encontrado.")
                .Build());
        }

        var accountId = identity.AccountId.Trim();
        var assignedTeams = await OperatorOperationResolver.ResolveAssignedTeamsAsync(accountId, _teams);

        if (!IsPaymentVisibleToOperator(payment, accountId, assignedTeams))
        {
            return Result<PaymentDetails>.Failure(Error.Create()
                .WithCode(PixPaymentErrorCodes.AccessDenied)
                .WithMessage("Você não tem permissão para visualizar este pagamento.")
                .Build());
        }

        return Result<PaymentDetails>.Success(
            await _enrichment.EnrichAsync(PaymentDetailsMapper.Map(payment)));
    }

    private async Task<List<Payment>> LoadScopedPaymentsAsync(string accountId, IReadOnlyList<Team> assignedTeams)
    {
        var byOperator = await _payments.AsQueryable()
            .Where(p => p.OperatorId == accountId)
            .ToArrayAsync();

        var bySplit = await _payments.AsQueryable()
            .Where(p => p.Splits.Any(s => s.AccountId == accountId))
            .ToArrayAsync();

        var operationIds = assignedTeams
            .Select(t => t.OperationId)
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        var byTeamScope = operationIds.Length == 0
            ? Array.Empty<Payment>()
            : await _payments.AsQueryable()
                .Where(p => operationIds.Contains(p.OperationId))
                .ToArrayAsync();

        var byTeam = byTeamScope
            .Where(p => assignedTeams.Any(t =>
                t.OperationId == p.OperationId &&
                (t.OperatorIds.Contains(p.OperatorId ?? string.Empty) ||
                 t.StrawManIds.Contains(p.StrawManId))))
            .ToArray();

        return byOperator
            .Concat(byTeam)
            .Concat(bySplit)
            .DistinctBy(p => p.Id)
            .ToList();
    }

    private static bool IsPaymentVisibleToOperator(
        Payment payment,
        string accountId,
        IReadOnlyList<Team> assignedTeams) =>
        string.Equals(payment.OperatorId, accountId, StringComparison.Ordinal)
        || assignedTeams.Any(t =>
            t.OperationId == payment.OperationId &&
            (t.OperatorIds.Contains(payment.OperatorId ?? string.Empty) ||
             t.StrawManIds.Contains(payment.StrawManId)))
        || payment.Splits.Any(split => string.Equals(split.AccountId, accountId, StringComparison.Ordinal));

    private static IEnumerable<Payment> ApplyKeywordFilter(IEnumerable<Payment> payments, string? keyword)
    {
        if (string.IsNullOrWhiteSpace(keyword))
            return payments;

        var term = keyword.ToLowerInvariant();
        return payments.Where(p =>
            p.Id.ToLower().Contains(term)
            || p.OperationId.ToLower().Contains(term)
            || p.GatewayTransactionId.ToLower().Contains(term)
            || (p.OperatorId != null && p.OperatorId.ToLower().Contains(term))
            || p.StrawManId.ToLower().Contains(term));
    }
}

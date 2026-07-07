using Aidan.Core.Errors;
using Aidan.Core.Linq.Extensions;
using Aidan.Core.Patterns;
using Nexus.Authorization.Application.Models;
using Nexus.Payments.Application.Contracts;
using Nexus.Payments.Application.Mapping;
using Nexus.Payments.Application.Models;
using Nexus.Payments.Errors;

namespace Nexus.Payments.Application.Services;

public sealed class StrawManPaymentSearchService : IStrawManPaymentSearchService
{
    private IPaymentRepository _payments { get; }
    private IPaymentDetailsEnrichmentService _enrichment { get; }

    public StrawManPaymentSearchService(
        IPaymentRepository payments,
        IPaymentDetailsEnrichmentService enrichment)
    {
        _payments = payments;
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

        var query = _payments.AsQueryable()
            .Where(p => p.StrawManId == accountId)
            .ApplyKeywordFilter(keyword);

        var total = await query.CountAsync();

        var items = await query
            .OrderByDescending(p => p.CreatedAt)
            .Skip(offset)
            .Take(limit)
            .ToArrayAsync();

        return Result<SearchPaymentsResponse>.Success(new SearchPaymentsResponse
        {
            Offset = offset,
            Limit = limit,
            Total = total,
            Items = await _enrichment.EnrichManyAsync(PaymentDetailsMapper.MapMany(items)),
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

        var accountId = identity.AccountId.Trim();
        var payment = await _payments.AsQueryable()
            .Where(p => p.Id == paymentId && p.StrawManId == accountId)
            .FirstOrDefaultAsync();

        if (payment is null)
        {
            return Result<PaymentDetails>.Failure(Error.Create()
                .WithCode(PixPaymentErrorCodes.PaymentNotFound)
                .WithMessage($"O pagamento '{paymentId}' não foi encontrado.")
                .Build());
        }

        return Result<PaymentDetails>.Success(
            await _enrichment.EnrichAsync(PaymentDetailsMapper.Map(payment)));
    }
}

using Aidan.Core.Linq;
using Aidan.Core.Linq.Extensions;
using Nexus.Payments.Aggregates;
using Nexus.Payments.Application.Models;

namespace Nexus.Payments.Application.Services;

internal static class PaymentSearchQueryExtensions
{
    public static IAsyncQueryable<Payment> ApplyKeywordFilter(
        this IAsyncQueryable<Payment> query,
        string? keyword)
    {
        if (string.IsNullOrWhiteSpace(keyword))
            return query;

        var term = keyword.ToLowerInvariant();
        return query.Where(p =>
            p.Id.ToLower().Contains(term)
            || p.OperationId.ToLower().Contains(term)
            || p.GatewayTransactionId.ToLower().Contains(term)
            || (p.OperatorId != null && p.OperatorId.ToLower().Contains(term))
            || p.StrawManId.ToLower().Contains(term));
    }

    public static IAsyncQueryable<Payment> ApplyAdminFilters(
        this IAsyncQueryable<Payment> query,
        SearchPaymentsRequest request)
    {
        if (request.Status is PaymentStatus status)
            query = query.Where(p => p.Status == status);

        if (request.SettlementStatus is PaymentSettlementStatus settlementStatus)
            query = query.Where(p => p.SettlementStatus == settlementStatus);

        if (request.DistributionStatus is PaymentDistributionStatus distributionStatus)
            query = query.Where(p => p.DistributionStatus == distributionStatus);

        var operationId = request.OperationId?.Trim();
        if (!string.IsNullOrWhiteSpace(operationId))
            query = query.Where(p => p.OperationId == operationId);

        var strawManId = request.StrawManId?.Trim();
        if (!string.IsNullOrWhiteSpace(strawManId))
            query = query.Where(p => p.StrawManId == strawManId);

        return query;
    }
}

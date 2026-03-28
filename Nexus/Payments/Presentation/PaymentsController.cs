using Aidan.Core.Errors;
using Aidan.Core.Linq.Extensions;
using Aidan.Web.Controllers;
using Microsoft.AspNetCore.Mvc;
using Nexus.Payments.Aggregates;
using Nexus.Payments.Application;
using Nexus.Payments.Application.Models;

namespace Nexus.Payments.Presentation;

[Route("api/payments")]
public sealed class PaymentsController : WebController
{
    private readonly IPaymentRepository _paymentRepository;

    public PaymentsController(IPaymentRepository paymentRepository)
    {
        _paymentRepository = paymentRepository;
    }

    private static object ToPaymentResponse(Payment p) => new
    {
        p.Id,
        p.OperationId,
        p.OperatorAccountId,
        p.StrawManAccountId,
        Gateway = p.Gateway.ToString(),
        GatewayTransactionId = p.GatewayTransactionId,
        p.Amount,
        Status = p.Status.ToString(),
        p.CreatedAt,
        p.PaidAt,
        p.RefundedAt,
        p.DiedAt,
        p.DeathReason,
    };

    [HttpPost("search")]
    public async Task<ActionResult> SearchAsync([FromBody] SearchPaymentsRequest? request)
    {
        request ??= new SearchPaymentsRequest();

        var limit = request.Limit <= 0 ? 30 : request.Limit;
        var offset = request.Offset;
        var keyword = request.Keyword?.Trim();

        if (limit < 0 || limit >= 1000)
        {
            return ProblemResponse(422, Error.Create()
                .WithCode("Payment.SEARCH_LIMIT_INVALID")
                .WithMessage("Limit must be between 1 and 999.")
                .Build());
        }

        if (offset < 0)
        {
            return ProblemResponse(422, Error.Create()
                .WithCode("Payment.SEARCH_OFFSET_INVALID")
                .WithMessage("Offset cannot be negative.")
                .Build());
        }

        if (!string.IsNullOrWhiteSpace(keyword) && keyword.Length > 200)
        {
            return ProblemResponse(422, Error.Create()
                .WithCode("Payment.SEARCH_KEYWORD_TOO_LONG")
                .WithMessage("Keyword can have at most 200 characters.")
                .Build());
        }

        var query = _paymentRepository.AsQueryable();

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var term = keyword.ToLowerInvariant();
            query = query.Where(p =>
                p.Id.ToLower().Contains(term)
                || p.OperationId.ToLower().Contains(term)
                || p.GatewayTransactionId.ToLower().Contains(term)
                || (p.OperatorAccountId != null && p.OperatorAccountId.ToLower().Contains(term))
                || (p.StrawManAccountId != null && p.StrawManAccountId.ToLower().Contains(term)));
        }

        var total = await query.CountAsync();

        var items = await query
            .OrderByDescending(p => p.CreatedAt)
            .Skip(offset)
            .Take(limit)
            .ToArrayAsync();

        return Ok(new
        {
            Total = total,
            Items = items.Select(ToPaymentResponse).ToArray(),
        });
    }
}

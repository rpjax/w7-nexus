using Nexus.Payments.Application.Models;

namespace Nexus.Payments.Application.Contracts;

public interface IPaymentDetailsEnrichmentService
{
    Task<PaymentDetails> EnrichAsync(PaymentDetails details, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PaymentDetails>> EnrichManyAsync(
        IReadOnlyList<PaymentDetails> items,
        CancellationToken cancellationToken = default);
}

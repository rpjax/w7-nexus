using Aidan.Core.Patterns;
using Nexus.Payments.Application.Contracts;
using Nexus.Payments.Application.Models;
using Nexus.Payments.Application.Services;

namespace Nexus.Tests.Payments;

internal static class PaymentTestDoubles
{
    internal sealed class PassthroughPaymentDetailsEnrichmentService : IPaymentDetailsEnrichmentService
    {
        public Task<PaymentDetails> EnrichAsync(PaymentDetails details, CancellationToken cancellationToken = default) =>
            Task.FromResult(details);

        public Task<IReadOnlyList<PaymentDetails>> EnrichManyAsync(
            IReadOnlyList<PaymentDetails> items,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(items);
    }

    internal static IPaymentDetailsEnrichmentService PassthroughEnrichment() =>
        new PassthroughPaymentDetailsEnrichmentService();
}

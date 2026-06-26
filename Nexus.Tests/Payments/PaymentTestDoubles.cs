using Aidan.Core.Patterns;
using Nexus.Payments.Application.Contracts;
using Nexus.Payments.Application.Models;
using Nexus.Payments.Application.Services;
using Nexus.StrawMen.Application.Contracts;

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
    internal sealed class StubStrawManSettingsQueryService : IStrawManSettingsQueryService
    {
        private readonly Dictionary<string, decimal> _fees;

        public StubStrawManSettingsQueryService(Dictionary<string, decimal>? fees = null) =>
            _fees = fees ?? new Dictionary<string, decimal>(StringComparer.Ordinal);

        public Task<decimal> GetMovementFeePercentageAsync(
            string strawManId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(_fees.TryGetValue(strawManId, out var fee) ? fee : 0m);

        public Task<IResult<StrawManSettingsDetails>> GetSettingsAsync(
            string strawManId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IResult<StrawManSettingsDetails>>(Result<StrawManSettingsDetails>.Success(new StrawManSettingsDetails
            {
                StrawManId = strawManId,
                MovementFeePercentage = _fees.TryGetValue(strawManId, out var fee) ? fee : 0m,
            }));
    }

    internal static PaymentSplitCalculationService SplitCalculation(
        Dictionary<string, decimal>? fees = null) =>
        new(new StubStrawManSettingsQueryService(fees));
}

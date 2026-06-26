using Nexus.Payments.Application.Contracts;

namespace Nexus.Payments.Application.Services;

public sealed class NoOpPaymentDistributionTracker : IPaymentDistributionTracker
{
    public Task EvaluateAfterPayoutAsync(string transferId, CancellationToken cancellationToken = default)
        => Task.CompletedTask;
}

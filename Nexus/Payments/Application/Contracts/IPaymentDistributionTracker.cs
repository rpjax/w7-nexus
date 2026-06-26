namespace Nexus.Payments.Application.Contracts;

/// <summary>
/// Hook for future automation: evaluate transfer/payout activity and update payment distribution status.
/// V1 uses manual <see cref="IPaymentService.MarkAsDistributedAsync"/>; wire this from TransferService in phase 2.
/// </summary>
public interface IPaymentDistributionTracker
{
    Task EvaluateAfterPayoutAsync(string transferId, CancellationToken cancellationToken = default);
}

using Refactor.Nexus.Api.Charging.Application.Ports.Out.Issuing;

namespace Refactor.Nexus.Api.Charging.Infrastructure.Issuing;

public sealed class NoOpPaymentIssuer : IPaymentIssuer
{
    public Task<PaymentIssueResult> IssueAsync(
        Guid chargeId,
        decimal grossAmount,
        string currency,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(new PaymentIssueResult($"noop-{chargeId:N}"));
}

using Refactor.Nexus.Api.Ledger.Application.Ports.Out.Persistence;
using Refactor.Nexus.Api.WorldAccounts.Application.Ports.Out.Ledger;

namespace Refactor.Nexus.Api.Ledger.Infrastructure.Persistence;

public sealed class LedgerClaimObservationAdapter : ILedgerClaimObservationPort
{
    private readonly IClaimRepository _claims;

    public LedgerClaimObservationAdapter(IClaimRepository claims)
    {
        _claims = claims;
    }

    public async Task<LedgerClaimPresence> GetPresenceAsync(
        Guid worldAccountId,
        string currency,
        CancellationToken cancellationToken = default)
    {
        var currencyNorm = currency.Trim().ToUpperInvariant();
        var located = await _claims.ListAsync(null, worldAccountId, null, cancellationToken);
        var matching = located
            .Where(c => string.Equals(c.Currency, currencyNorm, StringComparison.OrdinalIgnoreCase))
            .ToList();
        return new LedgerClaimPresence(
            matching.Count > 0,
            matching.Any(c => c.IsActive));
    }
}

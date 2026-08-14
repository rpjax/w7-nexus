using Refactor.Nexus.Api.Ledger.Application.Ports.Out.Persistence;
using ChargeAggregate = Refactor.Nexus.Api.Charging.Domain.Aggregates.Charge.Charge;
using ClaimAggregate = Refactor.Nexus.Api.Ledger.Domain.Aggregates.Claim.Claim;
using WorldAccountAggregate = Refactor.Nexus.Api.WorldAccounts.Domain.Aggregates.WorldAccount.WorldAccount;

namespace Refactor.Nexus.Api.Ledger.Infrastructure.Persistence;

public sealed class MaterializationCommitAdapter : IMaterializationCommit
{
    private readonly ILedgerCommit _commit;

    public MaterializationCommitAdapter(ILedgerCommit commit)
    {
        _commit = commit;
    }

    public Task SaveAsync(
        ChargeAggregate charge,
        WorldAccountAggregate account,
        IReadOnlyList<ClaimAggregate> claims,
        CancellationToken cancellationToken = default) =>
        _commit.SaveAsync([account], claims, hop: null, charge, cancellationToken);
}

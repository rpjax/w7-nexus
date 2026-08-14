using Refactor.Nexus.Api.Mandates.Application.Ports.Out.Identity;
using Refactor.Nexus.Api.Mandates.Application.Ports.Out.Persistence;
using Refactor.Nexus.Api.Mandates.Domain.Aggregates;
using Refactor.Nexus.Api.Mandates.Domain.Catalog;
using Refactor.Nexus.Api.Mandates.Domain.ValueObjects;
using Refactor.Nexus.Api.WorldAccounts.Application.Ports.Out.Mandates;

namespace Refactor.Nexus.Api.WorldAccounts.Infrastructure.Mandates;

public sealed class WorldAccountAccessAdapter : IWorldAccountAccess
{
    private readonly IAccountDirectory _accounts;
    private readonly IMemberMandateReadRepository _mandates;

    public WorldAccountAccessAdapter(IAccountDirectory accounts, IMemberMandateReadRepository mandates)
    {
        _accounts = accounts;
        _mandates = mandates;
    }

    public Task<bool> IsAdministratorAsync(Guid accountId, CancellationToken cancellationToken = default) =>
        _accounts.IsAdministratorAsync(new MemberId(accountId), cancellationToken);

    public async Task<bool> CanManageGatewaysAsync(Guid accountId, CancellationToken cancellationToken = default)
    {
        if (await IsAdministratorAsync(accountId, cancellationToken))
            return true;

        var mandate = await _mandates.GetByMemberIdAsync(new MemberId(accountId), cancellationToken);
        if (mandate is null)
            return false;

        return mandate.HasCapability(Capabilities.GerirGateways, MandateScope.Organization())
            || mandate.AppliedPresets.Contains(PresetIds.Gateways, StringComparer.OrdinalIgnoreCase);
    }

    public async Task<bool> IsEligibleOrangeAsync(Guid accountId, CancellationToken cancellationToken = default)
    {
        var memberId = new MemberId(accountId);
        if (!await _accounts.ExistsAsync(memberId, cancellationToken))
            return false;

        var mandate = await _mandates.GetByMemberIdAsync(memberId, cancellationToken);
        if (mandate is null)
            return false;

        return mandate.AppliedPresets.Contains(PresetIds.Orange, StringComparer.OrdinalIgnoreCase)
            || mandate.HasCapability(Capabilities.AtuarComoLaranja, MandateScope.Organization());
    }
}

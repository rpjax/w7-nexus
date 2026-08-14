using Refactor.Nexus.Api.Mandates.Application.Ports.Out.Identity;
using Refactor.Nexus.Api.Mandates.Application.Ports.Out.Persistence;
using Refactor.Nexus.Api.Mandates.Domain.Aggregates;
using Refactor.Nexus.Api.Mandates.Domain.Catalog;
using Refactor.Nexus.Api.Mandates.Domain.ValueObjects;
using Refactor.Nexus.Api.Ledger.Application.Ports.Out.Mandates;

namespace Refactor.Nexus.Api.Ledger.Infrastructure.Mandates;

public sealed class LedgerAccessAdapter : ILedgerAccess
{
    private readonly IAccountDirectory _accounts;
    private readonly IMemberMandateReadRepository _mandates;

    public LedgerAccessAdapter(IAccountDirectory accounts, IMemberMandateReadRepository mandates)
    {
        _accounts = accounts;
        _mandates = mandates;
    }

    public async Task<bool> CanMaterializeAsync(Guid accountId, CancellationToken cancellationToken = default)
    {
        if (await _accounts.IsAdministratorAsync(new MemberId(accountId), cancellationToken))
            return true;

        var mandate = await _mandates.GetByMemberIdAsync(new MemberId(accountId), cancellationToken);
        if (mandate is null)
            return false;

        return mandate.HasCapability(Capabilities.RegistrarMovimentoFinanceiro, MandateScope.Organization())
            || mandate.AppliedPresets.Contains(PresetIds.Accountant, StringComparer.OrdinalIgnoreCase);
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

using Refactor.Nexus.Api.Journal.Application.Ports.Out.Mandates;
using Refactor.Nexus.Api.Mandates.Application.Ports.Out.Identity;
using Refactor.Nexus.Api.Mandates.Application.Ports.Out.Persistence;
using Refactor.Nexus.Api.Mandates.Domain.Aggregates;
using Refactor.Nexus.Api.Mandates.Domain.Catalog;
using Refactor.Nexus.Api.Mandates.Domain.ValueObjects;

namespace Refactor.Nexus.Api.Journal.Infrastructure.Mandates;

public sealed class JournalAccessAdapter : IJournalAccess
{
    private readonly IAccountDirectory _accounts;
    private readonly IMemberMandateReadRepository _mandates;

    public JournalAccessAdapter(IAccountDirectory accounts, IMemberMandateReadRepository mandates)
    {
        _accounts = accounts;
        _mandates = mandates;
    }

    public async Task<bool> CanReadAuditLogAsync(Guid accountId, CancellationToken cancellationToken = default)
    {
        var memberId = new MemberId(accountId);
        if (await _accounts.IsAdministratorAsync(memberId, cancellationToken))
            return true;

        var mandate = await _mandates.GetByMemberIdAsync(memberId, cancellationToken);
        return mandate is not null
            && mandate.HasCapability(Capabilities.LerLogAuditoria, MandateScope.Organization());
    }
}

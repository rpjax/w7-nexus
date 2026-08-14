using Refactor.Nexus.Api.Mandates.Application.Ports.Out.Identity;
using Refactor.Nexus.Api.Mandates.Application.Ports.Out.Persistence;
using Refactor.Nexus.Api.Mandates.Domain.Aggregates;
using Refactor.Nexus.Api.Mandates.Domain.Catalog;
using Refactor.Nexus.Api.Mandates.Domain.ValueObjects;
using Refactor.Nexus.Api.Operations.Application.Ports.Out.Mandates;
using Refactor.Nexus.Api.Operations.Domain.Aggregates.Operation;

namespace Refactor.Nexus.Api.Operations.Infrastructure.Mandates;

public sealed class MandateCapabilityGateAdapter : IMandateCapabilityGate
{
    private readonly IAccountDirectory _accountDirectory;
    private readonly IMemberMandateReadRepository _mandates;

    public MandateCapabilityGateAdapter(
        IAccountDirectory accountDirectory,
        IMemberMandateReadRepository mandates)
    {
        _accountDirectory = accountDirectory;
        _mandates = mandates;
    }

    public Task<bool> IsAdministratorAsync(Guid accountId, CancellationToken cancellationToken = default) =>
        _accountDirectory.IsAdministratorAsync(new MemberId(accountId), cancellationToken);

    public async Task<bool> CanManageOperationAsync(
        Guid accountId,
        OperationId operationId,
        CancellationToken cancellationToken = default)
    {
        if (await IsAdministratorAsync(accountId, cancellationToken))
            return true;

        return await HasManagementOverOperationAsync(accountId, operationId, cancellationToken);
    }

    public async Task<bool> HasManagementOverOperationAsync(
        Guid accountId,
        OperationId operationId,
        CancellationToken cancellationToken = default)
    {
        var mandate = await _mandates.GetByMemberIdAsync(new MemberId(accountId), cancellationToken);
        if (mandate is null)
            return false;

        var specific = MandateScope.OperationSpecific([operationId.Value]);
        if (specific.IsFailure)
            return false;

        return mandate.HasCapability(Capabilities.GerirOperacao, specific.Value!)
            || mandate.HasCapability(Capabilities.GerirOperacao, MandateScope.OperationAll())
            || mandate.HasCapability(Capabilities.GerirOperacao, MandateScope.Organization());
    }
}

public sealed class OperatorEligibilityAdapter : IOperatorEligibility
{
    private readonly IAccountDirectory _accountDirectory;
    private readonly IMemberMandateReadRepository _mandates;
    private readonly IAgencyDealReadRepository _deals;

    public OperatorEligibilityAdapter(
        IAccountDirectory accountDirectory,
        IMemberMandateReadRepository mandates,
        IAgencyDealReadRepository deals)
    {
        _accountDirectory = accountDirectory;
        _mandates = mandates;
        _deals = deals;
    }

    public async Task<bool> IsEligibleOperatorAsync(Guid accountId, CancellationToken cancellationToken = default)
    {
        var memberId = new MemberId(accountId);
        if (!await _accountDirectory.ExistsAsync(memberId, cancellationToken))
            return false;

        var mandate = await _mandates.GetByMemberIdAsync(memberId, cancellationToken);
        var isOperator = mandate?.AppliedPresets.Contains(PresetIds.Operator) == true
            || mandate?.HasCapability(Capabilities.AtuarComoOperador, MandateScope.Organization()) == true;

        if (!isOperator)
            return false;

        return await _deals.HasActiveDealForOperatorAsync(memberId, cancellationToken);
    }
}

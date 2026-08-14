using Aidan.Core.Patterns;
using Refactor.Nexus.Api.Accounts.Application.Ports.Out.Identity;
using Refactor.Nexus.Api.Authorization;
using Refactor.Nexus.Api.Mandates.Application.Ports.Out.Persistence;
using Refactor.Nexus.Api.Mandates.Application.UseCases.Shared;
using Refactor.Nexus.Api.Mandates.Domain.Aggregates;
using Refactor.Nexus.Api.Mandates.Domain.Aggregates.MemberMandate;
using Refactor.Nexus.Api.Mandates.Domain.Catalog;
using Refactor.Nexus.Api.Mandates.Domain.ValueObjects;

namespace Refactor.Nexus.Api.Mandates.Application.UseCases.Authenticated.Queries.GetMyMandate;

public sealed record GetMyMandateResult(
    string AccountId,
    IReadOnlyList<string> AppliedPresets,
    IReadOnlyList<string> Capabilities,
    bool CanGrant,
    bool CanManageOperations,
    bool CanManageGateways,
    bool CanSeeFinance,
    bool CanActAsOperator,
    bool CanRecruit);

public interface IGetMyMandateUseCase
{
    Task<IOperationResult<GetMyMandateResult>> HandleAsync(CancellationToken cancellationToken = default);
}

public sealed class GetMyMandateHandler : IGetMyMandateUseCase
{
    private readonly IRequestContext _requestContext;
    private readonly IMemberMandateReadRepository _mandates;

    public GetMyMandateHandler(IRequestContext requestContext, IMemberMandateReadRepository mandates)
    {
        _requestContext = requestContext;
        _mandates = mandates;
    }

    public async Task<IOperationResult<GetMyMandateResult>> HandleAsync(CancellationToken cancellationToken = default)
    {
        var requesterResult = await _requestContext.GetCurrentAsync(cancellationToken);
        if (requesterResult.IsFailure || requesterResult.Value is not RequesterContext requester)
            return OperationResult<GetMyMandateResult>.Failure(requesterResult.Errors);

        if (!MemberId.TryParse(requester.AccountId, out var memberId))
            return OperationResult<GetMyMandateResult>.Failure(MandateAdministratorGuards.Unauthorized("Identidade invalida."));

        var mandate = await _mandates.GetByMemberIdAsync(memberId, cancellationToken)
            ?? MemberMandate.Empty(memberId);

        var capabilities = mandate.Grants
            .Select(g => g.Capability)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(x => x)
            .ToList();

        return OperationResult<GetMyMandateResult>.Success(new GetMyMandateResult(
            memberId.ToString(),
            mandate.AppliedPresets.OrderBy(x => x).ToList(),
            capabilities,
            MandateAdministratorGuards.CanGrantNested(mandate)
                || requester.Roles.Contains(Roles.Administrator, StringComparer.OrdinalIgnoreCase),
            Has(mandate, Capabilities.GerirOperacao) || HasPreset(mandate, PresetIds.OperationsManager),
            Has(mandate, Capabilities.GerirGateways) || HasPreset(mandate, PresetIds.Gateways),
            Has(mandate, Capabilities.VerFinanceiroAmplo)
                || Has(mandate, Capabilities.RegistrarMovimentoFinanceiro)
                || HasPreset(mandate, PresetIds.Accountant),
            Has(mandate, Capabilities.AtuarComoOperador) || HasPreset(mandate, PresetIds.Operator),
            Has(mandate, Capabilities.Recrutar) || HasPreset(mandate, PresetIds.Recruiter)));
    }

    private static bool Has(MemberMandate mandate, string capability) =>
        mandate.HasCapability(capability, MandateScope.Organization())
        || mandate.HasCapability(capability, MandateScope.CarteiraDirect())
        || mandate.HasCapability(capability, MandateScope.OperationAll())
        || mandate.Grants.Any(g => string.Equals(g.Capability, capability, StringComparison.Ordinal));

    private static bool HasPreset(MemberMandate mandate, string preset) =>
        mandate.AppliedPresets.Contains(preset, StringComparer.OrdinalIgnoreCase);
}

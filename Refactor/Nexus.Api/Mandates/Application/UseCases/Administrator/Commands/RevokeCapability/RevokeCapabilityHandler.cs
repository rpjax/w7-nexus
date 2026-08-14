using Aidan.Core.Patterns;
using Refactor.Nexus.Api.Accounts.Application.Ports.Out.Identity;
using Refactor.Nexus.Api.Mandates.Application.Authorization;
using Refactor.Nexus.Api.Mandates.Application.Ports.In.Administrator.Commands;
using Refactor.Nexus.Api.Mandates.Application.Ports.Out.Persistence;
using Refactor.Nexus.Api.Mandates.Application.UseCases.Shared;
using Refactor.Nexus.Api.Mandates.Domain.Aggregates;
using Refactor.Nexus.Api.Mandates.Domain.Aggregates.MemberMandate;

namespace Refactor.Nexus.Api.Mandates.Application.UseCases.Administrator.Commands.RevokeCapability;

public sealed record RevokeCapabilityCommand(
    string AccountId,
    string Capability,
    string ScopeKind,
    IReadOnlyList<Guid>? OperationIds = null);

public sealed class RevokeCapabilityResult;

public sealed class RevokeCapabilityHandler : IRevokeCapabilityUseCase
{
    private readonly IRequestContext _requestContext;
    private readonly IMandateAccessPolicy _accessPolicy;
    private readonly IMemberMandateRepository _mandateRepository;
    private readonly IMemberMandateReadRepository _mandateReadRepository;

    public RevokeCapabilityHandler(
        IRequestContext requestContext,
        IMandateAccessPolicy accessPolicy,
        IMemberMandateRepository mandateRepository,
        IMemberMandateReadRepository mandateReadRepository)
    {
        _requestContext = requestContext;
        _accessPolicy = accessPolicy;
        _mandateRepository = mandateRepository;
        _mandateReadRepository = mandateReadRepository;
    }

    public async Task<IOperationResult<RevokeCapabilityResult>> HandleAsync(
        RevokeCapabilityCommand command,
        CancellationToken cancellationToken = default)
    {
        if (command is null)
            return OperationResult<RevokeCapabilityResult>.Failure(MandateAdministratorGuards.RequestBodyRequired());

        var access = await MandateAdministratorGuards.AuthorizeAdminAsync<RevokeCapabilityResult>(
            _requestContext, _accessPolicy, cancellationToken);
        if (access is not null)
            return access;

        if (!MemberId.TryParse(command.AccountId, out var memberId))
            return OperationResult<RevokeCapabilityResult>.Failure(MandateAdministratorGuards.AccountNotFound(command.AccountId));

        var scopeResult = MandateScopeParser.Parse(command.ScopeKind, command.OperationIds);
        if (scopeResult.IsFailure)
            return OperationResult<RevokeCapabilityResult>.Failure(scopeResult.Errors);

        var mandate = await _mandateRepository.GetByMemberIdAsync(memberId, cancellationToken);
        if (mandate is null)
            return OperationResult<RevokeCapabilityResult>.Failure(MandateAdministratorGuards.AccountNotFound(command.AccountId));

        var mutation = mandate.RevokeCapability(command.Capability, scopeResult.Value!);
        if (mutation.IsFailure)
            return OperationResult<RevokeCapabilityResult>.Failure(mutation.Errors);

        await _mandateRepository.SaveAsync(mandate, cancellationToken);

        var grantorMandate = mandate;
        var dependents = await _mandateReadRepository.ListGrantedByAsync(memberId, cancellationToken);
        foreach (var dependent in dependents)
        {
            var pruned = dependent.PruneToUmbrella(grantorMandate, grantorIsAdministrator: false);
            if (pruned > 0)
                await _mandateRepository.SaveAsync(dependent, cancellationToken);
        }

        return OperationResult<RevokeCapabilityResult>.Success(new RevokeCapabilityResult());
    }
}

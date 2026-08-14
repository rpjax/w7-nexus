using Aidan.Core.Patterns;
using Refactor.Nexus.Api.Accounts.Application.Ports.Out.Identity;
using Refactor.Nexus.Api.Authorization;
using Refactor.Nexus.Api.Mandates.Application.Authorization;
using Refactor.Nexus.Api.Mandates.Application.Ports.In.Administrator.Commands;
using Refactor.Nexus.Api.Mandates.Application.Ports.Out.Persistence;
using Refactor.Nexus.Api.Mandates.Application.UseCases.Shared;
using Refactor.Nexus.Api.Mandates.Domain.Aggregates;
using Refactor.Nexus.Api.Mandates.Domain.Aggregates.MemberMandate;
using Refactor.Nexus.Api.Mandates.Domain.Catalog;

namespace Refactor.Nexus.Api.Mandates.Application.UseCases.Administrator.Commands.RevokePreset;

public sealed record RevokePresetCommand(string AccountId, string PresetId);

public sealed class RevokePresetResult;

public sealed class RevokePresetHandler : IRevokePresetUseCase
{
    private readonly IRequestContext _requestContext;
    private readonly IMandateAccessPolicy _accessPolicy;
    private readonly IMemberMandateRepository _mandateRepository;
    private readonly IMemberMandateReadRepository _mandateReadRepository;

    public RevokePresetHandler(
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

    public async Task<IOperationResult<RevokePresetResult>> HandleAsync(
        RevokePresetCommand command,
        CancellationToken cancellationToken = default)
    {
        if (command is null)
            return OperationResult<RevokePresetResult>.Failure(MandateAdministratorGuards.RequestBodyRequired());

        var access = await MandateAdministratorGuards.AuthorizeAdminAsync<RevokePresetResult>(
            _requestContext, _accessPolicy, cancellationToken);
        if (access is not null)
            return access;

        if (!MemberId.TryParse(command.AccountId, out var memberId))
            return OperationResult<RevokePresetResult>.Failure(MandateAdministratorGuards.AccountNotFound(command.AccountId));

        var mandate = await _mandateRepository.GetByMemberIdAsync(memberId, cancellationToken);
        if (mandate is null)
            return OperationResult<RevokePresetResult>.Failure(MandateAdministratorGuards.AccountNotFound(command.AccountId));

        var mutation = mandate.RevokePreset(command.PresetId);
        if (mutation.IsFailure)
            return OperationResult<RevokePresetResult>.Failure(mutation.Errors);

        await _mandateRepository.SaveAsync(mandate, cancellationToken);
        await CascadePruneAsync(memberId, cancellationToken);

        return OperationResult<RevokePresetResult>.Success(new RevokePresetResult());
    }

    private async Task CascadePruneAsync(MemberId grantorId, CancellationToken cancellationToken)
    {
        var grantorMandate = await _mandateReadRepository.GetByMemberIdAsync(grantorId, cancellationToken)
            ?? MemberMandate.Empty(grantorId);

        var dependents = await _mandateReadRepository.ListGrantedByAsync(grantorId, cancellationToken);
        foreach (var dependent in dependents)
        {
            var pruned = dependent.PruneToUmbrella(grantorMandate, grantorIsAdministrator: false);
            if (pruned > 0)
                await _mandateRepository.SaveAsync(dependent, cancellationToken);
        }
    }
}

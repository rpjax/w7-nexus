using Aidan.Core.Errors;
using Aidan.Core.Patterns;
using Refactor.Nexus.Api.Accounts.Application.Ports.Out.Identity;
using Refactor.Nexus.Api.Authorization;
using Refactor.Nexus.Api.Mandates.Application.Authorization;
using Refactor.Nexus.Api.Mandates.Application.Ports.In.Administrator.Commands;
using Refactor.Nexus.Api.Mandates.Application.Ports.Out.Identity;
using Refactor.Nexus.Api.Mandates.Application.Ports.Out.Persistence;
using Refactor.Nexus.Api.Mandates.Application.UseCases.Shared;
using Refactor.Nexus.Api.Mandates.Domain.Aggregates;
using Refactor.Nexus.Api.Mandates.Domain.Aggregates.MemberMandate;
using Refactor.Nexus.Api.Mandates.Domain.Catalog;
using Refactor.Nexus.Api.Mandates.Domain.Errors;

namespace Refactor.Nexus.Api.Mandates.Application.UseCases.Administrator.Commands.GrantPreset;

public sealed record GrantPresetCommand(string AccountId, string PresetId);

public sealed class GrantPresetResult;

public sealed class GrantPresetHandler : IGrantPresetUseCase
{
    private readonly IRequestContext _requestContext;
    private readonly IMandateAccessPolicy _accessPolicy;
    private readonly IAccountDirectory _accountDirectory;
    private readonly IMemberMandateRepository _mandateRepository;
    private readonly IMemberMandateReadRepository _mandateReadRepository;
    private readonly IAgencyDealReadRepository _agencyDealReadRepository;

    public GrantPresetHandler(
        IRequestContext requestContext,
        IMandateAccessPolicy accessPolicy,
        IAccountDirectory accountDirectory,
        IMemberMandateRepository mandateRepository,
        IMemberMandateReadRepository mandateReadRepository,
        IAgencyDealReadRepository agencyDealReadRepository)
    {
        _requestContext = requestContext;
        _accessPolicy = accessPolicy;
        _accountDirectory = accountDirectory;
        _mandateRepository = mandateRepository;
        _mandateReadRepository = mandateReadRepository;
        _agencyDealReadRepository = agencyDealReadRepository;
    }

    public async Task<IOperationResult<GrantPresetResult>> HandleAsync(
        GrantPresetCommand command,
        CancellationToken cancellationToken = default)
    {
        if (command is null)
            return OperationResult<GrantPresetResult>.Failure(MandateAdministratorGuards.RequestBodyRequired());

        var (requester, failure) = await MandateAdministratorGuards.AuthorizeAdminWithRequesterAsync<GrantPresetResult>(
            _requestContext, _accessPolicy, cancellationToken);
        if (failure is not null)
            return failure;

        if (!MemberId.TryParse(command.AccountId, out var memberId))
            return OperationResult<GrantPresetResult>.Failure(MandateAdministratorGuards.AccountNotFound(command.AccountId));

        if (!await _accountDirectory.ExistsAsync(memberId, cancellationToken))
            return OperationResult<GrantPresetResult>.Failure(MandateAdministratorGuards.AccountNotFound(command.AccountId));

        if (PresetIds.IsKnown(command.PresetId)
            && string.Equals(PresetIds.Normalize(command.PresetId), PresetIds.Operator, StringComparison.OrdinalIgnoreCase)
            && !await _agencyDealReadRepository.HasActiveDealForOperatorAsync(memberId, cancellationToken))
        {
            return OperationResult<GrantPresetResult>.Failure(Error.Create()
                .WithCode(MandateErrorCodes.OperatorRequiresDeal)
                .WithMessage("Preset Operator exige um AgencyDeal ativo (Recrutador-raiz Admin com pct=0 e valido).")
                .Build());
        }

        var grantorId = new MemberId(Guid.Parse(requester!.AccountId));
        var grantorIsAdmin = await _accountDirectory.IsAdministratorAsync(grantorId, cancellationToken);
        var grantorMandate = grantorIsAdmin
            ? null
            : await _mandateReadRepository.GetByMemberIdAsync(grantorId, cancellationToken)
              ?? MemberMandate.Empty(grantorId);

        var mandate = await _mandateRepository.GetByMemberIdAsync(memberId, cancellationToken)
            ?? MemberMandate.Empty(memberId);

        var mutation = mandate.GrantPreset(command.PresetId, grantorId, grantorIsAdmin, grantorMandate);
        if (mutation.IsFailure)
            return OperationResult<GrantPresetResult>.Failure(mutation.Errors);

        await _mandateRepository.SaveAsync(mandate, cancellationToken);
        return OperationResult<GrantPresetResult>.Success(new GrantPresetResult());
    }
}

using Aidan.Core.Errors;
using Aidan.Core.Patterns;
using Refactor.Nexus.Api.Accounts.Application.Ports.Out.Identity;
using Refactor.Nexus.Api.Mandates.Application.Authorization;
using Refactor.Nexus.Api.Mandates.Application.Ports.Out.Identity;
using Refactor.Nexus.Api.Mandates.Application.Ports.Out.Persistence;
using Refactor.Nexus.Api.Mandates.Application.UseCases.Shared;
using Refactor.Nexus.Api.Mandates.Domain.Aggregates;
using Refactor.Nexus.Api.Mandates.Domain.Catalog;
using Refactor.Nexus.Api.Mandates.Domain.Errors;
using AgencyDealAggregate = Refactor.Nexus.Api.Mandates.Domain.Aggregates.AgencyDeal.AgencyDeal;

namespace Refactor.Nexus.Api.Mandates.Application.UseCases.Administrator.Commands.UpsertAgencyDeal;

public sealed record UpsertAgencyDealCommand(
    string RecruiterAccountId,
    string OperatorAccountId,
    decimal OperatorPercent,
    decimal RecruiterPercent);

public sealed record UpsertAgencyDealResult(Guid DealId);

public interface IUpsertAgencyDealUseCase
{
    Task<IOperationResult<UpsertAgencyDealResult>> HandleAsync(
        UpsertAgencyDealCommand command,
        CancellationToken cancellationToken = default);
}

public sealed class UpsertAgencyDealHandler : IUpsertAgencyDealUseCase
{
    private readonly IRequestContext _requestContext;
    private readonly IMandateAccessPolicy _accessPolicy;
    private readonly IAccountDirectory _accountDirectory;
    private readonly IAgencyDealRepository _dealRepository;
    private readonly IMemberMandateReadRepository _mandateReadRepository;

    public UpsertAgencyDealHandler(
        IRequestContext requestContext,
        IMandateAccessPolicy accessPolicy,
        IAccountDirectory accountDirectory,
        IAgencyDealRepository dealRepository,
        IMemberMandateReadRepository mandateReadRepository)
    {
        _requestContext = requestContext;
        _accessPolicy = accessPolicy;
        _accountDirectory = accountDirectory;
        _dealRepository = dealRepository;
        _mandateReadRepository = mandateReadRepository;
    }

    public async Task<IOperationResult<UpsertAgencyDealResult>> HandleAsync(
        UpsertAgencyDealCommand command,
        CancellationToken cancellationToken = default)
    {
        if (command is null)
            return OperationResult<UpsertAgencyDealResult>.Failure(MandateAdministratorGuards.RequestBodyRequired());

        var access = await MandateAdministratorGuards.AuthorizeAdminAsync<UpsertAgencyDealResult>(
            _requestContext, _accessPolicy, cancellationToken);
        if (access is not null)
            return access;

        if (!MemberId.TryParse(command.RecruiterAccountId, out var recruiterId))
            return OperationResult<UpsertAgencyDealResult>.Failure(MandateAdministratorGuards.AccountNotFound(command.RecruiterAccountId));
        if (!MemberId.TryParse(command.OperatorAccountId, out var operatorId))
            return OperationResult<UpsertAgencyDealResult>.Failure(MandateAdministratorGuards.AccountNotFound(command.OperatorAccountId));

        if (!await _accountDirectory.ExistsAsync(recruiterId, cancellationToken))
            return OperationResult<UpsertAgencyDealResult>.Failure(MandateAdministratorGuards.AccountNotFound(command.RecruiterAccountId));
        if (!await _accountDirectory.ExistsAsync(operatorId, cancellationToken))
            return OperationResult<UpsertAgencyDealResult>.Failure(MandateAdministratorGuards.AccountNotFound(command.OperatorAccountId));

        var recruiterIsAdmin = await _accountDirectory.IsAdministratorAsync(recruiterId, cancellationToken);
        if (command.RecruiterPercent == 0m && !recruiterIsAdmin)
        {
            return OperationResult<UpsertAgencyDealResult>.Failure(Error.Create()
                .WithCode(MandateErrorCodes.DealRootRequiresAdmin)
                .WithMessage("Recrutador-raiz (recrutador_pct = 0) deve ser uma conta Admin.")
                .Build());
        }

        if (!recruiterIsAdmin && command.RecruiterPercent > 0m)
        {
            var recruiterMandate = await _mandateReadRepository.GetByMemberIdAsync(recruiterId, cancellationToken);
            var canRecruit = recruiterMandate?.HasCapability(Capabilities.Recrutar, Domain.ValueObjects.MandateScope.CarteiraDirect()) == true
                || recruiterMandate?.AppliedPresets.Contains(PresetIds.Recruiter) == true;
            if (!canRecruit)
            {
                return OperationResult<UpsertAgencyDealResult>.Failure(Error.Create()
                    .WithCode(MandateErrorCodes.DealRecruiterLacksCapability)
                    .WithMessage("Recrutador precisa do preset Recruiter (ou capacidade recrutar).")
                    .Build());
            }
        }

        var existing = await _dealRepository.GetActiveByOperatorIdAsync(operatorId, cancellationToken);
        if (existing is null)
        {
            var created = AgencyDealAggregate.Open(recruiterId, operatorId, command.OperatorPercent, command.RecruiterPercent);
            if (created.IsFailure)
                return OperationResult<UpsertAgencyDealResult>.Failure(created.Errors);

            await _dealRepository.SaveAsync(created.Value!, cancellationToken);
            return OperationResult<UpsertAgencyDealResult>.Success(new UpsertAgencyDealResult(created.Value!.Id));
        }

        var updated = existing.UpdatePercents(command.OperatorPercent, command.RecruiterPercent, recruiterId);
        if (updated.IsFailure)
            return OperationResult<UpsertAgencyDealResult>.Failure(updated.Errors);

        await _dealRepository.SaveAsync(existing, cancellationToken);
        return OperationResult<UpsertAgencyDealResult>.Success(new UpsertAgencyDealResult(existing.Id));
    }
}

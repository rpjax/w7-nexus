using Aidan.Core.Errors;
using Aidan.Core.Patterns;
using Refactor.Nexus.Api.Accounts.Application.Ports.Out.Identity;
using Refactor.Nexus.Api.Mandates.Application.Authorization;
using Refactor.Nexus.Api.Mandates.Application.Ports.Out.Persistence;
using Refactor.Nexus.Api.Mandates.Application.UseCases.Shared;
using Refactor.Nexus.Api.Mandates.Domain.Aggregates;
using Refactor.Nexus.Api.Mandates.Domain.Catalog;
using Refactor.Nexus.Api.Mandates.Domain.Errors;

namespace Refactor.Nexus.Api.Mandates.Application.UseCases.Administrator.Commands.CloseAgencyDeal;

public sealed record CloseAgencyDealCommand(string OperatorAccountId);

public sealed class CloseAgencyDealResult;

public interface ICloseAgencyDealUseCase
{
    Task<IOperationResult<CloseAgencyDealResult>> HandleAsync(
        CloseAgencyDealCommand command,
        CancellationToken cancellationToken = default);
}

public sealed class CloseAgencyDealHandler : ICloseAgencyDealUseCase
{
    private readonly IRequestContext _requestContext;
    private readonly IMandateAccessPolicy _accessPolicy;
    private readonly IAgencyDealRepository _dealRepository;
    private readonly IMemberMandateReadRepository _mandateReadRepository;

    public CloseAgencyDealHandler(
        IRequestContext requestContext,
        IMandateAccessPolicy accessPolicy,
        IAgencyDealRepository dealRepository,
        IMemberMandateReadRepository mandateReadRepository)
    {
        _requestContext = requestContext;
        _accessPolicy = accessPolicy;
        _dealRepository = dealRepository;
        _mandateReadRepository = mandateReadRepository;
    }

    public async Task<IOperationResult<CloseAgencyDealResult>> HandleAsync(
        CloseAgencyDealCommand command,
        CancellationToken cancellationToken = default)
    {
        if (command is null)
            return OperationResult<CloseAgencyDealResult>.Failure(MandateAdministratorGuards.RequestBodyRequired());

        var access = await MandateAdministratorGuards.AuthorizeAdminAsync<CloseAgencyDealResult>(
            _requestContext, _accessPolicy, cancellationToken);
        if (access is not null)
            return access;

        if (!MemberId.TryParse(command.OperatorAccountId, out var operatorId))
            return OperationResult<CloseAgencyDealResult>.Failure(MandateAdministratorGuards.AccountNotFound(command.OperatorAccountId));

        var deal = await _dealRepository.GetActiveByOperatorIdAsync(operatorId, cancellationToken);
        if (deal is null)
        {
            return OperationResult<CloseAgencyDealResult>.Failure(Error.Create()
                .WithCode(MandateErrorCodes.DealNotFound)
                .WithMessage("Nao ha deal ativo para este Operador.")
                .Build());
        }

        var mandate = await _mandateReadRepository.GetByMemberIdAsync(operatorId, cancellationToken);
        if (mandate?.AppliedPresets.Contains(PresetIds.Operator) == true)
        {
            return OperationResult<CloseAgencyDealResult>.Failure(Error.Create()
                .WithCode(MandateErrorCodes.DealCannotCloseWhileOperatorPreset)
                .WithMessage("Revogue o preset Operator antes de encerrar o deal.")
                .Build());
        }

        var closed = deal.Close();
        if (closed.IsFailure)
            return OperationResult<CloseAgencyDealResult>.Failure(closed.Errors);

        await _dealRepository.SaveAsync(deal, cancellationToken);
        return OperationResult<CloseAgencyDealResult>.Success(new CloseAgencyDealResult());
    }
}

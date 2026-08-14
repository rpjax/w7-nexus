using Aidan.Core.Errors;
using Aidan.Core.Patterns;
using Refactor.Nexus.Api.Accounts.Application.Ports.Out.Identity;
using Refactor.Nexus.Api.Mandates.Application.Authorization;
using Refactor.Nexus.Api.Mandates.Application.Ports.Out.Persistence;
using Refactor.Nexus.Api.Mandates.Application.UseCases.Shared;
using Refactor.Nexus.Api.Mandates.Domain.Aggregates;
using Refactor.Nexus.Api.Mandates.Domain.Errors;

namespace Refactor.Nexus.Api.Mandates.Application.UseCases.Administrator.Commands.RemoveShareholderStake;

public sealed record RemoveShareholderStakeCommand(string AccountId);

public sealed class RemoveShareholderStakeResult;

public interface IRemoveShareholderStakeUseCase
{
    Task<IOperationResult<RemoveShareholderStakeResult>> HandleAsync(
        RemoveShareholderStakeCommand command,
        CancellationToken cancellationToken = default);
}

public sealed class RemoveShareholderStakeHandler : IRemoveShareholderStakeUseCase
{
    private readonly IRequestContext _requestContext;
    private readonly IMandateAccessPolicy _accessPolicy;
    private readonly IShareholderStakeRepository _stakeRepository;

    public RemoveShareholderStakeHandler(
        IRequestContext requestContext,
        IMandateAccessPolicy accessPolicy,
        IShareholderStakeRepository stakeRepository)
    {
        _requestContext = requestContext;
        _accessPolicy = accessPolicy;
        _stakeRepository = stakeRepository;
    }

    public async Task<IOperationResult<RemoveShareholderStakeResult>> HandleAsync(
        RemoveShareholderStakeCommand command,
        CancellationToken cancellationToken = default)
    {
        if (command is null)
            return OperationResult<RemoveShareholderStakeResult>.Failure(MandateAdministratorGuards.RequestBodyRequired());

        var access = await MandateAdministratorGuards.AuthorizeAdminAsync<RemoveShareholderStakeResult>(
            _requestContext, _accessPolicy, cancellationToken);
        if (access is not null)
            return access;

        if (!MemberId.TryParse(command.AccountId, out var accountId))
            return OperationResult<RemoveShareholderStakeResult>.Failure(MandateAdministratorGuards.AccountNotFound(command.AccountId));

        var existing = await _stakeRepository.GetByAccountIdAsync(accountId, cancellationToken);
        if (existing is null)
        {
            return OperationResult<RemoveShareholderStakeResult>.Failure(Error.Create()
                .WithCode(MandateErrorCodes.StakeNotFound)
                .WithMessage("Participacao de Acionista nao encontrada.")
                .Build());
        }

        await _stakeRepository.DeleteAsync(accountId, cancellationToken);
        return OperationResult<RemoveShareholderStakeResult>.Success(new RemoveShareholderStakeResult());
    }
}

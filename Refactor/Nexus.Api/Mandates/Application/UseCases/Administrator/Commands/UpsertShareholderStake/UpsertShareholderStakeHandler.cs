using Aidan.Core.Patterns;
using Refactor.Nexus.Api.Accounts.Application.Ports.Out.Identity;
using Refactor.Nexus.Api.Mandates.Application.Authorization;
using Refactor.Nexus.Api.Mandates.Application.Ports.Out.Identity;
using Refactor.Nexus.Api.Mandates.Application.Ports.Out.Persistence;
using Refactor.Nexus.Api.Mandates.Application.UseCases.Shared;
using Refactor.Nexus.Api.Mandates.Domain.Aggregates;
using ShareholderStakeAggregate = Refactor.Nexus.Api.Mandates.Domain.Aggregates.ShareholderStake.ShareholderStake;

namespace Refactor.Nexus.Api.Mandates.Application.UseCases.Administrator.Commands.UpsertShareholderStake;

public sealed record UpsertShareholderStakeCommand(string AccountId, decimal Percentage);

public sealed record UpsertShareholderStakeResult(string AccountId, decimal Percentage);

public interface IUpsertShareholderStakeUseCase
{
    Task<IOperationResult<UpsertShareholderStakeResult>> HandleAsync(
        UpsertShareholderStakeCommand command,
        CancellationToken cancellationToken = default);
}

public sealed class UpsertShareholderStakeHandler : IUpsertShareholderStakeUseCase
{
    private readonly IRequestContext _requestContext;
    private readonly IMandateAccessPolicy _accessPolicy;
    private readonly IAccountDirectory _accountDirectory;
    private readonly IShareholderStakeRepository _stakeRepository;
    private readonly IShareholderStakeReadRepository _stakeReadRepository;

    public UpsertShareholderStakeHandler(
        IRequestContext requestContext,
        IMandateAccessPolicy accessPolicy,
        IAccountDirectory accountDirectory,
        IShareholderStakeRepository stakeRepository,
        IShareholderStakeReadRepository stakeReadRepository)
    {
        _requestContext = requestContext;
        _accessPolicy = accessPolicy;
        _accountDirectory = accountDirectory;
        _stakeRepository = stakeRepository;
        _stakeReadRepository = stakeReadRepository;
    }

    public async Task<IOperationResult<UpsertShareholderStakeResult>> HandleAsync(
        UpsertShareholderStakeCommand command,
        CancellationToken cancellationToken = default)
    {
        if (command is null)
            return OperationResult<UpsertShareholderStakeResult>.Failure(MandateAdministratorGuards.RequestBodyRequired());

        var access = await MandateAdministratorGuards.AuthorizeAdminAsync<UpsertShareholderStakeResult>(
            _requestContext, _accessPolicy, cancellationToken);
        if (access is not null)
            return access;

        if (!MemberId.TryParse(command.AccountId, out var accountId))
            return OperationResult<UpsertShareholderStakeResult>.Failure(MandateAdministratorGuards.AccountNotFound(command.AccountId));

        if (!await _accountDirectory.ExistsAsync(accountId, cancellationToken))
            return OperationResult<UpsertShareholderStakeResult>.Failure(MandateAdministratorGuards.AccountNotFound(command.AccountId));

        var existing = await _stakeRepository.GetByAccountIdAsync(accountId, cancellationToken);
        var currentTotal = await _stakeReadRepository.SumPercentagesAsync(cancellationToken);
        var previous = existing?.Percentage ?? 0m;
        var proposedTotal = currentTotal - previous + command.Percentage;

        var totalCheck = ShareholderStakeAggregate.EnsureTotalWithinHundred(proposedTotal);
        if (totalCheck.IsFailure)
            return OperationResult<UpsertShareholderStakeResult>.Failure(totalCheck.Errors);

        if (existing is null)
        {
            var created = ShareholderStakeAggregate.Open(accountId, command.Percentage);
            if (created.IsFailure)
                return OperationResult<UpsertShareholderStakeResult>.Failure(created.Errors);

            await _stakeRepository.SaveAsync(created.Value!, cancellationToken);
            return OperationResult<UpsertShareholderStakeResult>.Success(
                new UpsertShareholderStakeResult(accountId.ToString(), created.Value!.Percentage));
        }

        var updated = existing.UpdatePercentage(command.Percentage);
        if (updated.IsFailure)
            return OperationResult<UpsertShareholderStakeResult>.Failure(updated.Errors);

        await _stakeRepository.SaveAsync(existing, cancellationToken);
        return OperationResult<UpsertShareholderStakeResult>.Success(
            new UpsertShareholderStakeResult(accountId.ToString(), existing.Percentage));
    }
}

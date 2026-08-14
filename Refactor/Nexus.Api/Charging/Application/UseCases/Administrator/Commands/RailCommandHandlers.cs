using Aidan.Core.Errors;
using Aidan.Core.Patterns;
using Refactor.Nexus.Api.Accounts.Application.Ports.Out.Identity;
using Refactor.Nexus.Api.Charging.Application.Journal;
using Refactor.Nexus.Api.Journal.Services.Contracts;
using Refactor.Nexus.Api.Charging.Application.Ports.Out.Mandates;
using Refactor.Nexus.Api.Charging.Application.Ports.Out.Persistence;
using Refactor.Nexus.Api.Charging.Application.UseCases.Shared;
using Refactor.Nexus.Api.Charging.Domain.Errors;
using Refactor.Nexus.Api.WorldAccounts.Application.Ports.Out.Persistence;
using Refactor.Nexus.Api.WorldAccounts.Domain.Aggregates.WorldAccount;

namespace Refactor.Nexus.Api.Charging.Application.UseCases.Administrator.Commands;

public sealed record BindEmissionRailCommand(string OperationId, string RailId);
public sealed class BindEmissionRailResult;
public interface IBindEmissionRailUseCase
{
    Task<IOperationResult<BindEmissionRailResult>> HandleAsync(BindEmissionRailCommand command, CancellationToken cancellationToken = default);
}

public sealed class BindEmissionRailHandler : IBindEmissionRailUseCase
{
    private readonly IRequestContext _requestContext;
    private readonly IChargingMandateSnapshot _mandates;
    private readonly IWorldAccountRepository _accounts;
    private readonly IOperationEmissionSetRepository _sets;
    private readonly Ports.Out.Operations.IOperationChargingDirectory _operations;
    private readonly IJournalWriter _journal;

    public BindEmissionRailHandler(
        IRequestContext requestContext,
        IChargingMandateSnapshot mandates,
        IWorldAccountRepository accounts,
        IOperationEmissionSetRepository sets,
        Ports.Out.Operations.IOperationChargingDirectory operations,
        IJournalWriter journal)
    {
        _requestContext = requestContext;
        _mandates = mandates;
        _accounts = accounts;
        _sets = sets;
        _operations = operations;
        _journal = journal;
    }

    public async Task<IOperationResult<BindEmissionRailResult>> HandleAsync(
        BindEmissionRailCommand command,
        CancellationToken cancellationToken = default)
    {
        var access = await ChargingGuards.AuthorizeRailsAsync<BindEmissionRailResult>(_requestContext, _mandates, cancellationToken);
        if (access.Failure is not null)
            return access.Failure;

        if (!Guid.TryParse(command.OperationId, out var operationId) || !Guid.TryParse(command.RailId, out var accountId))
        {
            return OperationResult<BindEmissionRailResult>.Failure(Error.Create()
                .WithCode(ChargingErrorCodes.OperationNotFound)
                .WithMessage("Operacao ou Conta invalida.")
                .Build());
        }

        if (await _operations.GetAsync(operationId, Guid.Empty, cancellationToken) is null)
        {
            return OperationResult<BindEmissionRailResult>.Failure(Error.Create()
                .WithCode(ChargingErrorCodes.OperationNotFound)
                .WithMessage("Operacao nao encontrada.")
                .Build());
        }

        var account = await _accounts.GetByIdAsync(accountId, cancellationToken);
        if (account is null || account.Kind != WorldAccountKind.Gateway)
        {
            return OperationResult<BindEmissionRailResult>.Failure(Error.Create()
                .WithCode(ChargingErrorCodes.RailNotFound)
                .WithMessage("Conta de Gateway nao encontrada.")
                .Build());
        }

        await _sets.BindAsync(operationId, accountId, cancellationToken);
        _journal.RecordRailBound(operationId, Guid.Parse(access.Requester!.AccountId));
        return OperationResult<BindEmissionRailResult>.Success(new BindEmissionRailResult());
    }
}

public sealed record UnbindEmissionRailCommand(string OperationId, string RailId);
public sealed class UnbindEmissionRailResult;
public interface IUnbindEmissionRailUseCase
{
    Task<IOperationResult<UnbindEmissionRailResult>> HandleAsync(UnbindEmissionRailCommand command, CancellationToken cancellationToken = default);
}

public sealed class UnbindEmissionRailHandler : IUnbindEmissionRailUseCase
{
    private readonly IRequestContext _requestContext;
    private readonly IChargingMandateSnapshot _mandates;
    private readonly IOperationEmissionSetRepository _sets;
    private readonly IJournalWriter _journal;

    public UnbindEmissionRailHandler(
        IRequestContext requestContext,
        IChargingMandateSnapshot mandates,
        IOperationEmissionSetRepository sets,
        IJournalWriter journal)
    {
        _requestContext = requestContext;
        _mandates = mandates;
        _sets = sets;
        _journal = journal;
    }

    public async Task<IOperationResult<UnbindEmissionRailResult>> HandleAsync(
        UnbindEmissionRailCommand command,
        CancellationToken cancellationToken = default)
    {
        var access = await ChargingGuards.AuthorizeRailsAsync<UnbindEmissionRailResult>(_requestContext, _mandates, cancellationToken);
        if (access.Failure is not null)
            return access.Failure;

        if (!Guid.TryParse(command.OperationId, out var operationId) || !Guid.TryParse(command.RailId, out var accountId))
            return OperationResult<UnbindEmissionRailResult>.Failure(ChargingGuards.BodyRequired());

        await _sets.UnbindAsync(operationId, accountId, cancellationToken);
        _journal.RecordRailUnbound(operationId, Guid.Parse(access.Requester!.AccountId));
        return OperationResult<UnbindEmissionRailResult>.Success(new UnbindEmissionRailResult());
    }
}

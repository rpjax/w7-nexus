using Aidan.Core.Errors;
using Aidan.Core.Patterns;
using Refactor.Nexus.Api.Accounts.Application.Ports.Out.Identity;
using Refactor.Nexus.Api.Charging.Application.Journal;
using Refactor.Nexus.Api.Journal.Services.Contracts;
using Refactor.Nexus.Api.Authorization;
using Refactor.Nexus.Api.Charging.Application.Ports.Out.Issuing;
using Refactor.Nexus.Api.Charging.Application.Ports.Out.Mandates;
using Refactor.Nexus.Api.Charging.Application.Ports.Out.Operations;
using Refactor.Nexus.Api.Charging.Application.Ports.Out.Persistence;
using Refactor.Nexus.Api.Charging.Application.UseCases.Shared;
using Refactor.Nexus.Api.Charging.Domain.Errors;
using Refactor.Nexus.Api.Charging.Domain.Services;
using Refactor.Nexus.Api.WorldAccounts.Application.Ports.Out.Persistence;
using ChargeAggregate = Refactor.Nexus.Api.Charging.Domain.Aggregates.Charge.Charge;
using WorldAccountAggregate = Refactor.Nexus.Api.WorldAccounts.Domain.Aggregates.WorldAccount.WorldAccount;

namespace Refactor.Nexus.Api.Charging.Application.UseCases.Authenticated.Commands;

public sealed record CreateChargeCommand(
    string OperationId,
    decimal GrossAmount,
    string? Currency,
    string? EmissionRailId,
    string? OperatorMemberId);

public sealed record CreateChargeResult(Guid ChargeId, string Status, string? ExternalReference);

public interface ICreateChargeUseCase
{
    Task<IOperationResult<CreateChargeResult>> HandleAsync(CreateChargeCommand command, CancellationToken cancellationToken = default);
}

public sealed class CreateChargeHandler : ICreateChargeUseCase
{
    private readonly IRequestContext _requestContext;
    private readonly IChargingMandateSnapshot _mandates;
    private readonly IOperationChargingDirectory _operations;
    private readonly IWorldAccountRepository _accounts;
    private readonly IOperationEmissionSetRepository _sets;
    private readonly IChargeRepository _charges;
    private readonly IPaymentIssuer _issuer;
    private readonly IJournalWriter _journal;

    public CreateChargeHandler(
        IRequestContext requestContext,
        IChargingMandateSnapshot mandates,
        IOperationChargingDirectory operations,
        IWorldAccountRepository accounts,
        IOperationEmissionSetRepository sets,
        IChargeRepository charges,
        IPaymentIssuer issuer,
        IJournalWriter journal)
    {
        _requestContext = requestContext;
        _mandates = mandates;
        _operations = operations;
        _accounts = accounts;
        _sets = sets;
        _charges = charges;
        _issuer = issuer;
        _journal = journal;
    }

    public async Task<IOperationResult<CreateChargeResult>> HandleAsync(
        CreateChargeCommand command,
        CancellationToken cancellationToken = default)
    {
        if (command is null)
            return OperationResult<CreateChargeResult>.Failure(ChargingGuards.BodyRequired());

        var requesterResult = await _requestContext.GetCurrentAsync(cancellationToken);
        if (requesterResult.IsFailure || requesterResult.Value is not RequesterContext requester)
            return OperationResult<CreateChargeResult>.Failure(requesterResult.Errors);

        if (!Guid.TryParse(requester.AccountId, out var requesterId))
            return OperationResult<CreateChargeResult>.Failure(ChargingGuards.Unauthorized("Identidade invalida."));

        var isAdmin = await _mandates.IsAdministratorAsync(requesterId, cancellationToken)
            || requester.Roles.Contains(Roles.Administrator, StringComparer.OrdinalIgnoreCase);

        if (!Guid.TryParse(command.OperationId, out var operationId))
        {
            return OperationResult<CreateChargeResult>.Failure(Error.Create()
                .WithCode(ChargingErrorCodes.OperationNotFound)
                .WithMessage("Operacao invalida.")
                .Build());
        }

        Guid operatorId = requesterId;
        if (isAdmin)
        {
            if (string.IsNullOrWhiteSpace(command.OperatorMemberId))
            {
                return OperationResult<CreateChargeResult>.Failure(Error.Create()
                    .WithCode(ChargingErrorCodes.OperatorNotAssigned)
                    .WithMessage("Associe um operador à operação e escolha-o aqui.")
                    .Build());
            }

            if (!Guid.TryParse(command.OperatorMemberId, out operatorId))
            {
                return OperationResult<CreateChargeResult>.Failure(Error.Create()
                    .WithCode(ChargingErrorCodes.OperatorNotAssigned)
                    .WithMessage("Operador inválido.")
                    .Build());
            }
        }

        var operation = await _operations.GetAsync(operationId, operatorId, cancellationToken);
        if (operation is null)
        {
            return OperationResult<CreateChargeResult>.Failure(Error.Create()
                .WithCode(ChargingErrorCodes.OperationNotFound)
                .WithMessage("Operacao nao encontrada.")
                .Build());
        }

        if (!operation.AllowsNewCharging)
        {
            return OperationResult<CreateChargeResult>.Failure(Error.Create()
                .WithCode(ChargingErrorCodes.OperationNotActive)
                .WithMessage("Só operação Ativa aceita nova cobrança.")
                .Build());
        }

        if (!isAdmin && requesterId != operatorId)
            return OperationResult<CreateChargeResult>.Unauthorized(ChargingGuards.Unauthorized("Operador so emite as proprias Cobranças."));

        if (!operation.OperatorAssigned)
        {
            return OperationResult<CreateChargeResult>.Failure(Error.Create()
                .WithCode(ChargingErrorCodes.OperatorNotAssigned)
                .WithMessage("Esse operador não está associado a esta operação.")
                .Build());
        }

        var snapshot = await _mandates.CaptureAsync(operatorId, cancellationToken);
        if (snapshot is null)
        {
            return OperationResult<CreateChargeResult>.Failure(Error.Create()
                .WithCode(ChargingErrorCodes.DealRequired)
                .WithMessage("Operador precisa de AgencyDeal ativo.")
                .Build());
        }

        var currency = string.IsNullOrWhiteSpace(command.Currency) ? "BRL" : command.Currency.Trim().ToUpperInvariant();
        var boundIds = await _sets.ListRailIdsAsync(operationId, cancellationToken);
        var pool = new List<WorldAccountAggregate>();
        foreach (var accountId in boundIds)
        {
            var account = await _accounts.GetByIdAsync(accountId, cancellationToken);
            if (account is not null && account.CanEmit(currency, command.GrossAmount))
                pool.Add(account);
        }

        WorldAccountAggregate? selected = null;
        if (!string.IsNullOrWhiteSpace(command.EmissionRailId))
        {
            if (!Guid.TryParse(command.EmissionRailId, out var forcedId) || pool.All(r => r.Id != forcedId))
            {
                return OperationResult<CreateChargeResult>.Failure(Error.Create()
                    .WithCode(ChargingErrorCodes.RailNotInSet)
                    .WithMessage("Override so e permitido para Conta de Gateway no conjunto da Op com quota.")
                    .Build());
            }

            selected = pool.First(r => r.Id == forcedId);
        }
        else
        {
            selected = pool.OrderBy(r => r.Id).FirstOrDefault();
        }

        if (selected is null)
        {
            return OperationResult<CreateChargeResult>.Failure(Error.Create()
                .WithCode(ChargingErrorCodes.NoQuota)
                .WithMessage("Nenhuma Conta de Gateway com quota disponivel para esta Operacao.")
                .Build());
        }

        var intent = SplitIntentFactory.Create(
            selected.OrangeMemberId!.Value,
            selected.Level1CutPercent ?? 0,
            snapshot.Shareholders,
            operation.ManagementCutPercent,
            snapshot.Agency);
        if (intent.IsFailure)
            return OperationResult<CreateChargeResult>.Failure(intent.Errors);

        var opened = ChargeAggregate.Open(
            operationId,
            operatorId,
            command.GrossAmount,
            currency,
            selected.Id,
            selected.OrangeMemberId.Value,
            intent.Value!);
        if (opened.IsFailure)
            return OperationResult<CreateChargeResult>.Failure(opened.Errors);

        var charge = opened.Value!;
        var consumed = selected.ConsumeQuota(currency, command.GrossAmount, charge.Id);
        if (consumed.IsFailure)
            return OperationResult<CreateChargeResult>.Failure(consumed.Errors);

        var issued = await _issuer.IssueAsync(charge.Id, charge.GrossAmount, charge.Currency, cancellationToken);
        charge.AssignExternalReference(issued.ExternalReference);

        await _accounts.SaveAsync(selected, cancellationToken);
        await _charges.SaveAsync(charge, cancellationToken);
        _journal.RecordChargeCreated(charge.Id, Guid.Parse(requester.AccountId));

        return OperationResult<CreateChargeResult>.Success(
            new CreateChargeResult(charge.Id, charge.Status.ToString(), charge.ExternalReference));
    }
}

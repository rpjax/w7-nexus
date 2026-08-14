using Aidan.Core.Errors;
using Aidan.Core.Patterns;
using IResult = Aidan.Core.Patterns.IResult;
using Refactor.Nexus.Api.Accounts.Application.Ports.Out.Identity;
using Refactor.Nexus.Api.Charging.Application.Journal;
using Refactor.Nexus.Api.Journal.Services.Contracts;
using Refactor.Nexus.Api.Charging.Application.Ports.Out.Mandates;
using Refactor.Nexus.Api.Charging.Application.Ports.Out.Persistence;
using Refactor.Nexus.Api.Charging.Application.UseCases.Shared;
using Refactor.Nexus.Api.Charging.Domain.Aggregates.Charge;
using Refactor.Nexus.Api.Charging.Domain.Errors;
using ChargeAggregate = Refactor.Nexus.Api.Charging.Domain.Aggregates.Charge.Charge;

namespace Refactor.Nexus.Api.Charging.Application.UseCases.Administrator.Commands;

public sealed record TransitionChargeCommand(string ChargeId, string Target);
public sealed record TransitionChargeResult(Guid ChargeId, string Status);
public interface ITransitionChargeUseCase
{
    Task<IOperationResult<TransitionChargeResult>> HandleAsync(TransitionChargeCommand command, CancellationToken cancellationToken = default);
}

public sealed class TransitionChargeHandler : ITransitionChargeUseCase
{
    private readonly IRequestContext _requestContext;
    private readonly IChargingMandateSnapshot _mandates;
    private readonly IChargeRepository _charges;
    private readonly IJournalWriter _journal;

    public TransitionChargeHandler(IRequestContext requestContext, IChargingMandateSnapshot mandates, IChargeRepository charges, IJournalWriter journal)
    {
        _requestContext = requestContext;
        _mandates = mandates;
        _charges = charges;
        _journal = journal;
    }

    public async Task<IOperationResult<TransitionChargeResult>> HandleAsync(
        TransitionChargeCommand command,
        CancellationToken cancellationToken = default)
    {
        var access = await ChargingGuards.AuthorizeAdminAsync<TransitionChargeResult>(_requestContext, _mandates, cancellationToken);
        if (access.Failure is not null)
            return access.Failure;

        if (!Guid.TryParse(command.ChargeId, out var chargeId))
            return NotFound(command.ChargeId);

        var charge = await _charges.GetByIdAsync(chargeId, cancellationToken);
        if (charge is null)
            return NotFound(command.ChargeId);

        var result = Apply(charge, command.Target);
        if (result.IsFailure)
            return OperationResult<TransitionChargeResult>.Failure(result.Errors);

        await _charges.SaveAsync(charge, cancellationToken);
        ChargingJournal.TryRecordChargeTransitioned(_journal, charge.Id, Guid.Parse(access.Requester!.AccountId));
        return OperationResult<TransitionChargeResult>.Success(new TransitionChargeResult(charge.Id, charge.Status.ToString()));
    }

    private static IResult Apply(ChargeAggregate charge, string target)
    {
        if (Enum.TryParse<ChargeStatus>(target, ignoreCase: true, out var status))
        {
            return status switch
            {
                ChargeStatus.Paid => charge.MarkPaid(),
                ChargeStatus.Cancelled => charge.Cancel(),
                ChargeStatus.Expired => charge.Expire(),
                ChargeStatus.Failed => charge.Fail(),
                _ => Result.Failure(Error.Create()
                    .WithCode(ChargingErrorCodes.InvalidTransition)
                    .WithMessage($"Transicao '{target}' invalida.")
                    .Build())
            };
        }

        return Result.Failure(Error.Create()
            .WithCode(ChargingErrorCodes.InvalidTransition)
            .WithMessage($"Transicao '{target}' invalida.")
            .Build());
    }

    private static IOperationResult<TransitionChargeResult> NotFound(string id) =>
        OperationResult<TransitionChargeResult>.Failure(Error.Create()
            .WithCode(ChargingErrorCodes.ChargeNotFound)
            .WithMessage($"Cobrança '{id}' nao encontrada.")
            .Build());
}

public sealed record MarkChargePaidCommand(string? ChargeId, string? ExternalReference);
public sealed record MarkChargePaidResult(Guid ChargeId, string Status);
public interface IMarkChargePaidUseCase
{
    Task<IOperationResult<MarkChargePaidResult>> HandleAsync(MarkChargePaidCommand command, CancellationToken cancellationToken = default);
}

public sealed class MarkChargePaidHandler : IMarkChargePaidUseCase
{
    private readonly IRequestContext _requestContext;
    private readonly IChargeRepository _charges;
    private readonly IJournalWriter _journal;

    public MarkChargePaidHandler(IRequestContext requestContext, IChargeRepository charges, IJournalWriter journal)
    {
        _requestContext = requestContext;
        _charges = charges;
        _journal = journal;
    }

    public async Task<IOperationResult<MarkChargePaidResult>> HandleAsync(
        MarkChargePaidCommand command,
        CancellationToken cancellationToken = default)
    {
        ChargeAggregate? charge = null;
        if (!string.IsNullOrWhiteSpace(command.ChargeId) && Guid.TryParse(command.ChargeId, out var id))
            charge = await _charges.GetByIdAsync(id, cancellationToken);

        if (charge is null && !string.IsNullOrWhiteSpace(command.ExternalReference))
            charge = await _charges.GetByExternalReferenceAsync(command.ExternalReference, cancellationToken);

        if (charge is null)
        {
            return OperationResult<MarkChargePaidResult>.Failure(Error.Create()
                .WithCode(ChargingErrorCodes.ChargeNotFound)
                .WithMessage("Cobrança nao encontrada.")
                .Build());
        }

        var paid = charge.MarkPaid();
        if (paid.IsFailure)
            return OperationResult<MarkChargePaidResult>.Failure(paid.Errors);

        await _charges.SaveAsync(charge, cancellationToken);

        var actedBy = Guid.Empty;
        var requester = await _requestContext.GetCurrentAsync(cancellationToken);
        if (requester.IsSuccess
            && requester.Value is RequesterContext context
            && Guid.TryParse(context.AccountId, out var accountId))
        {
            actedBy = accountId;
        }

        ChargingJournal.TryRecordChargeTransitioned(_journal, charge.Id, actedBy);
        return OperationResult<MarkChargePaidResult>.Success(new MarkChargePaidResult(charge.Id, charge.Status.ToString()));
    }
}

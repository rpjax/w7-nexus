using System.Text.Json.Serialization;
using Aidan.Core.Errors;
using Aidan.Core.Patterns;
using Refactor.Nexus.Api.Accounts.Application.Ports.Out.Identity;
using Refactor.Nexus.Api.Charging.Application.Journal;
using Refactor.Nexus.Api.Journal.Services.Contracts;
using Refactor.Nexus.Api.Authorization;
using Refactor.Nexus.Api.Charging.Application.Ports.Out.Mandates;
using Refactor.Nexus.Api.Charging.Application.Ports.Out.Persistence;
using Refactor.Nexus.Api.Charging.Application.UseCases.Shared;
using Refactor.Nexus.Api.Charging.Domain.Errors;
using Refactor.Nexus.Api.Charging.Domain.ValueObjects;
using Refactor.Nexus.Api.WorldAccounts.Application.Ports.Out.Persistence;
using Refactor.Nexus.Api.WorldAccounts.Domain.Aggregates.WorldAccount;
using ChargeAggregate = Refactor.Nexus.Api.Charging.Domain.Aggregates.Charge.Charge;

namespace Refactor.Nexus.Api.Charging.Application.UseCases.Administrator.Queries;

public sealed record EmissionRailView(
    Guid RailId,
    Guid OrangeMemberId,
    decimal Level1CutPercent,
    string Currency,
    decimal QuotaRemaining,
    string Status);

public sealed record ListEmissionRailsResult(IReadOnlyList<EmissionRailView> Items);
public interface IListEmissionRailsUseCase
{
    Task<IOperationResult<ListEmissionRailsResult>> HandleAsync(CancellationToken cancellationToken = default);
}

public sealed class ListEmissionRailsHandler : IListEmissionRailsUseCase
{
    private readonly IRequestContext _requestContext;
    private readonly IChargingMandateSnapshot _mandates;
    private readonly IWorldAccountRepository _accounts;
    private readonly IJournalWriter _journal;

    public ListEmissionRailsHandler(
        IRequestContext requestContext,
        IChargingMandateSnapshot mandates,
        IWorldAccountRepository accounts,
        IJournalWriter journal)
    {
        _requestContext = requestContext;
        _mandates = mandates;
        _accounts = accounts;
        _journal = journal;
    }

    public async Task<IOperationResult<ListEmissionRailsResult>> HandleAsync(CancellationToken cancellationToken = default)
    {
        var access = await ChargingGuards.AuthorizeRailsAsync<ListEmissionRailsResult>(_requestContext, _mandates, cancellationToken);
        if (access.Failure is not null)
            return access.Failure;

        var items = await _accounts.ListAsync(cancellationToken);
        _journal.RecordRailsListed(Guid.Parse(access.Requester!.AccountId));
        return OperationResult<ListEmissionRailsResult>.Success(new ListEmissionRailsResult(
            items.Where(a => a.Kind == WorldAccountKind.Gateway).Select(ToView).ToList()));
    }

    internal static EmissionRailView ToView(WorldAccount account)
    {
        var currency = account.Quotas.Keys.OrderBy(k => k, StringComparer.OrdinalIgnoreCase).FirstOrDefault() ?? "BRL";
        return new EmissionRailView(
            account.Id,
            account.OrangeMemberId ?? Guid.Empty,
            account.Level1CutPercent ?? 0,
            currency,
            account.QuotaOf(currency),
            account.EmissionStatus.ToString());
    }
}

public sealed record ListOperationEmissionSetQuery(string OperationId);
public sealed record ListOperationEmissionSetResult(IReadOnlyList<Guid> RailIds);
public interface IListOperationEmissionSetUseCase
{
    Task<IOperationResult<ListOperationEmissionSetResult>> HandleAsync(ListOperationEmissionSetQuery query, CancellationToken cancellationToken = default);
}

public sealed class ListOperationEmissionSetHandler : IListOperationEmissionSetUseCase
{
    private readonly IRequestContext _requestContext;
    private readonly IChargingMandateSnapshot _mandates;
    private readonly IOperationEmissionSetRepository _sets;

    public ListOperationEmissionSetHandler(
        IRequestContext requestContext,
        IChargingMandateSnapshot mandates,
        IOperationEmissionSetRepository sets)
    {
        _requestContext = requestContext;
        _mandates = mandates;
        _sets = sets;
    }

    public async Task<IOperationResult<ListOperationEmissionSetResult>> HandleAsync(
        ListOperationEmissionSetQuery query,
        CancellationToken cancellationToken = default)
    {
        var access = await ChargingGuards.AuthorizeRailsAsync<ListOperationEmissionSetResult>(_requestContext, _mandates, cancellationToken);
        if (access.Failure is not null)
            return access.Failure;

        if (!Guid.TryParse(query.OperationId, out var operationId))
        {
            return OperationResult<ListOperationEmissionSetResult>.Failure(Error.Create()
                .WithCode(ChargingErrorCodes.OperationNotFound)
                .WithMessage("Operacao invalida.")
                .Build());
        }

        var ids = await _sets.ListRailIdsAsync(operationId, cancellationToken);
        return OperationResult<ListOperationEmissionSetResult>.Success(new ListOperationEmissionSetResult(ids));
    }
}

public sealed record ChargeView(
    Guid ChargeId,
    Guid OperationId,
    Guid OperatorMemberId,
    decimal GrossAmount,
    string Currency,
    Guid EmissionRailId,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] Guid? OrangeMemberId,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] SplitIntent? SplitIntent,
    string Status,
    string? ExternalReference,
    decimal? NetAmount,
    Guid? LandingWorldAccountId,
    DateTime OpenedAt);

public sealed record ListChargesQuery(string? OperationId, string? OperatorMemberId);
public sealed record ListChargesResult(IReadOnlyList<ChargeView> Items);
public interface IListChargesUseCase
{
    Task<IOperationResult<ListChargesResult>> HandleAsync(ListChargesQuery query, CancellationToken cancellationToken = default);
}

public sealed class ListChargesHandler : IListChargesUseCase
{
    private readonly IRequestContext _requestContext;
    private readonly IChargingMandateSnapshot _mandates;
    private readonly IChargeRepository _charges;

    public ListChargesHandler(IRequestContext requestContext, IChargingMandateSnapshot mandates, IChargeRepository charges)
    {
        _requestContext = requestContext;
        _mandates = mandates;
        _charges = charges;
    }

    public async Task<IOperationResult<ListChargesResult>> HandleAsync(
        ListChargesQuery query,
        CancellationToken cancellationToken = default)
    {
        var requesterResult = await _requestContext.GetCurrentAsync(cancellationToken);
        if (requesterResult.IsFailure || requesterResult.Value is not RequesterContext requester)
            return OperationResult<ListChargesResult>.Failure(requesterResult.Errors);

        if (!Guid.TryParse(requester.AccountId, out var requesterId))
            return OperationResult<ListChargesResult>.Failure(ChargingGuards.Unauthorized("Identidade invalida."));

        var isAdmin = await _mandates.IsAdministratorAsync(requesterId, cancellationToken)
            || requester.Roles.Contains(Roles.Administrator, StringComparer.OrdinalIgnoreCase);
        var includeSplit = isAdmin
            || await _mandates.CanSeeChargeSplitAsync(requesterId, cancellationToken)
            || await _mandates.CanManageOperationsAsync(requesterId, cancellationToken);

        Guid? operationId = Guid.TryParse(query.OperationId, out var op) ? op : null;
        Guid? operatorId = includeSplit && Guid.TryParse(query.OperatorMemberId, out var oid)
            ? oid
            : includeSplit ? null : requesterId;

        var items = await _charges.ListAsync(operationId, operatorId, cancellationToken);
        return OperationResult<ListChargesResult>.Success(new ListChargesResult(items.Select(c => ToView(c, includeSplit)).ToList()));
    }

    internal static ChargeView ToView(ChargeAggregate charge, bool includeSplit) =>
        new(
            charge.Id,
            charge.OperationId,
            charge.OperatorMemberId,
            charge.GrossAmount,
            charge.Currency,
            charge.EmissionRailId,
            includeSplit ? charge.OrangeMemberId : null,
            includeSplit ? charge.SplitIntent : null,
            charge.Status.ToString(),
            charge.ExternalReference,
            charge.NetAmount,
            charge.LandingWorldAccountId,
            charge.OpenedAt);
}

public sealed record GetChargeQuery(string ChargeId);
public interface IGetChargeUseCase
{
    Task<IOperationResult<ChargeView>> HandleAsync(GetChargeQuery query, CancellationToken cancellationToken = default);
}

public sealed class GetChargeHandler : IGetChargeUseCase
{
    private readonly IRequestContext _requestContext;
    private readonly IChargingMandateSnapshot _mandates;
    private readonly IChargeRepository _charges;

    public GetChargeHandler(IRequestContext requestContext, IChargingMandateSnapshot mandates, IChargeRepository charges)
    {
        _requestContext = requestContext;
        _mandates = mandates;
        _charges = charges;
    }

    public async Task<IOperationResult<ChargeView>> HandleAsync(GetChargeQuery query, CancellationToken cancellationToken = default)
    {
        var requesterResult = await _requestContext.GetCurrentAsync(cancellationToken);
        if (requesterResult.IsFailure || requesterResult.Value is not RequesterContext requester)
            return OperationResult<ChargeView>.Failure(requesterResult.Errors);

        if (!Guid.TryParse(query.ChargeId, out var chargeId))
        {
            return OperationResult<ChargeView>.Failure(Error.Create()
                .WithCode(ChargingErrorCodes.ChargeNotFound)
                .WithMessage("Cobrança nao encontrada.")
                .Build());
        }

        var charge = await _charges.GetByIdAsync(chargeId, cancellationToken);
        if (charge is null)
        {
            return OperationResult<ChargeView>.Failure(Error.Create()
                .WithCode(ChargingErrorCodes.ChargeNotFound)
                .WithMessage("Cobrança nao encontrada.")
                .Build());
        }

        if (!Guid.TryParse(requester.AccountId, out var requesterId))
            return OperationResult<ChargeView>.Failure(ChargingGuards.Unauthorized("Identidade invalida."));

        var isAdmin = await _mandates.IsAdministratorAsync(requesterId, cancellationToken)
            || requester.Roles.Contains(Roles.Administrator, StringComparer.OrdinalIgnoreCase);
        var includeSplit = isAdmin
            || await _mandates.CanSeeChargeSplitAsync(requesterId, cancellationToken)
            || await _mandates.CanManageOperationsAsync(requesterId, cancellationToken);

        if (!includeSplit && charge.OperatorMemberId != requesterId)
            return OperationResult<ChargeView>.Unauthorized(ChargingGuards.Unauthorized("Operador so ve as proprias Cobranças."));

        return OperationResult<ChargeView>.Success(ListChargesHandler.ToView(charge, includeSplit));
    }
}

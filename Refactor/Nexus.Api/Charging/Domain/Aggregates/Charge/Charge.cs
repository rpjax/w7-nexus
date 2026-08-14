using System.Text.Json.Serialization;
using Aidan.Core.Errors;
using Aidan.Core.Patterns;
using IResult = Aidan.Core.Patterns.IResult;
using Refactor.Nexus.Api.Charging.Domain.Errors;
using Refactor.Nexus.Api.Charging.Domain.Events;
using Refactor.Nexus.Api.Charging.Domain.ValueObjects;

namespace Refactor.Nexus.Api.Charging.Domain.Aggregates.Charge;

public enum ChargeStatus
{
    Open = 0,
    Paid = 1,
    Materialized = 2,
    Expired = 3,
    Cancelled = 4,
    Failed = 5,
    Reversed = 6
}

public sealed class Charge
{
    private readonly List<object> _uncommitted = [];

    public Charge()
    {
    }

    public Guid Id { get; private set; }
    public Guid OperationId { get; private set; }
    public Guid OperatorMemberId { get; private set; }
    public decimal GrossAmount { get; private set; }
    public string Currency { get; private set; } = "BRL";
    public Guid EmissionRailId { get; private set; }
    public Guid OrangeMemberId { get; private set; }
    public SplitIntent SplitIntent { get; private set; } = new([]);
    public ChargeStatus Status { get; private set; }
    public string? ExternalReference { get; private set; }
    public decimal? NetAmount { get; private set; }
    public string? MaterializedCurrency { get; private set; }
    public Guid? LandingWorldAccountId { get; private set; }
    public DateTime OpenedAt { get; private set; }
    public DateTime LastUpdatedAt { get; private set; }

    [JsonIgnore]
    public IReadOnlyList<object> UncommittedEvents => _uncommitted;
    public bool IsTerminal => Status is ChargeStatus.Materialized or ChargeStatus.Expired or ChargeStatus.Cancelled or ChargeStatus.Failed or ChargeStatus.Reversed;
    public bool IsPaid => Status == ChargeStatus.Paid;
    public bool IsMaterialized => Status == ChargeStatus.Materialized;

    public static IResult<Charge> Open(
        Guid operationId,
        Guid operatorMemberId,
        decimal grossAmount,
        string currency,
        Guid emissionRailId,
        Guid orangeMemberId,
        SplitIntent splitIntent)
    {
        if (grossAmount <= 0)
        {
            return Result<Charge>.Failure(Error.Create()
                .WithCode(ChargingErrorCodes.InvalidAmount)
                .WithMessage("Valor bruto deve ser maior que zero.")
                .Build());
        }

        if (splitIntent.Lines.Count != 5)
        {
            return Result<Charge>.Failure(Error.Create()
                .WithCode(ChargingErrorCodes.InvalidCut)
                .WithMessage("Split intencao deve ter as 5 linhas do waterfall.")
                .Build());
        }

        var charge = new Charge();
        var now = DateTime.UtcNow;
        charge.ApplyChange(new ChargeOpened(
            Guid.NewGuid(),
            operationId,
            operatorMemberId,
            grossAmount,
            string.IsNullOrWhiteSpace(currency) ? "BRL" : currency.Trim().ToUpperInvariant(),
            emissionRailId,
            orangeMemberId,
            splitIntent,
            now));
        return Result<Charge>.Success(charge);
    }

    public IResult AssignExternalReference(string externalReference)
    {
        if (Status != ChargeStatus.Open)
            return TerminalFailure();

        ApplyChange(new ChargeExternalReferenceAssigned(Id, externalReference, DateTime.UtcNow));
        return Result.Success();
    }

    public IResult MarkPaid()
    {
        if (Status == ChargeStatus.Paid)
            return Result.Success();

        if (Status != ChargeStatus.Open)
            return TerminalFailure();

        ApplyChange(new ChargePaid(Id, DateTime.UtcNow));
        return Result.Success();
    }

    public IResult Cancel() => TransitionOpen(new ChargeCancelled(Id, DateTime.UtcNow));
    public IResult Expire() => TransitionOpen(new ChargeExpired(Id, DateTime.UtcNow));
    public IResult Fail() => TransitionOpen(new ChargeFailed(Id, DateTime.UtcNow));

    public IResult MarkMaterialized(decimal netAmount, string currency, Guid landingWorldAccountId)
    {
        var normalized = string.IsNullOrWhiteSpace(currency) ? "BRL" : currency.Trim().ToUpperInvariant();
        if (Status == ChargeStatus.Materialized)
        {
            if (NetAmount == netAmount
                && string.Equals(MaterializedCurrency, normalized, StringComparison.OrdinalIgnoreCase)
                && LandingWorldAccountId == landingWorldAccountId)
                return Result.Success();

            return Result.Failure(Error.Create()
                .WithCode(ChargingErrorCodes.AlreadyMaterialized)
                .WithMessage("Cobrança ja materializada com outro liquido ou Conta.")
                .Build());
        }

        if (Status != ChargeStatus.Paid)
        {
            return Result.Failure(Error.Create()
                .WithCode(ChargingErrorCodes.NotPaid)
                .WithMessage("So Cobrança Paga pode ser materializada.")
                .Build());
        }

        if (netAmount <= 0)
        {
            return Result.Failure(Error.Create()
                .WithCode(ChargingErrorCodes.InvalidAmount)
                .WithMessage("Liquido X deve ser maior que zero.")
                .Build());
        }

        if (landingWorldAccountId == Guid.Empty)
        {
            return Result.Failure(Error.Create()
                .WithCode(ChargingErrorCodes.InvalidAmount)
                .WithMessage("Conta de aterrissagem e obrigatoria.")
                .Build());
        }

        ApplyChange(new ChargeMaterialized(Id, netAmount, normalized, landingWorldAccountId, DateTime.UtcNow));
        return Result.Success();
    }

    public IResult MarkReversed()
    {
        if (Status == ChargeStatus.Reversed)
            return Result.Success();

        if (Status != ChargeStatus.Materialized && Status != ChargeStatus.Paid)
        {
            return Result.Failure(Error.Create()
                .WithCode(ChargingErrorCodes.NotPaid)
                .WithMessage("So Cobrança Paga ou Materializada pode ser estornada.")
                .Build());
        }

        ApplyChange(new ChargeReversed(Id, DateTime.UtcNow));
        return Result.Success();
    }

    public void ClearUncommitted() => _uncommitted.Clear();

    public void Apply(ChargeOpened e)
    {
        Id = e.ChargeId;
        OperationId = e.OperationId;
        OperatorMemberId = e.OperatorMemberId;
        GrossAmount = e.GrossAmount;
        Currency = e.Currency;
        EmissionRailId = e.EmissionRailId;
        OrangeMemberId = e.OrangeMemberId;
        SplitIntent = e.SplitIntent;
        Status = ChargeStatus.Open;
        OpenedAt = e.OccurredAt;
        LastUpdatedAt = e.OccurredAt;
    }

    public void Apply(ChargeExternalReferenceAssigned e)
    {
        ExternalReference = e.ExternalReference;
        LastUpdatedAt = e.OccurredAt;
    }

    public void Apply(ChargePaid e)
    {
        Status = ChargeStatus.Paid;
        LastUpdatedAt = e.OccurredAt;
    }

    public void Apply(ChargeCancelled e)
    {
        Status = ChargeStatus.Cancelled;
        LastUpdatedAt = e.OccurredAt;
    }

    public void Apply(ChargeExpired e)
    {
        Status = ChargeStatus.Expired;
        LastUpdatedAt = e.OccurredAt;
    }

    public void Apply(ChargeFailed e)
    {
        Status = ChargeStatus.Failed;
        LastUpdatedAt = e.OccurredAt;
    }

    public void Apply(ChargeMaterialized e)
    {
        Status = ChargeStatus.Materialized;
        NetAmount = e.NetAmount;
        MaterializedCurrency = e.Currency;
        LandingWorldAccountId = e.LandingWorldAccountId;
        LastUpdatedAt = e.OccurredAt;
    }

    public void Apply(ChargeReversed e)
    {
        Status = ChargeStatus.Reversed;
        LastUpdatedAt = e.OccurredAt;
    }

    private IResult TransitionOpen(object @event)
    {
        if (Status != ChargeStatus.Open)
            return TerminalFailure();

        ApplyChange(@event);
        return Result.Success();
    }

    private IResult TerminalFailure()
    {
        if (Status == ChargeStatus.Paid)
        {
            return Result.Failure(Error.Create()
                .WithCode(ChargingErrorCodes.AlreadyPaid)
                .WithMessage("Cobrança ja esta Paga.")
                .Build());
        }

        return Result.Failure(Error.Create()
            .WithCode(ChargingErrorCodes.Terminal)
            .WithMessage("Cobrança ja esta em estado terminal.")
            .Build());
    }

    private void ApplyChange(object @event)
    {
        switch (@event)
        {
            case ChargeOpened opened:
                Apply(opened);
                break;
            case ChargeExternalReferenceAssigned assigned:
                Apply(assigned);
                break;
            case ChargePaid paid:
                Apply(paid);
                break;
            case ChargeCancelled cancelled:
                Apply(cancelled);
                break;
            case ChargeExpired expired:
                Apply(expired);
                break;
            case ChargeFailed failed:
                Apply(failed);
                break;
            case ChargeMaterialized materialized:
                Apply(materialized);
                break;
            case ChargeReversed reversed:
                Apply(reversed);
                break;
            default:
                throw new InvalidOperationException($"Evento desconhecido: {@event.GetType().Name}");
        }

        _uncommitted.Add(@event);
    }
}

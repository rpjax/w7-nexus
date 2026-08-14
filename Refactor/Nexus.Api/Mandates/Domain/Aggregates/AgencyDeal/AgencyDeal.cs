using System.Text.Json.Serialization;
using Aidan.Core.Errors;
using Aidan.Core.Patterns;
using IResult = Aidan.Core.Patterns.IResult;
using Refactor.Nexus.Api.Mandates.Domain.Errors;
using Refactor.Nexus.Api.Mandates.Domain.Events;

namespace Refactor.Nexus.Api.Mandates.Domain.Aggregates.AgencyDeal;

public enum AgencyDealStatus { Active = 0, Closed = 1 }

public sealed class AgencyDeal
{
    private readonly List<object> _uncommitted = [];

    public AgencyDeal()
    {
    }

    public Guid Id { get; private set; }
    public MemberId RecruiterId { get; private set; }
    public MemberId OperatorId { get; private set; }
    public decimal OperatorPercent { get; private set; }
    public decimal RecruiterPercent { get; private set; }
    public AgencyDealStatus Status { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime LastUpdatedAt { get; private set; }
    public bool IsActive => Status == AgencyDealStatus.Active;

    [JsonIgnore]
    public IReadOnlyList<object> UncommittedEvents => _uncommitted;
    public void ClearUncommitted() => _uncommitted.Clear();

    public static IResult<AgencyDeal> Open(MemberId recruiterId, MemberId operatorId, decimal operatorPercent, decimal recruiterPercent)
    {
        var validation = ValidatePercents(operatorPercent, recruiterPercent);
        if (validation.IsFailure)
            return Result<AgencyDeal>.Failure(validation.Errors);
        if (recruiterId.Equals(operatorId))
            return Result<AgencyDeal>.Failure(Error.Create().WithCode(MandateErrorCodes.DealSameParties).WithMessage("Recrutador e Operador devem ser contas distintas.").Build());

        var deal = new AgencyDeal();
        var now = DateTime.UtcNow;
        deal.ApplyChange(new AgencyDealOpened(Guid.NewGuid(), recruiterId.Value, operatorId.Value, operatorPercent, recruiterPercent, now, null));
        return Result<AgencyDeal>.Success(deal);
    }

    public static AgencyDeal Rehydrate(Guid id, MemberId recruiterId, MemberId operatorId, decimal operatorPercent, decimal recruiterPercent, AgencyDealStatus status, DateTime createdAt, DateTime lastUpdatedAt)
    {
        var deal = new AgencyDeal();
        deal.Apply(new AgencyDealBackfilled(id, recruiterId.Value, operatorId.Value, operatorPercent, recruiterPercent, status.ToString(), createdAt, lastUpdatedAt));
        return deal;
    }

    public IResult UpdatePercents(decimal operatorPercent, decimal recruiterPercent, MemberId recruiterId)
    {
        if (!IsActive)
            return Result.Failure(Error.Create().WithCode(MandateErrorCodes.DealAlreadyClosed).WithMessage("Deal ja esta encerrado.").Build());
        var validation = ValidatePercents(operatorPercent, recruiterPercent);
        if (validation.IsFailure) return validation;
        if (recruiterId.Equals(OperatorId))
            return Result.Failure(Error.Create().WithCode(MandateErrorCodes.DealSameParties).WithMessage("Recrutador e Operador devem ser contas distintas.").Build());
        ApplyChange(new AgencyDealRatesChanged(Id, recruiterId.Value, operatorPercent, recruiterPercent, DateTime.UtcNow, null));
        return Result.Success();
    }

    public IResult Close()
    {
        if (!IsActive)
            return Result.Failure(Error.Create().WithCode(MandateErrorCodes.DealAlreadyClosed).WithMessage("Deal ja esta encerrado.").Build());
        ApplyChange(new AgencyDealClosed(Id, DateTime.UtcNow, null));
        return Result.Success();
    }

    public void Apply(AgencyDealOpened e)
    {
        Id = e.DealId;
        RecruiterId = new MemberId(e.RecruiterId);
        OperatorId = new MemberId(e.OperatorId);
        OperatorPercent = e.OperatorPercent;
        RecruiterPercent = e.RecruiterPercent;
        Status = AgencyDealStatus.Active;
        CreatedAt = e.OccurredAt;
        LastUpdatedAt = e.OccurredAt;
    }

    public void Apply(AgencyDealBackfilled e)
    {
        Id = e.DealId;
        RecruiterId = new MemberId(e.RecruiterId);
        OperatorId = new MemberId(e.OperatorId);
        OperatorPercent = e.OperatorPercent;
        RecruiterPercent = e.RecruiterPercent;
        Status = Enum.TryParse<AgencyDealStatus>(e.Status, true, out var s) ? s : AgencyDealStatus.Active;
        CreatedAt = e.CreatedAt;
        LastUpdatedAt = e.LastUpdatedAt;
    }

    public void Apply(AgencyDealRatesChanged e)
    {
        RecruiterId = new MemberId(e.RecruiterId);
        OperatorPercent = e.OperatorPercent;
        RecruiterPercent = e.RecruiterPercent;
        LastUpdatedAt = e.OccurredAt;
    }

    public void Apply(AgencyDealClosed e)
    {
        Status = AgencyDealStatus.Closed;
        LastUpdatedAt = e.OccurredAt;
    }

    public static IResult ValidatePercents(decimal operatorPercent, decimal recruiterPercent)
    {
        if (operatorPercent < 0 || recruiterPercent < 0 || operatorPercent > 100 || recruiterPercent > 100)
            return Result.Failure(Error.Create().WithCode(MandateErrorCodes.DealPercentsInvalid).WithMessage("Percentuais devem estar entre 0 e 100.").Build());
        if (operatorPercent + recruiterPercent > 100m)
            return Result.Failure(Error.Create().WithCode(MandateErrorCodes.DealPercentsInvalid).WithMessage("operador_pct + recrutador_pct deve ser <= 100.").Build());
        return Result.Success();
    }

    private void ApplyChange(object @event)
    {
        switch (@event)
        {
            case AgencyDealOpened e: Apply(e); break;
            case AgencyDealBackfilled e: Apply(e); break;
            case AgencyDealRatesChanged e: Apply(e); break;
            case AgencyDealClosed e: Apply(e); break;
            default: throw new InvalidOperationException(@event.GetType().Name);
        }
        _uncommitted.Add(@event);
    }
}

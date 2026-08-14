using System.Text.Json.Serialization;
using Aidan.Core.Errors;
using Aidan.Core.Patterns;
using IResult = Aidan.Core.Patterns.IResult;
using Refactor.Nexus.Api.Mandates.Domain.Errors;
using Refactor.Nexus.Api.Mandates.Domain.Events;

namespace Refactor.Nexus.Api.Mandates.Domain.Aggregates.ShareholderStake;

public sealed class ShareholderStake
{
    private readonly List<object> _uncommitted = [];

    public ShareholderStake()
    {
    }

    public MemberId AccountId { get; private set; }
    public Guid PersistenceId => AccountId.Value;
    public decimal Percentage { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime LastUpdatedAt { get; private set; }
    public bool IsRemoved { get; private set; }

    [JsonIgnore]
    public IReadOnlyList<object> UncommittedEvents => _uncommitted;
    public void ClearUncommitted() => _uncommitted.Clear();

    public static IResult<ShareholderStake> Create(MemberId accountId, decimal percentage)
    {
        var validation = ValidatePercentage(percentage);
        if (validation.IsFailure)
            return Result<ShareholderStake>.Failure(validation.Errors);
        var stake = new ShareholderStake();
        var now = DateTime.UtcNow;
        stake.ApplyChange(new ShareholderStakeSet(accountId.Value, percentage, now, null));
        return Result<ShareholderStake>.Success(stake);
    }

    public static ShareholderStake Rehydrate(MemberId accountId, decimal percentage, DateTime createdAt, DateTime lastUpdatedAt)
    {
        var stake = new ShareholderStake();
        stake.AccountId = accountId;
        stake.Percentage = percentage;
        stake.CreatedAt = createdAt;
        stake.LastUpdatedAt = lastUpdatedAt;
        return stake;
    }

    public IResult UpdatePercentage(decimal percentage)
    {
        var validation = ValidatePercentage(percentage);
        if (validation.IsFailure) return validation;
        ApplyChange(new ShareholderStakeSet(AccountId.Value, percentage, DateTime.UtcNow, null));
        return Result.Success();
    }

    public IResult Remove()
    {
        ApplyChange(new ShareholderStakeRemoved(AccountId.Value, DateTime.UtcNow, null));
        return Result.Success();
    }

    public void Apply(ShareholderStakeSet e)
    {
        AccountId = new MemberId(e.AccountId);
        Percentage = e.Percentage;
        IsRemoved = false;
        if (CreatedAt == default) CreatedAt = e.OccurredAt;
        LastUpdatedAt = e.OccurredAt;
    }

    public void Apply(ShareholderStakeRemoved e)
    {
        IsRemoved = true;
        LastUpdatedAt = e.OccurredAt;
    }

    public static IResult ValidatePercentage(decimal percentage)
    {
        if (percentage <= 0 || percentage > 100)
            return Result.Failure(Error.Create().WithCode(MandateErrorCodes.StakePercentageInvalid).WithMessage("Percentual de Acionista deve ser > 0 e <= 100.").Build());
        return Result.Success();
    }

    public static IResult EnsureTotalWithinHundred(decimal proposedTotal)
    {
        if (proposedTotal > 100m)
            return Result.Failure(Error.Create().WithCode(MandateErrorCodes.StakeTotalExceedsHundred).WithMessage("A soma das participacoes de Acionistas nao pode exceder 100%.").Build());
        return Result.Success();
    }

    private void ApplyChange(object @event)
    {
        switch (@event)
        {
            case ShareholderStakeSet e: Apply(e); break;
            case ShareholderStakeRemoved e: Apply(e); break;
            default: throw new InvalidOperationException(@event.GetType().Name);
        }
        _uncommitted.Add(@event);
    }
}

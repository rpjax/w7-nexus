using System.Text.Json.Serialization;
using Aidan.Core.Errors;
using Aidan.Core.Patterns;
using IResult = Aidan.Core.Patterns.IResult;
using Refactor.Nexus.Api.Ledger.Domain;
using Refactor.Nexus.Api.Ledger.Domain.Errors;
using Refactor.Nexus.Api.Ledger.Domain.Events;

namespace Refactor.Nexus.Api.Ledger.Domain.Aggregates.Claim;

public enum ClaimStatus
{
    Active = 0,
    Repassed = 1,
    Lost = 2,
    Reversed = 3,
    Archived = 4
}

public sealed class Claim
{
    public const string PathCutKind = "PathCut";

    private readonly List<object> _uncommitted = [];

    public Claim()
    {
    }

    public Guid Id { get; private set; }
    public Guid BeneficiaryId { get; private set; }
    public decimal Amount { get; private set; }
    public string Currency { get; private set; } = "BRL";
    public Guid OriginChargeId { get; private set; }
    public Guid LocationAccountId { get; private set; }
    public ClaimStatus Status { get; private set; }
    public string Kind { get; private set; } = "";
    public Guid? ParentClaimId { get; private set; }
    public DateTime OpenedAt { get; private set; }
    public decimal BirthAmount { get; private set; }
    public string BirthCurrency { get; private set; } = "BRL";
    public bool Visible { get; private set; }
    public decimal? ReleasedAmount { get; private set; }
    public string? ReleasedCurrency { get; private set; }
    public string? ReportSummary { get; private set; }
    public DateTime? RevealedAt { get; private set; }

    [JsonIgnore]
    public IReadOnlyList<object> UncommittedEvents => _uncommitted;

    public bool IsActive => Status == ClaimStatus.Active;

    public static IResult<Claim> Open(
        Guid beneficiaryId,
        decimal amount,
        string currency,
        Guid originChargeId,
        Guid locationAccountId,
        string kind,
        Guid? parentClaimId = null,
        decimal? birthAmount = null,
        string? birthCurrency = null)
    {
        if (amount <= 0)
        {
            return Result<Claim>.Failure(Error.Create()
                .WithCode(LedgerErrorCodes.InvalidAmount)
                .WithMessage("Claim deve ter valor maior que zero.")
                .Build());
        }

        var currencyNorm = currency.Trim().ToUpperInvariant();
        var claim = new Claim();
        claim.ApplyChange(new ClaimOpened(
            Guid.NewGuid(),
            beneficiaryId,
            amount,
            currencyNorm,
            originChargeId,
            locationAccountId,
            kind,
            DateTime.UtcNow,
            parentClaimId,
            birthAmount ?? amount,
            string.IsNullOrWhiteSpace(birthCurrency) ? currencyNorm : birthCurrency.Trim().ToUpperInvariant()));
        return Result<Claim>.Success(claim);
    }

    public IResult Adjust(decimal amount, string currency, Guid locationAccountId)
    {
        if (!IsActive)
            return Fail(LedgerErrorCodes.ClaimNotActive, "Claim nao esta ativo.");
        if (amount <= 0)
            return Archive();

        ApplyChange(new ClaimAdjusted(
            Id,
            amount,
            currency.Trim().ToUpperInvariant(),
            locationAccountId,
            DateTime.UtcNow));
        return Result.Success();
    }

    public IResult Archive()
    {
        if (Status == ClaimStatus.Archived)
            return Result.Success();
        if (!IsActive)
            return Fail(LedgerErrorCodes.ClaimNotActive, "Claim nao esta ativo.");

        ApplyChange(new ClaimArchived(Id, DateTime.UtcNow));
        return Result.Success();
    }

    public IResult Repass()
    {
        if (Status == ClaimStatus.Repassed)
            return Result.Success();
        if (!IsActive)
            return Fail(LedgerErrorCodes.ClaimNotActive, "Claim nao esta ativo.");

        ApplyChange(new ClaimRepassed(Id, DateTime.UtcNow));
        return Result.Success();
    }

    public IResult Reveal(decimal releasedAmount, string releasedCurrency, string summary)
    {
        if (string.IsNullOrWhiteSpace(summary))
            return Fail(LedgerErrorCodes.SummaryRequired, "Relatório controlado exige um texto.");

        var currencyNorm = releasedCurrency.Trim().ToUpperInvariant();
        var trimmed = summary.Trim();
        if (Visible
            && ReleasedAmount == releasedAmount
            && string.Equals(ReleasedCurrency, currencyNorm, StringComparison.OrdinalIgnoreCase)
            && string.Equals(ReportSummary, trimmed, StringComparison.Ordinal))
        {
            return Result.Success();
        }

        if (Visible)
            return Fail(LedgerErrorCodes.AlreadyRevealed, "Claim ja foi revelado.");

        ApplyChange(new ClaimRevealed(Id, releasedAmount, currencyNorm, trimmed, DateTime.UtcNow));
        return Result.Success();
    }

    public IResult WriteOff(string cause)
    {
        if (Status == ClaimStatus.Lost)
            return Result.Success();
        if (!IsActive)
            return Fail(LedgerErrorCodes.ClaimNotActive, "Claim nao esta ativo.");
        if (!AttritionCause.TryNormalize(cause, out var normalized))
            return Fail(LedgerErrorCodes.CauseRequired, "Causa de attrition invalida.");

        ApplyChange(new ClaimLost(Id, normalized, DateTime.UtcNow));
        return Result.Success();
    }

    public IResult Reverse(string cause)
    {
        if (Status == ClaimStatus.Reversed)
            return Result.Success();
        if (!IsActive)
            return Fail(LedgerErrorCodes.ClaimNotActive, "Claim nao esta ativo.");
        if (!AttritionCause.TryNormalize(cause, out var normalized))
            return Fail(LedgerErrorCodes.CauseRequired, "Causa de attrition invalida.");

        ApplyChange(new ClaimReversed(Id, normalized, DateTime.UtcNow));
        return Result.Success();
    }

    public void ClearUncommitted() => _uncommitted.Clear();

    public void Apply(ClaimOpened e)
    {
        Id = e.ClaimId;
        BeneficiaryId = e.BeneficiaryId;
        Amount = e.Amount;
        Currency = e.Currency;
        OriginChargeId = e.OriginChargeId;
        LocationAccountId = e.LocationAccountId;
        Kind = e.Kind;
        ParentClaimId = e.ParentClaimId;
        Status = ClaimStatus.Active;
        OpenedAt = e.OccurredAt;
        BirthAmount = e.BirthAmount ?? e.Amount;
        BirthCurrency = string.IsNullOrWhiteSpace(e.BirthCurrency) ? e.Currency : e.BirthCurrency;
    }

    public void Apply(ClaimAdjusted e)
    {
        Amount = e.Amount;
        Currency = e.Currency;
        LocationAccountId = e.LocationAccountId;
    }

    public void Apply(ClaimArchived e) => Status = ClaimStatus.Archived;

    public void Apply(ClaimRepassed e) => Status = ClaimStatus.Repassed;

    public void Apply(ClaimRevealed e)
    {
        Visible = true;
        ReleasedAmount = e.ReleasedAmount;
        ReleasedCurrency = e.ReleasedCurrency;
        ReportSummary = e.Summary;
        RevealedAt = e.OccurredAt;
    }

    public void Apply(ClaimLost e) => Status = ClaimStatus.Lost;

    public void Apply(ClaimReversed e) => Status = ClaimStatus.Reversed;

    private static IResult Fail(string code, string message) =>
        Result.Failure(Error.Create().WithCode(code).WithMessage(message).Build());

    private void ApplyChange(object @event)
    {
        switch (@event)
        {
            case ClaimOpened opened:
                Apply(opened);
                break;
            case ClaimAdjusted adjusted:
                Apply(adjusted);
                break;
            case ClaimArchived archived:
                Apply(archived);
                break;
            case ClaimRepassed repassed:
                Apply(repassed);
                break;
            case ClaimRevealed revealed:
                Apply(revealed);
                break;
            case ClaimLost lost:
                Apply(lost);
                break;
            case ClaimReversed reversed:
                Apply(reversed);
                break;
            default:
                throw new InvalidOperationException(@event.GetType().Name);
        }

        _uncommitted.Add(@event);
    }
}

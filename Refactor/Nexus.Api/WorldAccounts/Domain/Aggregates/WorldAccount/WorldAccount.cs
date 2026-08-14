using System.Text.Json.Serialization;
using Aidan.Core.Errors;
using Aidan.Core.Patterns;
using IResult = Aidan.Core.Patterns.IResult;
using Refactor.Nexus.Api.WorldAccounts.Domain.Errors;
using Refactor.Nexus.Api.WorldAccounts.Domain.Events;

namespace Refactor.Nexus.Api.WorldAccounts.Domain.Aggregates.WorldAccount;

public enum WorldAccountKind
{
    Gateway = 0,
    Bank = 1,
    Crypto = 2,
    Payout = 3
}

public enum EmissionStatus
{
    Ok = 0,
    Blocked = 1
}

public enum BalanceStatus
{
    Accessible = 0,
    Frozen = 1,
    Lost = 2
}

public sealed class WorldAccount
{
    private readonly Dictionary<string, decimal> _balances = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, decimal> _quotas = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<object> _uncommitted = [];

    public WorldAccount()
    {
    }

    public Guid Id { get; private set; }
    public WorldAccountKind Kind { get; private set; }
    public string Label { get; private set; } = "";
    public Guid? OrangeMemberId { get; private set; }
    public decimal? Level1CutPercent { get; private set; }
    public EmissionStatus EmissionStatus { get; private set; }
    public BalanceStatus BalanceStatus { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime LastUpdatedAt { get; private set; }
    public IReadOnlyDictionary<string, decimal> Balances => _balances;
    public IReadOnlyDictionary<string, decimal> Quotas => _quotas;

    [JsonIgnore]
    public IReadOnlyList<object> UncommittedEvents => _uncommitted;

    public bool IsGateway => Kind == WorldAccountKind.Gateway;

    public decimal BalanceOf(string currency) =>
        _balances.TryGetValue(NormalizeCurrency(currency), out var amount) ? amount : 0;

    public decimal QuotaOf(string currency) =>
        _quotas.TryGetValue(NormalizeCurrency(currency), out var amount) ? amount : 0;

    public bool CanEmit(string currency, decimal grossAmount) =>
        IsGateway
        && EmissionStatus == EmissionStatus.Ok
        && BalanceStatus != BalanceStatus.Lost
        && grossAmount > 0
        && QuotaOf(currency) >= grossAmount;

    public static IResult<WorldAccount> Open(
        WorldAccountKind kind,
        string label,
        Guid? orangeMemberId,
        decimal? level1CutPercent,
        string? quotaCurrency,
        decimal? quotaRemaining,
        Guid? actedBy = null)
    {
        if (string.IsNullOrWhiteSpace(label))
            return Fail<WorldAccount>(WorldAccountErrorCodes.LabelEmpty, "Rotulo da Conta e obrigatorio.");

        if (kind == WorldAccountKind.Gateway)
        {
            if (orangeMemberId is null || orangeMemberId == Guid.Empty)
                return Fail<WorldAccount>(WorldAccountErrorCodes.OrangeRequired, "Conta de Gateway exige Laranja.");
            var cut = level1CutPercent ?? 0;
            var cutCheck = ValidateCut(cut);
            if (cutCheck.IsFailure)
                return Result<WorldAccount>.Failure(cutCheck.Errors);
        }
        else if (orangeMemberId is not null)
        {
            return Fail<WorldAccount>(WorldAccountErrorCodes.OrangeNotAllowed, "Laranja so se aplica a Conta de Gateway.");
        }

        CurrencyAmount[] quotas = [];
        if (quotaRemaining is not null)
        {
            if (quotaRemaining.Value < 0)
                return Fail<WorldAccount>(WorldAccountErrorCodes.InvalidQuota, "Quota nao pode ser negativa.");
            var currency = NormalizeCurrency(quotaCurrency);
            if (string.IsNullOrEmpty(currency))
                return Fail<WorldAccount>(WorldAccountErrorCodes.CurrencyEmpty, "Moeda da quota e obrigatoria.");
            quotas = [new CurrencyAmount(currency, quotaRemaining.Value)];
        }

        var account = new WorldAccount();
        var now = DateTime.UtcNow;
        account.ApplyChange(new WorldAccountOpened(
            Guid.NewGuid(),
            kind.ToString(),
            label.Trim(),
            kind == WorldAccountKind.Gateway ? orangeMemberId : null,
            kind == WorldAccountKind.Gateway ? (level1CutPercent ?? 0) : null,
            quotas,
            now,
            actedBy));
        return Result<WorldAccount>.Success(account);
    }

    public void ClearUncommitted() => _uncommitted.Clear();

    public IResult Relabel(string label, Guid? actedBy = null)
    {
        if (string.IsNullOrWhiteSpace(label))
            return Fail(WorldAccountErrorCodes.LabelEmpty, "Rotulo da Conta e obrigatorio.");
        ApplyChange(new WorldAccountLabeled(Id, label.Trim(), DateTime.UtcNow, actedBy));
        return Result.Success();
    }

    public IResult ConfigureGateway(decimal? level1CutPercent, Guid? orangeMemberId, Guid? actedBy = null)
    {
        if (!IsGateway)
            return Fail(WorldAccountErrorCodes.NotGateway, "So Conta de Gateway aceita cut/Laranja.");

        if (level1CutPercent is not null)
        {
            var cut = ValidateCut(level1CutPercent.Value);
            if (cut.IsFailure)
                return cut;
            ApplyChange(new GatewayCutConfigured(Id, level1CutPercent.Value, DateTime.UtcNow, actedBy));
        }

        if (orangeMemberId is not null)
        {
            if (orangeMemberId == Guid.Empty)
                return Fail(WorldAccountErrorCodes.OrangeRequired, "Laranja invalido.");
            ApplyChange(new GatewayOrangeChanged(Id, orangeMemberId.Value, DateTime.UtcNow, actedBy));
        }

        return Result.Success();
    }

    public IResult SetEmissionStatus(EmissionStatus status, Guid? actedBy = null)
    {
        if (!IsGateway)
            return Fail(WorldAccountErrorCodes.NotGateway, "Eixo de emissão só existe em Conta de Gateway.");
        if (BalanceStatus == BalanceStatus.Lost)
            return Fail(WorldAccountErrorCodes.BalanceLost, "Conta perdida não emite.");
        ApplyChange(new EmissionStatusChanged(Id, status.ToString(), DateTime.UtcNow, actedBy));
        return Result.Success();
    }

    public IResult SetBalanceStatus(BalanceStatus status, Guid? actedBy = null)
    {
        if (BalanceStatus == BalanceStatus.Lost && status != BalanceStatus.Lost)
            return Fail(WorldAccountErrorCodes.BalanceLost, "Conta perdida não muda de estado.");
        ApplyChange(new BalanceStatusChanged(Id, status.ToString(), DateTime.UtcNow, actedBy));
        return Result.Success();
    }

    public IResult ConfigureQuota(string currency, decimal remaining, Guid? actedBy = null)
    {
        if (remaining < 0)
            return Fail(WorldAccountErrorCodes.InvalidQuota, "Quota nao pode ser negativa.");
        var normalized = NormalizeCurrency(currency);
        if (string.IsNullOrEmpty(normalized))
            return Fail(WorldAccountErrorCodes.CurrencyEmpty, "Moeda da quota e obrigatoria.");
        ApplyChange(new QuotaConfigured(Id, normalized, remaining, DateTime.UtcNow, actedBy));
        return Result.Success();
    }

    public IResult ConsumeQuota(string currency, decimal amount, Guid? chargeId = null, Guid? actedBy = null)
    {
        if (!CanEmit(currency, amount))
        {
            if (!IsGateway)
                return Fail(WorldAccountErrorCodes.NotGateway, "So Conta de Gateway emite.");
            if (EmissionStatus != EmissionStatus.Ok)
                return Fail(WorldAccountErrorCodes.EmissionBlocked, "Emissao bloqueada nesta Conta.");
            if (BalanceStatus == BalanceStatus.Lost)
                return Fail(WorldAccountErrorCodes.BalanceLost, "Conta com saldo perdido nao emite.");
            return Fail(WorldAccountErrorCodes.NoQuota, "Sem quota disponivel nesta moeda.");
        }

        ApplyChange(new QuotaConsumed(Id, NormalizeCurrency(currency), amount, chargeId, DateTime.UtcNow, actedBy));
        return Result.Success();
    }

    public IResult Credit(string currency, decimal amount, string? memo = null, Guid? actedBy = null)
    {
        var check = ValidateObservation(currency, amount);
        if (check.IsFailure)
            return check;
        ApplyChange(new ObservedCredited(Id, NormalizeCurrency(currency), amount, memo, DateTime.UtcNow, actedBy));
        return Result.Success();
    }

    public IResult Debit(string currency, decimal amount, string? memo = null, Guid? actedBy = null)
    {
        var check = ValidateObservation(currency, amount);
        if (check.IsFailure)
            return check;
        if (BalanceOf(currency) < amount)
            return Fail(WorldAccountErrorCodes.InsufficientBalance, "Saldo insuficiente nesta moeda.");
        ApplyChange(new ObservedDebited(Id, NormalizeCurrency(currency), amount, memo, DateTime.UtcNow, actedBy));
        return Result.Success();
    }

    public void Apply(WorldAccountOpened e)
    {
        Id = e.AccountId;
        Kind = Enum.Parse<WorldAccountKind>(e.Kind, true);
        Label = e.Label;
        OrangeMemberId = e.OrangeMemberId;
        Level1CutPercent = e.Level1CutPercent;
        EmissionStatus = EmissionStatus.Ok;
        BalanceStatus = BalanceStatus.Accessible;
        CreatedAt = e.OccurredAt;
        LastUpdatedAt = e.OccurredAt;
        ReplaceAmounts(_quotas, e.InitialQuotas);
    }

    public void Apply(WorldAccountBackfilled e)
    {
        Id = e.AccountId;
        Kind = Enum.Parse<WorldAccountKind>(e.Kind, true);
        Label = e.Label;
        OrangeMemberId = e.OrangeMemberId;
        Level1CutPercent = e.Level1CutPercent;
        EmissionStatus = Enum.TryParse<EmissionStatus>(e.EmissionStatus, true, out var es) ? es : EmissionStatus.Ok;
        BalanceStatus = Enum.TryParse<BalanceStatus>(e.BalanceStatus, true, out var bs) ? bs : BalanceStatus.Accessible;
        CreatedAt = e.CreatedAt;
        LastUpdatedAt = e.LastUpdatedAt;
        ReplaceAmounts(_quotas, e.Quotas);
        ReplaceAmounts(_balances, e.Balances);
    }

    public void Apply(WorldAccountLabeled e)
    {
        Label = e.Label;
        LastUpdatedAt = e.OccurredAt;
    }

    public void Apply(GatewayCutConfigured e)
    {
        Level1CutPercent = e.Level1CutPercent;
        LastUpdatedAt = e.OccurredAt;
    }

    public void Apply(GatewayOrangeChanged e)
    {
        OrangeMemberId = e.OrangeMemberId;
        LastUpdatedAt = e.OccurredAt;
    }

    public void Apply(EmissionStatusChanged e)
    {
        EmissionStatus = Enum.Parse<EmissionStatus>(e.Status, true);
        LastUpdatedAt = e.OccurredAt;
    }

    public void Apply(BalanceStatusChanged e)
    {
        BalanceStatus = Enum.Parse<BalanceStatus>(e.Status, true);
        LastUpdatedAt = e.OccurredAt;
    }

    public void Apply(QuotaConfigured e)
    {
        _quotas[e.Currency] = e.Remaining;
        LastUpdatedAt = e.OccurredAt;
    }

    public void Apply(QuotaConsumed e)
    {
        _quotas[e.Currency] = QuotaOf(e.Currency) - e.Amount;
        LastUpdatedAt = e.OccurredAt;
    }

    public void Apply(ObservedCredited e)
    {
        _balances[e.Currency] = BalanceOf(e.Currency) + e.Amount;
        LastUpdatedAt = e.OccurredAt;
    }

    public void Apply(ObservedDebited e)
    {
        _balances[e.Currency] = BalanceOf(e.Currency) - e.Amount;
        LastUpdatedAt = e.OccurredAt;
    }

    private static void ReplaceAmounts(Dictionary<string, decimal> target, CurrencyAmount[]? items)
    {
        target.Clear();
        if (items is null)
            return;
        foreach (var item in items)
            target[NormalizeCurrency(item.Currency)] = item.Amount;
    }

    private static IResult ValidateCut(decimal percent)
    {
        if (percent is < 0 or > 100)
            return Fail(WorldAccountErrorCodes.InvalidCut, "Cut nivel-1 deve estar em [0, 100].");
        return Result.Success();
    }

    private static IResult ValidateObservation(string currency, decimal amount)
    {
        if (amount <= 0)
            return Fail(WorldAccountErrorCodes.InvalidAmount, "Valor observado deve ser maior que zero.");
        if (string.IsNullOrWhiteSpace(NormalizeCurrency(currency)))
            return Fail(WorldAccountErrorCodes.CurrencyEmpty, "Moeda e obrigatoria.");
        return Result.Success();
    }

    private static string NormalizeCurrency(string? currency) =>
        string.IsNullOrWhiteSpace(currency) ? "" : currency.Trim().ToUpperInvariant();

    private static IResult Fail(string code, string message) =>
        Result.Failure(Error.Create().WithCode(code).WithMessage(message).Build());

    private static IResult<T> Fail<T>(string code, string message) =>
        Result<T>.Failure(Error.Create().WithCode(code).WithMessage(message).Build());

    private void ApplyChange(object @event)
    {
        switch (@event)
        {
            case WorldAccountOpened e: Apply(e); break;
            case WorldAccountBackfilled e: Apply(e); break;
            case WorldAccountLabeled e: Apply(e); break;
            case GatewayCutConfigured e: Apply(e); break;
            case GatewayOrangeChanged e: Apply(e); break;
            case EmissionStatusChanged e: Apply(e); break;
            case BalanceStatusChanged e: Apply(e); break;
            case QuotaConfigured e: Apply(e); break;
            case QuotaConsumed e: Apply(e); break;
            case ObservedCredited e: Apply(e); break;
            case ObservedDebited e: Apply(e); break;
            default: throw new InvalidOperationException(@event.GetType().Name);
        }

        _uncommitted.Add(@event);
    }
}

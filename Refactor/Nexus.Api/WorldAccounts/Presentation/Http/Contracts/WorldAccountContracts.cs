namespace Refactor.Nexus.Api.WorldAccounts.Presentation.Http.Contracts;

public sealed class OpenWorldAccountRequest
{
    public string Kind { get; set; } = "Gateway";
    public string Label { get; set; } = "";
    public string? OrangeMemberId { get; set; }
    public decimal? Level1CutPercent { get; set; }
    public string? QuotaCurrency { get; set; }
    public decimal? QuotaRemaining { get; set; }
}

public sealed class LabelWorldAccountRequest
{
    public string Label { get; set; } = "";
}

public sealed class ConfigureWorldAccountRequest
{
    public decimal? Level1CutPercent { get; set; }
    public string? OrangeMemberId { get; set; }
    public string? QuotaCurrency { get; set; }
    public decimal? QuotaRemaining { get; set; }
    public string? EmissionStatus { get; set; }
    public string? BalanceStatus { get; set; }
}

public sealed class RecordObservationRequest
{
    public string Direction { get; set; } = "credit";
    public string Currency { get; set; } = "BRL";
    public decimal Amount { get; set; }
    public string? Memo { get; set; }
}

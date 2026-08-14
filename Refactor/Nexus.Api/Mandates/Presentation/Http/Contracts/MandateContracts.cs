namespace Refactor.Nexus.Api.Mandates.Presentation.Http.Contracts;

public sealed class PresetRequest
{
    public string AccountId { get; set; } = string.Empty;
    public string PresetId { get; set; } = string.Empty;
}

public sealed class CapabilityRequest
{
    public string AccountId { get; set; } = string.Empty;
    public string Capability { get; set; } = string.Empty;
    public string ScopeKind { get; set; } = string.Empty;
    public Guid[]? OperationIds { get; set; }
}

public sealed class UpsertDealRequest
{
    public string RecruiterAccountId { get; set; } = string.Empty;
    public string OperatorAccountId { get; set; } = string.Empty;
    public decimal OperatorPercent { get; set; }
    public decimal RecruiterPercent { get; set; }
}

public sealed class CloseDealRequest
{
    public string OperatorAccountId { get; set; } = string.Empty;
}

public sealed class UpsertShareholderRequest
{
    public string AccountId { get; set; } = string.Empty;
    public decimal Percentage { get; set; }
}

public sealed class MemberAttritionRequest
{
    public string Status { get; set; } = string.Empty;
    public string Cause { get; set; } = string.Empty;
}

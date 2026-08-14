namespace Refactor.Nexus.Api.Charging.Presentation.Http.Contracts;

public sealed class CreateChargeRequest
{
    public string OperationId { get; set; } = "";
    public decimal GrossAmount { get; set; }
    public string? Currency { get; set; }
    public string? EmissionRailId { get; set; }
    public string? OperatorMemberId { get; set; }
}

public sealed class TransitionChargeRequest
{
    public string Target { get; set; } = "";
}

public sealed class MarkPaidWebhookRequest
{
    public string? ChargeId { get; set; }
    public string? ExternalReference { get; set; }
}

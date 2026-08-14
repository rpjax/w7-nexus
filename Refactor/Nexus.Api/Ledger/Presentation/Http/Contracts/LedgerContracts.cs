namespace Refactor.Nexus.Api.Ledger.Presentation.Http.Contracts;

public sealed class MaterializeChargeRequest
{
    public string ChargeId { get; set; } = "";
    public decimal NetAmount { get; set; }
    public string? Currency { get; set; }
    public string LandingWorldAccountId { get; set; } = "";
}

public sealed class HopDestinationRequest
{
    public string AccountId { get; set; } = "";
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "BRL";
}

public sealed class HopCutRequest
{
    public string OrangeMemberId { get; set; } = "";
    public decimal Percent { get; set; }
    public bool InPlace { get; set; }
    public string? OrangeAccountId { get; set; }
}

public sealed class RegisterHopRequest
{
    public string OriginAccountId { get; set; } = "";
    public string Currency { get; set; } = "BRL";
    public List<string>? ClaimIds { get; set; }
    public List<HopDestinationRequest>? Destinations { get; set; }
    public HopCutRequest? Cut { get; set; }
    public bool KeepRemainderAtOrigin { get; set; }
    public string? LossCause { get; set; }
}

public sealed class ReverseChargeRequest
{
    public string Cause { get; set; } = "";
}

public sealed class RepassClaimsRequest
{
    public string OriginAccountId { get; set; } = "";
    public List<string>? ClaimIds { get; set; }
    public string PayoutAccountId { get; set; } = "";
}

public sealed class RevealClaimRequest
{
    public string Summary { get; set; } = "";
}

public sealed class MarkAccountLostRequest
{
    public string Cause { get; set; } = "";
}

public sealed class ReconcileAccountRequest
{
    public string Currency { get; set; } = "BRL";
    public decimal ObservedBalance { get; set; }
    public string Cause { get; set; } = "";
    public string? ClaimId { get; set; }
}

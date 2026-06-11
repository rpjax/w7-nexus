namespace Nexus.Actors.Requests;

public class SetOperatorProfitShareRuleRequest
{
    public string? TeamId { get; set; }
    public string? OperatorId { get; set; }
    public ProfitShareCutRequest[] Cuts { get; set; } = Array.Empty<ProfitShareCutRequest>();
}

public class ProfitShareCutRequest
{
    public string? AccountId { get; set; }
    public decimal Percentage { get; set; }
}

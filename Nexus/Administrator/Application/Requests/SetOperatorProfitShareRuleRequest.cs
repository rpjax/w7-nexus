namespace Nexus.Administrator.Application.Requests;

public class SetOperatorProfitShareRuleRequest
{
    public string TeamId { get; set; } = string.Empty;
    public string OperatorId { get; set; } = string.Empty;
    public ProfitShareCutRequest[] Cuts { get; set; } = Array.Empty<ProfitShareCutRequest>();
}

public class ProfitShareCutRequest
{
    public string AccountId { get; set; } = string.Empty;
    public decimal Percentage { get; set; }
}

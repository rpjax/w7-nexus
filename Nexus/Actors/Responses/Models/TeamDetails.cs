namespace Nexus.Actors.Responses.Models;

public class TeamDetails
{
    public string Id { get; init; } = default!;
    public string OperationId { get; init; } = default!;
    public string Name { get; init; } = default!;
    public TeamLeaderDetails? TeamLeader { get; init; }
    public OperatorDetails[] Operators { get; init; } = Array.Empty<OperatorDetails>();
    public string GatewaySelectionStrategy { get; init; } = default!;
    public TeamAccountDetails[] StrawMen { get; init; } = Array.Empty<TeamAccountDetails>();
    public TeamGatewayCredentialDetails[] GatewayCredentials { get; init; } = Array.Empty<TeamGatewayCredentialDetails>();
    public TeamGatewayGroupDetails[] GatewayCredentialsGroups { get; init; } = Array.Empty<TeamGatewayGroupDetails>();
}

public class TeamAccountDetails
{
    public string AccountId { get; set; } = default!;
    public string Username { get; init; } = default!;
}

public class TeamLeaderDetails
{
    public string AccountId { get; set; } = default!;
    public string Username { get; init; } = default!;
}

public class OperationTeamLeaderDetails
{
    public string AccountId { get; set; } = default!;
    public string Username { get; init; } = default!;
}

public class OperatorDetails
{
    public string AccountId { get; set; } = default!;
    public string Username { get; init; } = default!;
    public ProfitShareRuleDetails ProfitShareRule { get; init; } = new();
}

public class ProfitShareRuleDetails
{
    public ProfitSplitDetails[] Cuts { get; set; } = Array.Empty<ProfitSplitDetails>();
}

public class ProfitSplitDetails
{
    public string AccountId { get; set; } = default!;
    public string Username { get; init; } = default!;
    public decimal Percentage { get; set; }
}
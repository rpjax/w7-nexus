using Nexus.Database.Models;
using Nexus.Operations.Aggregates;

namespace Nexus.Tests.Payments;

internal static class TeamTestFactory
{
    public static Team WithOperatorProfitShare(
        string teamId,
        string operationId,
        string operatorId,
        params (string AccountId, decimal Percentage)[] cuts)
    {
        var now = DateTime.UtcNow;
        return new Team(
            teamId,
            operationId,
            "Team",
            null,
            new[] { operatorId },
            Array.Empty<string>(),
            GatewaySelectionStrategy.PerStrawman,
            Array.Empty<string>(),
            Array.Empty<string>(),
            new[]
            {
                new OperatorProfitShareRuleRecord
                {
                    OperatorId = operatorId,
                    Cuts = cuts
                        .Select(cut => new ProfitSplitRecord
                        {
                            AccountId = cut.AccountId,
                            Percentage = cut.Percentage,
                        })
                        .ToList(),
                },
            },
            now,
            now);
    }
}

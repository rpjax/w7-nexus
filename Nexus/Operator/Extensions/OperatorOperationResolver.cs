using Aidan.Core.Linq.Extensions;
using Nexus.Operations.Application.Contracts;
using Nexus.Payments.Application.Contracts;

namespace Nexus.Operator.Extensions;

internal static class OperatorOperationResolver
{
    public static async Task<string[]> ResolveOperationIdsAsync(
        string operatorAccountId,
        ITeamRepository teams,
        IPaymentRepository payments)
    {
        var normalizedOperatorAccountId = operatorAccountId.Trim();

        var teamOperationIds = await teams.AsQueryable()
            .Where(t =>
                t.OperatorIds.Contains(normalizedOperatorAccountId)
                || t.OperatorProfitShareRules.Any(r =>
                    r.OperatorId == normalizedOperatorAccountId
                    || r.Cuts.Any(c => c.AccountId == normalizedOperatorAccountId)))
            .Select(t => t.OperationId)
            .Distinct()
            .ToArrayAsync();

        var paymentOperationIds = await payments.AsQueryable()
            .Where(p => p.OperatorAccountId == normalizedOperatorAccountId)
            .Select(p => p.OperationId)
            .Distinct()
            .ToArrayAsync();

        if (teamOperationIds.Length == 0 && paymentOperationIds.Length == 0)
            return Array.Empty<string>();

        return teamOperationIds
            .Concat(paymentOperationIds)
            .Distinct(StringComparer.Ordinal)
            .ToArray();
    }
}

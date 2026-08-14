using Refactor.Nexus.Api.Charging.Domain.ValueObjects;

namespace Refactor.Nexus.Api.Ledger.Domain.Services;

public sealed record WaterfallSlice(Guid BeneficiaryId, decimal Amount, string Kind);

public static class WaterfallMaterializer
{
    public static IReadOnlyList<WaterfallSlice> Allocate(SplitIntent intent, decimal netAmount)
    {
        var remaining = netAmount;
        var slices = new List<WaterfallSlice>();

        foreach (var line in intent.Lines.OrderBy(l => l.Order))
        {
            if (line.Kind == SplitIntent.ResidualOrg)
                continue;

            if (line.Participants.Count > 0)
            {
                foreach (var participant in line.Participants)
                {
                    if (participant.PercentOfLineBase <= 0 || remaining <= 0)
                        continue;

                    var amount = Take(remaining * participant.PercentOfLineBase / 100m, remaining);
                    if (amount <= 0)
                        continue;

                    slices.Add(new WaterfallSlice(participant.MemberId, amount, line.Kind));
                    remaining -= amount;
                }

                continue;
            }

            if (line.PercentOfRemainder <= 0 || remaining <= 0)
                continue;

            var lump = Take(remaining * line.PercentOfRemainder / 100m, remaining);
            if (lump <= 0)
                continue;

            slices.Add(new WaterfallSlice(OrganizationParty.Id, lump, line.Kind));
            remaining -= lump;
        }

        if (remaining > 0)
            slices.Add(new WaterfallSlice(OrganizationParty.Id, remaining, SplitIntent.ResidualOrg));
        else if (remaining < 0)
            AddDustToLast(slices, remaining);

        return slices.Where(s => s.Amount > 0).ToList();
    }

    private static decimal Take(decimal proposed, decimal remaining)
    {
        var rounded = Math.Round(proposed, 2, MidpointRounding.AwayFromZero);
        if (rounded > remaining)
            return remaining;
        if (rounded < 0)
            return 0;
        return rounded;
    }

    private static void AddDustToLast(List<WaterfallSlice> slices, decimal dust)
    {
        for (var i = slices.Count - 1; i >= 0; i--)
        {
            var next = slices[i].Amount + dust;
            if (next > 0)
            {
                slices[i] = slices[i] with { Amount = next };
                return;
            }
        }
    }
}

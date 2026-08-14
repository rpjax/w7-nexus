using Aidan.Core.Errors;
using Aidan.Core.Patterns;
using IResult = Aidan.Core.Patterns.IResult;
using Refactor.Nexus.Api.Ledger.Domain.Errors;

namespace Refactor.Nexus.Api.Ledger.Domain.Services;

public sealed record HopBundleItem(
    Guid ClaimId,
    Guid BeneficiaryId,
    decimal Amount,
    Guid OriginChargeId,
    Guid LocationAccountId,
    string Kind,
    string Currency,
    decimal BirthAmount,
    string BirthCurrency);

public sealed record HopDestSpec(Guid AccountId, decimal Amount, string Currency);

public sealed record HopCutSpec(Guid OrangeMemberId, decimal Percent, bool InPlace, Guid? OrangeAccountId);

public sealed record HopNewClaimSpec(
    Guid BeneficiaryId,
    decimal Amount,
    string Currency,
    Guid OriginChargeId,
    Guid LocationAccountId,
    string Kind,
    Guid? ParentClaimId,
    decimal BirthAmount,
    string BirthCurrency);

public sealed record HopPlan(
    IReadOnlyList<(Guid ClaimId, decimal Amount, string Currency, Guid Location)> Adjustments,
    IReadOnlyList<Guid> Archives,
    IReadOnlyList<HopNewClaimSpec> NewClaims,
    IReadOnlyList<(Guid AccountId, decimal Amount, string Currency, bool Credit)> WorldMoves,
    decimal LossAmount);

public static class HopAllocator
{
    public static IResult<HopPlan> Plan(
        IReadOnlyList<HopBundleItem> bundle,
        IReadOnlyList<HopDestSpec> destinations,
        HopCutSpec? cut,
        bool keepRemainderAtOrigin = false)
    {
        if (bundle.Count == 0)
            return Fail("Bundle vazio.");

        var originCurrency = bundle[0].Currency;
        if (bundle.Any(c => !string.Equals(c.Currency, originCurrency, StringComparison.OrdinalIgnoreCase)))
            return Fail("Bundle deve ser de uma so moeda.");

        var b = bundle.Sum(c => c.Amount);
        if (b <= 0)
            return Fail("Bundle deve ser maior que zero.");

        var working = bundle.Select(c => (Item: c, Remaining: c.Amount)).ToList();
        var newClaims = new List<HopNewClaimSpec>();
        var world = new List<(Guid AccountId, decimal Amount, string Currency, bool Credit)>();
        var originId = bundle[0].LocationAccountId;

        var extraOriginDebit = 0m;
        if (cut is not null)
        {
            if (cut.Percent is < 0 or > 100)
                return Fail("Cut deve estar em [0, 100].");

            var cutAmount = Round(b * cut.Percent / 100m);
            if (cutAmount > 0)
            {
                working = Scale(working, b - cutAmount, b);
                var cutLocation = cut.InPlace ? originId : cut.OrangeAccountId ?? Guid.Empty;
                if (cutLocation == Guid.Empty)
                    return Fail("Cut com transferencia exige Conta do Laranja.");

                newClaims.Add(new HopNewClaimSpec(
                    cut.OrangeMemberId,
                    cutAmount,
                    originCurrency,
                    working[0].Item.OriginChargeId,
                    cutLocation,
                    "PathCut",
                    null,
                    cutAmount,
                    originCurrency));

                if (!cut.InPlace)
                {
                    world.Add((cutLocation, cutAmount, originCurrency, true));
                    extraOriginDebit = cutAmount;
                }
            }
        }

        var remainingTotal = working.Sum(w => w.Remaining);
        var dests = destinations.ToList();
        if (dests.Count == 0)
        {
            if (cut is { InPlace: true })
            {
                return Result<HopPlan>.Success(BuildPlan(working, newClaims, world, originId, originCurrency, remainingTotal, inPlaceOnly: true, loss: 0));
            }

            return Fail("Hop exige destino (exceto cut in-place).");
        }

        var destCurrency = dests[0].Currency.Trim().ToUpperInvariant();
        if (dests.Any(d => !string.Equals(d.Currency.Trim(), destCurrency, StringComparison.OrdinalIgnoreCase)))
            return Fail("Todos os destinos devem ter a mesma moeda.");

        var s = dests.Sum(d => d.Amount);
        if (s <= 0)
            return Fail("Soma dos destinos deve ser maior que zero.");

        var sameCurrency = string.Equals(destCurrency, originCurrency, StringComparison.OrdinalIgnoreCase);
        if (sameCurrency && s > remainingTotal)
            return Fail("Soma dos destinos nao pode exceder o bundle (pos-cut).");

        if (sameCurrency)
        {
            if (keepRemainderAtOrigin && remainingTotal > s)
            {
                world.Add((originId, s + extraOriginDebit, originCurrency, false));
                foreach (var dest in dests)
                    world.Add((dest.AccountId, dest.Amount, destCurrency, true));
                return Result<HopPlan>.Success(
                    SplitKeepingRemainder(working, dests, destCurrency, originId, originCurrency, newClaims, world, s));
            }

            var loss = remainingTotal - s;
            working = Scale(working, s, remainingTotal);
            world.Add((originId, remainingTotal + extraOriginDebit, originCurrency, false));
            foreach (var dest in dests)
                world.Add((dest.AccountId, dest.Amount, destCurrency, true));

            return Result<HopPlan>.Success(SplitToDestinations(working, dests, destCurrency, newClaims, world, loss));
        }

        world.Add((originId, remainingTotal + extraOriginDebit, originCurrency, false));
        foreach (var dest in dests)
            world.Add((dest.AccountId, dest.Amount, destCurrency, true));

        var archives = working.Select(w => w.Item.ClaimId).ToList();
        var redenom = new List<HopNewClaimSpec>(newClaims);
        var originWeights = working.Select(w => w.Remaining).ToList();
        foreach (var dest in dests)
        {
            var parts = SplitAmount(dest.Amount, originWeights, remainingTotal);
            for (var i = 0; i < working.Count; i++)
            {
                if (parts[i] <= 0)
                    continue;
                var row = working[i];
                redenom.Add(new HopNewClaimSpec(
                    row.Item.BeneficiaryId,
                    parts[i],
                    destCurrency,
                    row.Item.OriginChargeId,
                    dest.AccountId,
                    row.Item.Kind,
                    row.Item.ClaimId,
                    row.Item.BirthAmount,
                    row.Item.BirthCurrency));
            }
        }

        return Result<HopPlan>.Success(new HopPlan([], archives, redenom, world, 0));
    }

    private static HopPlan BuildPlan(
        List<(HopBundleItem Item, decimal Remaining)> working,
        List<HopNewClaimSpec> newClaims,
        List<(Guid, decimal, string, bool)> world,
        Guid originId,
        string originCurrency,
        decimal remainingTotal,
        bool inPlaceOnly,
        decimal loss)
    {
        var adjustments = working
            .Where(w => w.Remaining > 0)
            .Select(w => (w.Item.ClaimId, w.Remaining, originCurrency, originId))
            .ToList();
        var archives = working.Where(w => w.Remaining <= 0).Select(w => w.Item.ClaimId).ToList();
        return new HopPlan(adjustments, archives, newClaims, world, loss);
    }

    private static HopPlan SplitKeepingRemainder(
        List<(HopBundleItem Item, decimal Remaining)> working,
        List<HopDestSpec> dests,
        string destCurrency,
        Guid originId,
        string originCurrency,
        List<HopNewClaimSpec> newClaims,
        List<(Guid, decimal, string, bool)> world,
        decimal destTotal)
    {
        var originalTotal = working.Sum(w => w.Remaining);
        var destShares = Scale(working, destTotal, originalTotal);
        var leftovers = working
            .Select((w, i) => (w.Item, Leftover: Round(w.Remaining - destShares[i].Remaining)))
            .ToList();

        var remainderClaims = leftovers
            .Where(row => row.Leftover > 0)
            .Select(row => new HopNewClaimSpec(
                row.Item.BeneficiaryId,
                row.Leftover,
                originCurrency,
                row.Item.OriginChargeId,
                originId,
                row.Item.Kind,
                row.Item.ClaimId,
                row.Item.BirthAmount,
                row.Item.BirthCurrency))
            .ToList();

        var destPlan = SplitToDestinations(destShares, dests, destCurrency, [.. newClaims, .. remainderClaims], world, 0);
        return destPlan;
    }

    private static HopPlan SplitToDestinations(
        List<(HopBundleItem Item, decimal Remaining)> working,
        List<HopDestSpec> dests,
        string destCurrency,
        List<HopNewClaimSpec> newClaims,
        List<(Guid, decimal, string, bool)> world,
        decimal loss)
    {
        var destTotal = dests.Sum(d => d.Amount);
        if (dests.Count == 1)
        {
            var dest = dests[0];
            var adjustments = working
                .Where(w => w.Remaining > 0)
                .Select(w => (w.Item.ClaimId, w.Remaining, destCurrency, dest.AccountId))
                .ToList();
            var archives = working.Where(w => w.Remaining <= 0).Select(w => w.Item.ClaimId).ToList();
            return new HopPlan(adjustments, archives, newClaims, world, loss);
        }

        var archivesAll = working.Select(w => w.Item.ClaimId).ToList();
        var children = new List<HopNewClaimSpec>(newClaims);
        foreach (var row in working)
        {
            var parts = SplitAmount(row.Remaining, dests.Select(d => d.Amount).ToList(), destTotal);
            for (var i = 0; i < dests.Count; i++)
            {
                if (parts[i] <= 0)
                    continue;
                children.Add(new HopNewClaimSpec(
                    row.Item.BeneficiaryId,
                    parts[i],
                    destCurrency,
                    row.Item.OriginChargeId,
                    dests[i].AccountId,
                    row.Item.Kind,
                    row.Item.ClaimId,
                    row.Item.BirthAmount,
                    row.Item.BirthCurrency));
            }
        }

        return new HopPlan([], archivesAll, children, world, loss);
    }

    private static List<(HopBundleItem Item, decimal Remaining)> Scale(
        List<(HopBundleItem Item, decimal Remaining)> working,
        decimal newTotal,
        decimal oldTotal)
    {
        if (oldTotal <= 0)
            return working.Select(w => (w.Item, 0m)).ToList();

        var scaled = new List<(HopBundleItem, decimal)>();
        var allocated = 0m;
        for (var i = 0; i < working.Count; i++)
        {
            var amount = i == working.Count - 1
                ? newTotal - allocated
                : Round(working[i].Remaining * newTotal / oldTotal);
            if (amount < 0)
                amount = 0;
            allocated += amount;
            scaled.Add((working[i].Item, amount));
        }

        return scaled;
    }

    private static List<decimal> SplitAmount(decimal amount, List<decimal> weights, decimal weightTotal)
    {
        var parts = new List<decimal>();
        var allocated = 0m;
        for (var i = 0; i < weights.Count; i++)
        {
            var part = i == weights.Count - 1
                ? amount - allocated
                : Round(amount * weights[i] / weightTotal);
            if (part < 0)
                part = 0;
            allocated += part;
            parts.Add(part);
        }

        return parts;
    }

    private static decimal Round(decimal value) => Math.Round(value, 2, MidpointRounding.AwayFromZero);

    private static IResult<HopPlan> Fail(string message) =>
        Result<HopPlan>.Failure(Error.Create().WithCode(LedgerErrorCodes.HopInvalid).WithMessage(message).Build());
}

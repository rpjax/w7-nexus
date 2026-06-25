using Nexus.StrawMen.Application.Contracts;
using Nexus.Transfers.Aggregates;
using Nexus.Transfers.Application.Services;
using Xunit;

namespace Nexus.Tests.Transfers;

public sealed class BalanceSplitCalculationServiceTests
{
    private sealed class StubStrawManSettingsQueryService : IStrawManSettingsQueryService
    {
        private readonly Dictionary<string, decimal> _fees;

        public StubStrawManSettingsQueryService(Dictionary<string, decimal>? fees = null) =>
            _fees = fees ?? new Dictionary<string, decimal>(StringComparer.Ordinal);

        public Task<decimal> GetMovementFeePercentageAsync(string strawManId, CancellationToken cancellationToken = default) =>
            Task.FromResult(_fees.TryGetValue(strawManId, out var fee) ? fee : 0m);

        public Task<Aidan.Core.Patterns.IResult<StrawManSettingsDetails>> GetSettingsAsync(
            string strawManId,
            CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();
    }

    private static TransferBalanceSplit ProfitShare(string accountId, decimal percentage, decimal amount) =>
        TransferBalanceSplit.Create(accountId, percentage, amount, TransferSplitKind.ProfitShare).Value!;

    [Fact]
    public async Task CalculateForCreditAsync_AppliesStrawManFeeAndDilutesProfitShare()
    {
        var sut = new BalanceSplitCalculationService(new StubStrawManSettingsQueryService(
            new Dictionary<string, decimal> { ["straw-d"] = 10m }));

        var original = new[]
        {
            ProfitShare("operator-1", 50m, 50m),
            ProfitShare("partner-1", 50m, 50m),
        };

        var result = await sut.CalculateForCreditAsync("straw-d", 100m, original);

        Assert.True(result.IsSuccess);
        var splits = result.Value!.Splits;
        Assert.Equal(3, splits.Count);

        var operatorSplit = splits.Single(s => s.AccountId == "operator-1");
        Assert.Equal(45m, operatorSplit.Percentage);
        Assert.Equal(45m, operatorSplit.Amount);

        var strawSplit = splits.Single(s => s.SplitKind == TransferSplitKind.StrawManMovementFee);
        Assert.Equal("straw-d", strawSplit.AccountId);
        Assert.Equal(10m, strawSplit.Percentage);
        Assert.Equal(10m, strawSplit.Amount);
    }

    [Fact]
    public async Task CalculateForCreditAsync_WhenFeeAlreadyApplied_DoesNotRecalculate()
    {
        var sut = new BalanceSplitCalculationService(new StubStrawManSettingsQueryService(
            new Dictionary<string, decimal> { ["straw-d"] = 10m }));

        var diluted = new[]
        {
            ProfitShare("operator-1", 45m, 45m),
            TransferBalanceSplit.Create("straw-d", 10m, 10m, TransferSplitKind.StrawManMovementFee).Value!,
        };

        var result = await sut.CalculateForCreditAsync("straw-d", 100m, diluted);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value!.Splits.Count);
    }

    [Fact]
    public async Task CalculateForCreditAsync_DefaultFeeIsZero_KeepsOriginalSplits()
    {
        var sut = new BalanceSplitCalculationService(new StubStrawManSettingsQueryService());
        var original = new[] { ProfitShare("operator-1", 100m, 100m) };

        var result = await sut.CalculateForCreditAsync("straw-d", 100m, original);

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value!.Splits);
    }
}

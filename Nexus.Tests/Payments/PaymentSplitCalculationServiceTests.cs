using Nexus.Payments.Aggregates;
using Nexus.Tests.Payments;
using Xunit;

namespace Nexus.Tests.Payments;

public sealed class PaymentSplitCalculationServiceTests
{
    [Fact]
    public async Task ApplyStrawManFeeAsync_DilutesProfitShareAndAddsStrawManSplit()
    {
        var sut = PaymentTestDoubles.SplitCalculation(new Dictionary<string, decimal> { ["straw-1"] = 10m });
        var profitShare = new[]
        {
            new PaymentSplit("operator-1", 50m, 50m),
            new PaymentSplit("partner-1", 50m, 50m),
        };

        var result = await sut.ApplyStrawManFeeAsync(100m, profitShare, "straw-1");

        Assert.Equal(3, result.Count);
        Assert.Equal(100m, result.Sum(split => split.Amount));

        var strawSplit = result.Single(s => s.SplitKind == PaymentSplitKind.StrawManFee);
        Assert.Equal("straw-1", strawSplit.AccountId);
        Assert.Equal(10m, strawSplit.Percentage);
        Assert.Equal(10m, strawSplit.Amount);

        var operatorSplit = result.Single(s => s.AccountId == "operator-1");
        Assert.Equal(45m, operatorSplit.Percentage);
        Assert.Equal(45m, operatorSplit.Amount);
    }

    [Fact]
    public async Task ApplyStrawManFeeAsync_WhenFeeAlreadyPresent_IsIdempotent()
    {
        var sut = PaymentTestDoubles.SplitCalculation(new Dictionary<string, decimal> { ["straw-1"] = 10m });
        var existing = new[]
        {
            new PaymentSplit("operator-1", 90m, 90m),
            new PaymentSplit("straw-1", 10m, 10m, PaymentSplitKind.StrawManFee),
        };

        var result = await sut.ApplyStrawManFeeAsync(100m, existing, "straw-1");

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task ApplyStrawManFeeAsync_WithoutConfiguredFee_KeepsOriginalSplits()
    {
        var sut = PaymentTestDoubles.SplitCalculation();
        var profitShare = new[] { new PaymentSplit("operator-1", 100m, 100m) };

        var result = await sut.ApplyStrawManFeeAsync(100m, profitShare, "straw-1");

        Assert.Single(result);
        Assert.Equal(PaymentSplitKind.ProfitShare, result[0].SplitKind);
    }
}

using Nexus.Withdrawals.Aggregates;
using Nexus.Withdrawals.Errors;
using Xunit;

namespace Nexus.Tests.Withdrawals;

public sealed class BrazilianBankMetadataTests
{
    [Fact]
    public void AllMembers_HaveMetadata()
    {
        foreach (var bank in Enum.GetValues<BrazilianBank>())
        {
            var (name, code, ispb) = BrazilianBankMetadata.Get(bank);
            Assert.False(string.IsNullOrWhiteSpace(name));
            Assert.False(string.IsNullOrWhiteSpace(code));
            Assert.False(string.IsNullOrWhiteSpace(ispb));
        }
    }

    [Fact]
    public void BancoDoBrasil_HasExpectedCode()
    {
        var (name, code, ispb) = BrazilianBankMetadata.Get(BrazilianBank.BancodoBrasilSA_001);
        Assert.Contains("Brasil", name, StringComparison.OrdinalIgnoreCase);
        Assert.Equal("001", code);
        Assert.Equal("00000000", ispb);
    }
}

using Nexus.BankAccounts.Aggregates;
using Xunit;

namespace Nexus.Tests.BankAccounts;

public sealed class BankAccountAggregateTests
{
    [Fact]
    public void Create_WithoutPixKey_Succeeds()
    {
        var result = BankAccount.Create(
            "straw-1",
            BrazilianBank.BancodoBrasilSA_001,
            "1234",
            "56789",
            "0",
            BankAccountType.Checking,
            "Main");

        Assert.True(result.IsSuccess);
        Assert.Equal("straw-1", result.Value!.OwnerId);
    }

    [Fact]
    public void BankBalance_DebitPartial_DividesBalanceCorrectly()
    {
        var origin = BankBalanceOrigin.Create("op-1", "operator-1").Value!;
        var split = BankBalanceSplit.Create("operator-1", 100m, 1000m, BankSplitKind.ProfitShare).Value!;
        var balance = BankBalance.Create(
            "bank-1",
            1000m,
            "transfer-1",
            new[] { split },
            origin).Value!;

        var debit = balance.DebitPartial(400m);

        Assert.True(debit.IsSuccess);
        Assert.Equal(400m, debit.Value!.DebitedBalance.AmountBrl);
        Assert.NotNull(debit.Value.RemainderBalance);
        Assert.Equal(600m, debit.Value.RemainderBalance!.AmountBrl);
    }
}

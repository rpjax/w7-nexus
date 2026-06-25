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
        Assert.Equal("straw-1", result.Value!.StrawManId);
        Assert.Empty(result.Value.Balances);
    }

    [Fact]
    public void DebitPartialBalance_DividesBalanceCorrectly()
    {
        var account = BankAccount.Create(
            "straw-1",
            BrazilianBank.BancodoBrasilSA_001,
            "1234",
            "56789",
            null,
            BankAccountType.Checking,
            null).Value!;

        var origin = BankBalanceOrigin.Create("op-1", "operator-1", "straw-1").Value!;
        var split = BankBalanceSplit.Create("operator-1", 100m, 1000m, BankSplitKind.ProfitShare).Value!;
        var balance = BankBalance.Create(1000m, "transfer-1", new[] { split }, Array.Empty<string>(), origin).Value!;

        Assert.True(account.CreditBalance(balance).IsSuccess);

        var debit = account.DebitPartialBalance(balance.Id, 400m);

        Assert.True(debit.IsSuccess);
        Assert.Equal(400m, debit.Value!.DebitedBalance.AmountBrl);
        Assert.NotNull(debit.Value.RemainderBalance);
        Assert.Equal(600m, debit.Value.RemainderBalance!.AmountBrl);
        Assert.Single(account.Balances);
        Assert.Equal(600m, account.Balances[0].AmountBrl);
    }
}

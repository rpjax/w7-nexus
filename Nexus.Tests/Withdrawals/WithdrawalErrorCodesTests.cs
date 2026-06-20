using Nexus.Withdrawals.Errors;
using Xunit;

namespace Nexus.Tests.Withdrawals;

public sealed class WithdrawalErrorCodesTests
{
    [Fact]
    public void WithdrawalCodes_AreStableNonEmptyStrings()
    {
        AssertErrorCodes(typeof(WithdrawalErrorCodes), "Withdrawal.");
    }

    [Fact]
    public void BankAccountCodes_AreStableNonEmptyStrings()
    {
        AssertErrorCodes(typeof(BankAccountErrorCodes), "BankAccount.");
    }

    [Fact]
    public void CryptoWalletCodes_AreStableNonEmptyStrings()
    {
        AssertErrorCodes(typeof(CryptoWalletErrorCodes), "CryptoWallet.");
    }

    private static void AssertErrorCodes(Type type, string prefix)
    {
        var codes = type
            .GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)
            .Where(f => f.FieldType == typeof(string));

        Assert.NotEmpty(codes);
        foreach (var field in codes)
        {
            var value = (string)field.GetValue(null)!;
            Assert.False(string.IsNullOrWhiteSpace(value));
            Assert.StartsWith(prefix, value, StringComparison.Ordinal);
        }
    }
}

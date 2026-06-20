using Nexus.Withdrawals.Aggregates;
using Nexus.Withdrawals.Errors;
using Xunit;

namespace Nexus.Tests.Withdrawals;

public sealed class BankAccountAggregateTests
{
    [Fact]
    public void Create_ValidAccount_Succeeds()
    {
        var result = BankAccount.Create(
            strawManAccountId: "straw-1",
            bank: BrazilianBank.ItauUnibancoSA_341,
            agency: "1234",
            accountNumber: "98765",
            accountDigit: "1",
            accountType: BankAccountType.Checking,
            pixKey: "joao@email.com",
            label: "Conta principal");

        Assert.True(result.IsSuccess);
        Assert.Equal("1234", result.Value!.Agency);
        Assert.Equal("joao@email.com", result.Value.PixKey);
    }

    [Fact]
    public void Create_WithOptionalFieldsNull_Succeeds()
    {
        var result = BankAccount.Create(
            strawManAccountId: "straw-1",
            bank: BrazilianBank.ItauUnibancoSA_341,
            agency: "1234",
            accountNumber: "98765",
            accountDigit: null,
            accountType: BankAccountType.Checking,
            pixKey: null,
            label: null);

        Assert.True(result.IsSuccess);
        Assert.Null(result.Value!.AccountDigit);
        Assert.Null(result.Value.PixKey);
    }

    [Fact]
    public void Create_MissingAgency_Fails()
    {
        var result = BankAccount.Create(
            strawManAccountId: "straw-1",
            bank: BrazilianBank.ItauUnibancoSA_341,
            agency: "",
            accountNumber: "98765",
            accountDigit: null,
            accountType: BankAccountType.Checking,
            pixKey: null,
            label: null);

        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors, e => e.Code == BankAccountErrorCodes.AgencyInvalid);
    }
}

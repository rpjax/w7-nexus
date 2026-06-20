using Nexus.Withdrawals.Aggregates;
using Nexus.Withdrawals.Errors;
using Xunit;

namespace Nexus.Tests.Withdrawals;

public sealed class BankAccountAggregateTests
{
    [Fact]
    public void Create_ValidAccount_NormalizesPixKey()
    {
        var result = BankAccount.Create(
            strawManAccountId: "straw-1",
            bank: BrazilianBank.ItauUnibancoSA_341,
            agency: "1234",
            accountNumber: "98765",
            accountDigit: "1",
            accountType: BankAccountType.Checking,
            pixKeyType: PixKeyType.Email,
            pixKey: "  Joao@Email.COM ",
            label: "Conta principal");

        Assert.True(result.IsSuccess);
        Assert.Equal(PixKeyType.Email, result.Value!.PixKeyType);
        Assert.Equal("joao@email.com", result.Value.PixKey);
    }

    [Fact]
    public void Create_MissingPixKey_Fails()
    {
        var result = BankAccount.Create(
            strawManAccountId: "straw-1",
            bank: BrazilianBank.ItauUnibancoSA_341,
            agency: "1234",
            accountNumber: "98765",
            accountDigit: null,
            accountType: BankAccountType.Checking,
            pixKeyType: PixKeyType.Cpf,
            pixKey: null,
            label: null);

        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors, e => e.Code == BankAccountErrorCodes.PixKeyRequired);
    }

    [Fact]
    public void Create_InvalidCpf_Fails()
    {
        var result = BankAccount.Create(
            strawManAccountId: "straw-1",
            bank: BrazilianBank.ItauUnibancoSA_341,
            agency: "1234",
            accountNumber: "98765",
            accountDigit: null,
            accountType: BankAccountType.Checking,
            pixKeyType: PixKeyType.Cpf,
            pixKey: "111.111.111-11",
            label: null);

        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors, e => e.Code == BankAccountErrorCodes.PixKeyInvalid);
    }

    [Fact]
    public void Create_InvalidAccountType_Fails()
    {
        var result = BankAccount.Create(
            strawManAccountId: "straw-1",
            bank: BrazilianBank.ItauUnibancoSA_341,
            agency: "1234",
            accountNumber: "98765",
            accountDigit: null,
            accountType: (BankAccountType)99,
            pixKeyType: PixKeyType.Email,
            pixKey: "conta@example.com",
            label: null);

        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors, e => e.Code == BankAccountErrorCodes.AccountTypeInvalid);
    }

    [Fact]
    public void Create_ValidCpf_NormalizesDigits()
    {
        var result = BankAccount.Create(
            strawManAccountId: "straw-1",
            bank: BrazilianBank.ItauUnibancoSA_341,
            agency: "1234",
            accountNumber: "98765",
            accountDigit: null,
            accountType: BankAccountType.Checking,
            pixKeyType: PixKeyType.Cpf,
            pixKey: "529.982.247-25",
            label: null);

        Assert.True(result.IsSuccess);
        Assert.Equal("52998224725", result.Value!.PixKey);
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
            pixKeyType: PixKeyType.Email,
            pixKey: "conta@example.com",
            label: null);

        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors, e => e.Code == BankAccountErrorCodes.AgencyInvalid);
    }
}

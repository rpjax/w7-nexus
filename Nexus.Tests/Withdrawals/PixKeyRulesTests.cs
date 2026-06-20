using Nexus.Withdrawals.Aggregates;
using Nexus.Withdrawals.Errors;
using Xunit;

namespace Nexus.Tests.Withdrawals;

public sealed class PixKeyRulesTests
{
    [Theory]
    [InlineData("529.982.247-25", "52998224725")]
    [InlineData("52998224725", "52998224725")]
    public void ValidateAndNormalize_Cpf_Succeeds(string raw, string expected)
    {
        var result = PixKeyRules.ValidateAndNormalize(PixKeyType.Cpf, raw);

        Assert.True(result.IsSuccess);
        Assert.Equal(PixKeyType.Cpf, result.Value!.Type);
        Assert.Equal(expected, result.Value.NormalizedKey);
    }

    [Theory]
    [InlineData("11.444.777/0001-61")]
    public void ValidateAndNormalize_Cnpj_Succeeds(string raw)
    {
        var result = PixKeyRules.ValidateAndNormalize(PixKeyType.Cnpj, raw);

        Assert.True(result.IsSuccess);
        Assert.Equal("11444777000161", result.Value!.NormalizedKey);
    }

    [Theory]
    [InlineData("cliente@mail.com", "cliente@mail.com")]
    [InlineData("  Cliente@Mail.COM ", "cliente@mail.com")]
    public void ValidateAndNormalize_Email_Succeeds(string raw, string expected)
    {
        var result = PixKeyRules.ValidateAndNormalize(PixKeyType.Email, raw);

        Assert.True(result.IsSuccess);
        Assert.Equal(expected, result.Value!.NormalizedKey);
    }

    [Theory]
    [InlineData("+5511987654321", "+5511987654321")]
    [InlineData("11987654321", "+5511987654321")]
    [InlineData("(11) 98765-4321", "+5511987654321")]
    public void ValidateAndNormalize_Phone_Succeeds(string raw, string expected)
    {
        var result = PixKeyRules.ValidateAndNormalize(PixKeyType.Phone, raw);

        Assert.True(result.IsSuccess);
        Assert.Equal(expected, result.Value!.NormalizedKey);
    }

    [Fact]
    public void ValidateAndNormalize_Random_Succeeds()
    {
        var result = PixKeyRules.ValidateAndNormalize(
            PixKeyType.Random,
            "123E4567-E89B-42D3-A456-426614174000");

        Assert.True(result.IsSuccess);
        Assert.Equal("123e4567-e89b-42d3-a456-426614174000", result.Value!.NormalizedKey);
    }

    [Fact]
    public void ValidateAndNormalize_EmptyKey_Fails()
    {
        var result = PixKeyRules.ValidateAndNormalize(PixKeyType.Email, "   ");

        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors, e => e.Code == BankAccountErrorCodes.PixKeyRequired);
    }

    [Fact]
    public void ValidateAndNormalize_InvalidPixKeyType_Fails()
    {
        var result = PixKeyRules.ValidateAndNormalize((PixKeyType)99, "52998224725");

        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors, e => e.Code == BankAccountErrorCodes.PixKeyTypeInvalid);
    }

    [Theory]
    [InlineData(PixKeyType.Cpf, "000.000.000-00")]
    [InlineData(PixKeyType.Cnpj, "11.111.111/1111-11")]
    [InlineData(PixKeyType.Email, "invalid-email")]
    [InlineData(PixKeyType.Phone, "123")]
    [InlineData(PixKeyType.Random, "not-a-uuid")]
    public void ValidateAndNormalize_InvalidValues_Fail(PixKeyType type, string raw)
    {
        var result = PixKeyRules.ValidateAndNormalize(type, raw);

        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors, e => e.Code == BankAccountErrorCodes.PixKeyInvalid);
    }
}

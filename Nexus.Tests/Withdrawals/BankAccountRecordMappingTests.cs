using Nexus.Withdrawals.Aggregates;
using Nexus.Withdrawals.Infrastructure.Mapping;
using Xunit;

namespace Nexus.Tests.Withdrawals;

public sealed class BankAccountRecordMappingTests
{
    [Theory]
    [InlineData(PixKeyType.Cpf, "52998224725")]
    [InlineData(PixKeyType.Email, "conta@example.com")]
    [InlineData(PixKeyType.Phone, "+5511987654321")]
    [InlineData(PixKeyType.Random, "123e4567-e89b-42d3-a456-426614174000")]
    public void RoundTrip_PreservesPixKeyFields(PixKeyType pixKeyType, string pixKey)
    {
        var account = WithdrawalTestFactory.CreateBankAccount(
            id: string.Empty,
            pixKeyType: pixKeyType,
            pixKey: pixKey);

        var record = BankAccountRecordMapping.ToRecord(account);
        var restored = BankAccountRecordMapping.ToBankAccount(record);

        Assert.Equal(account.PixKeyType, restored.PixKeyType);
        Assert.Equal(account.PixKey, restored.PixKey);
        Assert.Equal(account.Agency, restored.Agency);
        Assert.Equal(account.StrawManAccountId, restored.StrawManAccountId);
    }
}

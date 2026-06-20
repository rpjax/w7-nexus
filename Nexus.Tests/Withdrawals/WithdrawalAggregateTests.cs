using Nexus.Withdrawals.Aggregates;
using Nexus.Withdrawals.Errors;
using Xunit;

namespace Nexus.Tests.Withdrawals;

public sealed class WithdrawalAggregateTests
{
    [Fact]
    public void Create_PixWithdrawal_ComputesNetAmount()
    {
        var result = Withdrawal.Create(
            operationId: "op-1",
            type: WithdrawalType.Pix,
            strawManAccountId: "straw-1",
            bankAccountId: "bank-1",
            cryptoWalletId: null,
            paymentIds: new[] { "pay-1", "pay-2" },
            costDescription: "Taxa TED",
            costAmount: 5m,
            pixProof: null,
            cryptoProof: null,
            paymentsTotalAmount: 100m);

        Assert.True(result.IsSuccess);
        Assert.Equal(95m, result.Value!.NetAmount);
        Assert.Equal(100m, result.Value.PaymentsTotalAmount);
        Assert.Equal(WithdrawalType.Pix, result.Value.Type);
    }

    [Fact]
    public void Create_CryptoWithdrawal_RequiresWallet()
    {
        var result = Withdrawal.Create(
            operationId: "op-1",
            type: WithdrawalType.Crypto,
            strawManAccountId: "straw-1",
            bankAccountId: null,
            cryptoWalletId: null,
            paymentIds: new[] { "pay-1" },
            costDescription: null,
            costAmount: 0m,
            pixProof: null,
            cryptoProof: null,
            paymentsTotalAmount: 50m);

        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors, e => e.Code == WithdrawalErrorCodes.CryptoWalletRequired);
    }

    [Fact]
    public void Create_RejectsCostAbovePaymentsTotal()
    {
        var result = Withdrawal.Create(
            operationId: "op-1",
            type: WithdrawalType.Pix,
            strawManAccountId: "straw-1",
            bankAccountId: "bank-1",
            cryptoWalletId: null,
            paymentIds: new[] { "pay-1" },
            costDescription: "Taxa",
            costAmount: 101m,
            pixProof: null,
            cryptoProof: null,
            paymentsTotalAmount: 100m);

        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors, e => e.Code == WithdrawalErrorCodes.CostAmountInvalid);
    }

    [Fact]
    public void PixProof_Create_AllowsOptionalFields()
    {
        var empty = PixProof.Create(null, null);
        Assert.True(empty.IsSuccess);
        Assert.Null(empty.Value);

        var filled = PixProof.Create("e2e-id", "auth-code");
        Assert.True(filled.IsSuccess);
        Assert.Equal("e2e-id", filled.Value!.TransactionId);
        Assert.Equal("auth-code", filled.Value.AuthenticationCode);
    }
}

using Nexus.Payments.Aggregates;
using Nexus.Payments.Errors;
using Xunit;

namespace Nexus.Tests.Payments;

public sealed class PixPaymentAggregateTests
{
    private static Payment CreateSut(
        string operationId = "operation-1",
        PaymentGateway gateway = PaymentGateway.FusionPay,
        string gatewayPaymentId = "gw-1",
        decimal amount = 10m) =>
        new Payment(
            Guid.NewGuid().ToString("N"),
            operationId,
            gateway,
            gatewayPaymentId,
            amount,
            PaymentStatus.Pending,
            operatorAccountId: null,
            strawManAccountId: null,
            DateTime.UtcNow,
            paidAt: null,
            refundedAt: null,
            diedAt: null,
            deathReason: null);

    [Fact]
    public void Constructor_SetsPendingStateAndGatewayFields()
    {
        var p = CreateSut(gateway: PaymentGateway.Frendz, gatewayPaymentId: "ext-1", amount: 55.5m);

        Assert.Equal(PaymentStatus.Pending, p.Status);
        Assert.Equal(PaymentGateway.Frendz, p.Gateway);
        Assert.Equal("ext-1", p.GatewayTransactionId);
        Assert.Equal(55.5m, p.Amount);
        Assert.Equal("operation-1", p.OperationId);
        Assert.Null(p.PaidAt);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public void BindToStrawMan_EmptyId_Fails(string? id)
    {
        var p = CreateSut();

        var result = p.BindToStrawMan(id!);

        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors, e => e.Code == PixPaymentErrorCodes.StrawManInvalid);
    }

    [Fact]
    public void BindToStrawMan_Twice_FailsSecondTime()
    {
        var p = CreateSut();

        Assert.True(p.BindToStrawMan("sm-1").IsSuccess);
        var second = p.BindToStrawMan("sm-2");

        Assert.True(second.IsFailure);
        Assert.Contains(second.Errors, e => e.Code == PixPaymentErrorCodes.StrawManAlreadyBound);
        Assert.Equal("sm-1", p.StrawManAccountId);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public void BindToOperator_EmptyId_Fails(string? id)
    {
        var p = CreateSut();

        var result = p.BindToOperator(id!);

        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors, e => e.Code == PixPaymentErrorCodes.OperatorInvalid);
    }

    [Fact]
    public void BindToOperator_Twice_FailsSecondTime()
    {
        var p = CreateSut();

        Assert.True(p.BindToOperator("op-1").IsSuccess);
        var second = p.BindToOperator("op-2");

        Assert.True(second.IsFailure);
        Assert.Contains(second.Errors, e => e.Code == PixPaymentErrorCodes.OperatorAlreadyBound);
        Assert.Equal("op-1", p.OperatorAccountId);
    }

    [Fact]
    public void MarkAsPaid_WithoutOperator_Fails()
    {
        var p = CreateSut();

        var result = p.MarkAsPaid();

        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors, e => e.Code == PixPaymentErrorCodes.OperatorRequired);
    }

    [Fact]
    public void MarkAsPaid_WhenPendingAndOperatorBound_Succeeds()
    {
        var p = CreateSut();
        Assert.True(p.BindToOperator("op").IsSuccess);

        var result = p.MarkAsPaid();

        Assert.True(result.IsSuccess);
        Assert.Equal(PaymentStatus.Paid, p.Status);
        Assert.NotNull(p.PaidAt);
    }

    [Fact]
    public void MarkAsPaid_WhenAlreadyPaid_FailsInvalidTransition()
    {
        var p = CreateSut();
        Assert.True(p.BindToOperator("op").IsSuccess);
        Assert.True(p.MarkAsPaid().IsSuccess);

        var again = p.MarkAsPaid();

        Assert.True(again.IsFailure);
        Assert.Contains(again.Errors, e => e.Code == PixPaymentErrorCodes.InvalidTransition);
    }

    [Fact]
    public void Refund_WhenPending_FailsInvalidTransition()
    {
        var p = CreateSut();
        Assert.True(p.BindToOperator("op").IsSuccess);

        var result = p.Refund();

        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors, e => e.Code == PixPaymentErrorCodes.InvalidTransition);
    }

    [Fact]
    public void Refund_WhenPaid_Succeeds()
    {
        var p = CreateSut();
        Assert.True(p.BindToOperator("op").IsSuccess);
        Assert.True(p.MarkAsPaid().IsSuccess);

        var result = p.Refund();

        Assert.True(result.IsSuccess);
        Assert.Equal(PaymentStatus.Refunded, p.Status);
        Assert.NotNull(p.RefundedAt);
    }

    [Fact]
    public void Refund_WhenAlreadyRefunded_FailsInvalidTransition()
    {
        var p = CreateSut();
        Assert.True(p.BindToOperator("op").IsSuccess);
        Assert.True(p.MarkAsPaid().IsSuccess);
        Assert.True(p.Refund().IsSuccess);

        var second = p.Refund();

        Assert.True(second.IsFailure);
        Assert.Contains(second.Errors, e => e.Code == PixPaymentErrorCodes.InvalidTransition);
    }

    [Fact]
    public void Die_FromPaid_Succeeds()
    {
        var p = CreateSut();
        Assert.True(p.BindToOperator("op").IsSuccess);
        Assert.True(p.MarkAsPaid().IsSuccess);

        var result = p.Die("disputed");

        Assert.True(result.IsSuccess);
        Assert.Equal(PaymentStatus.Dead, p.Status);
        Assert.Equal("disputed", p.DeathReason);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Die_EmptyReason_Fails(string? reason)
    {
        var p = CreateSut();

        var result = p.Die(reason!);

        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors, e => e.Code == PixPaymentErrorCodes.DeathReasonRequired);
    }

    [Fact]
    public void Die_FromPending_Succeeds()
    {
        var p = CreateSut();

        var result = p.Die("expired");

        Assert.True(result.IsSuccess);
        Assert.Equal(PaymentStatus.Dead, p.Status);
        Assert.Equal("expired", p.DeathReason);
        Assert.NotNull(p.DiedAt);
    }

    [Fact]
    public void Die_WhenAlreadyDead_Fails()
    {
        var p = CreateSut();
        Assert.True(p.Die("first").IsSuccess);

        var second = p.Die("again");

        Assert.True(second.IsFailure);
        Assert.Contains(second.Errors, e => e.Code == PixPaymentErrorCodes.AlreadyDead);
    }

    [Fact]
    public void BindToGateway_ValidInput_UpdatesGatewayTransactionId()
    {
        var p = CreateSut(gateway: PaymentGateway.Frendz, gatewayPaymentId: "placeholder");

        var result = p.BindToGateway(PaymentGateway.Frendz, "frendz-trx-99");

        Assert.True(result.IsSuccess);
        Assert.Equal(PaymentGateway.Frendz, p.Gateway);
        Assert.Equal("frendz-trx-99", p.GatewayTransactionId);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public void BindToGateway_EmptyTransactionId_Fails(string? tx)
    {
        var p = CreateSut();

        var result = p.BindToGateway(PaymentGateway.Frendz, tx!);

        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors, e => e.Code == PixPaymentErrorCodes.GatewayPaymentIdInvalid);
    }

    [Fact]
    public void PaymentGateway_Enum_HasExpectedMembers()
    {
        Assert.Equal(0, (int)PaymentGateway.None);
        Assert.Equal(PaymentGateway.FusionPay, Enum.Parse<PaymentGateway>("FusionPay"));
        Assert.Equal(PaymentGateway.Frendz, Enum.Parse<PaymentGateway>("Frendz"));
        Assert.Equal(PaymentGateway.Wintech, Enum.Parse<PaymentGateway>("Wintech"));
    }
}

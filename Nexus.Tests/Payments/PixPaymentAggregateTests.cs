using Nexus.Gateways.Application.Models;
using Nexus.Payments.Aggregates;
using Nexus.Payments.Errors;
using Xunit;

namespace Nexus.Tests.Payments;

public sealed class PixPaymentAggregateTests
{
    private static readonly IReadOnlyList<PaymentSplit> DefaultSplits =
        PaymentSplit.AllocateFromCuts(10m, new[] { ("op-1", 100m) });

    private static Payment CreateSut(
        string operationId = "operation-1",
        PaymentGateway gateway = PaymentGateway.FusionPay,
        string gatewayPaymentId = "gw-1",
        decimal amount = 10m,
        IReadOnlyList<PaymentSplit>? splits = null,
        PaymentStatus status = PaymentStatus.Pending,
        PaymentSettlementStatus settlementStatus = PaymentSettlementStatus.Unsettled) =>
        PaymentTestFactory.Create(
            operationId: operationId,
            gateway: gateway,
            gatewayPaymentId: gatewayPaymentId,
            amount: amount,
            splits: splits ?? DefaultSplits,
            status: status,
            settlementStatus: settlementStatus);

    private static void BindForPaid(Payment p)
    {
        Assert.True(p.BindToOperator("op").IsSuccess);
    }

    [Fact]
    public void Constructor_SetsPendingStateAndGatewayFields()
    {
        var p = CreateSut(
            gateway: PaymentGateway.Frendz,
            gatewayPaymentId: "ext-1",
            amount: 55.5m,
            splits: PaymentSplit.AllocateFromCuts(55.5m, new[] { ("op-1", 100m) }));

        Assert.Equal(PaymentStatus.Pending, p.Status);
        Assert.Equal(PaymentGateway.Frendz, p.Gateway);
        Assert.Equal("ext-1", p.GatewayTransactionId);
        Assert.Equal(55.5m, p.Amount);
        Assert.Equal("operation-1", p.OperationId);
        Assert.Single(p.Splits);
        Assert.Equal(55.5m, p.Splits.Sum(split => split.Amount));
        Assert.Equal(PaymentSettlementStatus.Unsettled, p.SettlementStatus);
        Assert.Equal(PaymentDistributionStatus.Pending, p.DistributionStatus);
        Assert.Null(p.PaidAt);
    }

    [Fact]
    public void AllocateFromCuts_LastCutReceivesRemainder()
    {
        var splits = PaymentSplit.AllocateFromCuts(100m, new[]
        {
            ("a", 33.33m),
            ("b", 33.33m),
            ("c", 33.34m),
        });

        Assert.Equal(100m, splits.Sum(split => split.Amount));
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
    public void BindToStrawMan_WhenUnset_BindsSuccessfully()
    {
        var p = PaymentTestFactory.Create(strawManId: string.Empty);

        Assert.True(p.BindToStrawMan("sm-1").IsSuccess);
        Assert.Equal("sm-1", p.StrawManId);
    }

    [Fact]
    public void BindToStrawMan_Twice_FailsSecondTime()
    {
        var p = CreateSut();

        var second = p.BindToStrawMan("sm-2");

        Assert.True(second.IsFailure);
        Assert.Contains(second.Errors, e => e.Code == PixPaymentErrorCodes.StrawManAlreadyBound);
        Assert.Equal("sm-1", p.StrawManId);
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
        Assert.Equal("op-1", p.OperatorId);
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
    public void MarkAsPaid_WithoutSplits_Fails()
    {
        var p = CreateSut(splits: Array.Empty<PaymentSplit>());
        BindForPaid(p);

        var result = p.MarkAsPaid();

        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors, e => e.Code == PixPaymentErrorCodes.SplitsRequired);
    }

    [Fact]
    public void MarkAsPaid_WhenPendingAndOperatorBound_Succeeds()
    {
        var p = CreateSut();
        BindForPaid(p);

        var result = p.MarkAsPaid();

        Assert.True(result.IsSuccess);
        Assert.Equal(PaymentStatus.Paid, p.Status);
        Assert.NotNull(p.PaidAt);
        Assert.Equal(PaymentSettlementStatus.Unsettled, p.SettlementStatus);
    }

    [Fact]
    public void MarkAsPaid_WhenAlreadyPaid_FailsInvalidTransition()
    {
        var p = CreateSut();
        BindForPaid(p);
        Assert.True(p.MarkAsPaid().IsSuccess);

        var again = p.MarkAsPaid();

        Assert.True(again.IsFailure);
        Assert.Contains(again.Errors, e => e.Code == PixPaymentErrorCodes.InvalidTransition);
    }

    [Fact]
    public void MarkAsWithdrawn_WhenPending_Fails()
    {
        var p = CreateSut();
        Assert.True(p.BindToOperator("op").IsSuccess);

        var result = p.MarkAsWithdrawn();

        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors, e => e.Code == PixPaymentErrorCodes.InvalidSettlementTransition);
    }

    [Fact]
    public void MarkAsWithdrawn_WhenPaid_Succeeds()
    {
        var p = CreateSut();
        BindForPaid(p);
        Assert.True(p.MarkAsPaid().IsSuccess);

        var result = p.MarkAsWithdrawn();

        Assert.True(result.IsSuccess);
        Assert.Equal(PaymentSettlementStatus.Withdrawn, p.SettlementStatus);
        Assert.NotNull(p.WithdrawnAt);
    }

    [Fact]
    public void MarkAsWithdrawn_WhenAlreadyWithdrawn_Fails()
    {
        var p = CreateSut();
        BindForPaid(p);
        Assert.True(p.MarkAsPaid().IsSuccess);
        Assert.True(p.MarkAsWithdrawn().IsSuccess);

        var second = p.MarkAsWithdrawn();

        Assert.True(second.IsFailure);
        Assert.Contains(second.Errors, e => e.Code == PixPaymentErrorCodes.AlreadyWithdrawn);
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
        BindForPaid(p);
        Assert.True(p.MarkAsPaid().IsSuccess);

        var result = p.Refund();

        Assert.True(result.IsSuccess);
        Assert.Equal(PaymentStatus.Refunded, p.Status);
        Assert.NotNull(p.RefundedAt);
    }

    [Fact]
    public void Refund_WhenWithdrawn_Fails()
    {
        var p = CreateSut();
        BindForPaid(p);
        Assert.True(p.MarkAsPaid().IsSuccess);
        Assert.True(p.MarkAsWithdrawn().IsSuccess);

        var result = p.Refund();

        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors, e => e.Code == PixPaymentErrorCodes.InvalidTransition);
    }

    [Fact]
    public void Refund_WhenAlreadyRefunded_FailsInvalidTransition()
    {
        var p = CreateSut();
        BindForPaid(p);
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
        BindForPaid(p);
        Assert.True(p.MarkAsPaid().IsSuccess);

        var result = p.Kill("disputed");

        Assert.True(result.IsSuccess);
        Assert.Equal(PaymentStatus.Killed, p.Status);
        Assert.Equal("disputed", p.KillReason);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Die_EmptyReason_Fails(string? reason)
    {
        var p = CreateSut();

        var result = p.Kill(reason!);

        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors, e => e.Code == PixPaymentErrorCodes.KillReasonRequired);
    }

    [Fact]
    public void Die_FromPending_Succeeds()
    {
        var p = CreateSut();

        var result = p.Kill("expired");

        Assert.True(result.IsSuccess);
        Assert.Equal(PaymentStatus.Killed, p.Status);
        Assert.Equal("expired", p.KillReason);
        Assert.NotNull(p.KilledAt);
    }

    [Fact]
    public void Kill_WhenAlreadyKilled_Fails()
    {
        var p = CreateSut();
        Assert.True(p.Kill("first").IsSuccess);

        var second = p.Kill("again");

        Assert.True(second.IsFailure);
        Assert.Contains(second.Errors, e => e.Code == PixPaymentErrorCodes.AlreadyKilled);
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
    public void MarkAsDistributed_WhenPaidAndWithdrawn_Succeeds()
    {
        var p = CreateSut(status: PaymentStatus.Paid, settlementStatus: PaymentSettlementStatus.Withdrawn);
        BindForPaid(p);

        var result = p.MarkAsDistributed();

        Assert.True(result.IsSuccess);
        Assert.Equal(PaymentDistributionStatus.Complete, p.DistributionStatus);
        Assert.NotNull(p.DistributedAt);
    }

    [Fact]
    public void MarkAsDistributed_WhenUnsettled_Fails()
    {
        var p = PaymentTestFactory.Create(
            status: PaymentStatus.Paid,
            settlementStatus: PaymentSettlementStatus.Unsettled,
            operatorId: "op",
            paidAt: DateTime.UtcNow);

        var result = p.MarkAsDistributed();

        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors, e => e.Code == PixPaymentErrorCodes.DistributionRequiresWithdrawal);
    }

    [Fact]
    public void MarkAsDistributed_WhenPendingPayment_Fails()
    {
        var p = CreateSut();
        BindForPaid(p);

        var result = p.MarkAsDistributed();

        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors, e => e.Code == PixPaymentErrorCodes.InvalidDistributionTransition);
    }

    [Fact]
    public void MarkAsDistributed_WhenAlreadyComplete_Fails()
    {
        var p = CreateSut(status: PaymentStatus.Paid, settlementStatus: PaymentSettlementStatus.Withdrawn);
        BindForPaid(p);
        Assert.True(p.MarkAsDistributed().IsSuccess);

        var second = p.MarkAsDistributed();

        Assert.True(second.IsFailure);
        Assert.Contains(second.Errors, e => e.Code == PixPaymentErrorCodes.AlreadyDistributed);
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

using Nexus.Authorization;
using Nexus.Authorization.Application.Models;
using Nexus.Authorization.Errors;
using Nexus.Payments.Application.Models;
using Nexus.Payments.Errors;
using Nexus.Tests.Support;
using Xunit;

namespace Nexus.Tests.Payments;

public sealed class OperatorTests
{
    private readonly ActorTestContext _ctx = new();

    private RequesterIdentity Identity(string accountId = "operator-1")
        => _ctx.CreateRequesterIdentity(accountId, additionalRoles: Roles.Operator);

    [Fact]
    public async Task SearchPaymentsAsync_WithoutOperatorRole_ReturnsUnauthorized()
    {
        var sut = _ctx.CreatePaymentsOperator();
        var identity = _ctx.CreateRequesterIdentity("regular-user");

        var result = await sut.SearchPaymentsAsync(identity, new SearchPaymentsRequest { Limit = 20 });

        Assert.False(result.IsAuthorized);
        Assert.Contains(result.AuthorizationErrors, e => e.Code == AuthorizationErrorCodes.NotOperator);
    }

    [Fact]
    public async Task GetPaymentAsync_WithoutOperatorRole_ReturnsUnauthorized()
    {
        var sut = _ctx.CreatePaymentsOperator();
        var identity = _ctx.CreateRequesterIdentity("regular-user");

        var result = await sut.GetPaymentAsync(identity, "pay-1");

        Assert.False(result.IsAuthorized);
        Assert.Contains(result.AuthorizationErrors, e => e.Code == AuthorizationErrorCodes.NotOperator);
    }

    [Fact]
    public async Task GetPaymentAsync_EmptyPaymentId_ReturnsValidationError()
    {
        var sut = _ctx.CreatePaymentsOperator();

        var result = await sut.GetPaymentAsync(Identity(), string.Empty);

        Assert.True(result.IsAuthorized);
        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors, e => e.Code == PixPaymentErrorCodes.PaymentIdInvalid);
    }

    [Fact]
    public async Task GetPaymentAsync_PaymentNotFound_ReturnsNotFoundError()
    {
        var sut = _ctx.CreatePaymentsOperator();

        var result = await sut.GetPaymentAsync(Identity(), "missing-pay");

        Assert.True(result.IsAuthorized);
        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors, e => e.Code == PixPaymentErrorCodes.PaymentNotFound);
    }

    [Fact]
    public async Task GetPaymentAsync_OperatorAssignedToPayment_ReturnsPayment()
    {
        var operation = await _ctx.SeedOperationAsync();
        await _ctx.SeedPaymentAsync(operation.Id, operatorId: "operator-1", id: "pay-mine");
        var sut = _ctx.CreatePaymentsOperator();

        var result = await sut.GetPaymentAsync(Identity(), "pay-mine");

        Assert.True(result.IsAuthorized);
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal("pay-mine", result.Value.Id);
    }
}

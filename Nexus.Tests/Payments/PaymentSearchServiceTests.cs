using Nexus.Accounts.Aggregates;
using Nexus.Authorization;
using Nexus.Authorization.Application.Models;
using Nexus.Payments.Aggregates;
using Nexus.Payments.Application.Models;
using Nexus.Payments.Application.Services;
using Nexus.Payments.Errors;
using Nexus.Tests.Accounts;
using Nexus.Tests.Payments;
using Nexus.Tests.Support;
using Xunit;

namespace Nexus.Tests.Payments;

public sealed class PaymentSearchServiceTests
{
    private static RequesterIdentity OperatorIdentity(string accountId = "operator-1") =>
        new(accountId, new[] { "Operator" }, Array.Empty<string>());

    private static RequesterIdentity StrawManIdentity(string accountId = "straw-1") =>
        new(accountId, new[] { "StrawMan" }, Array.Empty<string>());

    [Fact]
    public async Task OperatorSearch_ReturnsOnlyScopedPayments()
    {
        var payments = new InMemoryPaymentRepository();
        var teams = new InMemoryTeamRepository();
        await teams.CreateAsync(TeamTestFactory.WithOperatorProfitShare("team-1", "operation-1", "operator-1", "straw-1", ("operator-1", 100m)));

        await payments.CreateAsync(PaymentTestFactory.Create(id: "pay-operator", operatorId: "operator-1"));
        await payments.CreateAsync(PaymentTestFactory.Create(
            id: "pay-team",
            operationId: "operation-1",
            operatorId: "operator-1",
            strawManId: "straw-1"));
        await payments.CreateAsync(PaymentTestFactory.Create(
            id: "pay-split",
            splits: new[] { new PaymentSplit("operator-1", 100m, 10m) }));
        await payments.CreateAsync(PaymentTestFactory.Create(id: "pay-other", operatorId: "operator-2"));

        var sut = new Nexus.Operators.Application.Services.OperatorPaymentSearchService(payments, teams);
        var result = await sut.SearchPaymentsAsync(OperatorIdentity(), new SearchPaymentsRequest { Limit = 50 });

        Assert.True(result.IsSuccess);
        var ids = result.Value!.Items.Select(item => item.Id).OrderBy(id => id).ToArray();
        Assert.Equal(new[] { "pay-operator", "pay-split", "pay-team" }, ids);
    }

    [Fact]
    public async Task StrawManSearch_ReturnsOnlyScopedPayments()
    {
        var payments = new InMemoryPaymentRepository();
        await payments.CreateAsync(PaymentTestFactory.Create(id: "pay-mine", strawManId: "straw-1"));
        await payments.CreateAsync(PaymentTestFactory.Create(id: "pay-other", strawManId: "straw-2"));

        var sut = new Nexus.StrawMen.Application.Services.StrawManPaymentSearchService(payments);
        var result = await sut.SearchPaymentsAsync(StrawManIdentity(), new SearchPaymentsRequest { Limit = 50 });

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value!.Items);
        Assert.Equal("pay-mine", result.Value.Items[0].Id);
    }

    [Fact]
    public async Task AdminSearch_NullRequest_UsesDefaults()
    {
        var payments = new InMemoryPaymentRepository();
        await payments.CreateAsync(PaymentTestFactory.Create(id: "pay-a", status: PaymentStatus.Paid, paidAt: DateTime.UtcNow));

        var sut = new Nexus.Administrators.Application.Services.AdministratorPaymentSearchService(payments);
        var result = await sut.SearchPaymentsAsync(null);

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value!.Items);
    }

    [Fact]
    public async Task AdminSearch_AppliesStatusAndOperationFilters()
    {
        var payments = new InMemoryPaymentRepository();
        await payments.CreateAsync(PaymentTestFactory.Create(id: "pay-a", operationId: "op-1", status: PaymentStatus.Pending));
        await payments.CreateAsync(PaymentTestFactory.Create(id: "pay-b", operationId: "op-1", status: PaymentStatus.Paid, paidAt: DateTime.UtcNow));
        await payments.CreateAsync(PaymentTestFactory.Create(id: "pay-c", operationId: "op-2", status: PaymentStatus.Pending));

        var sut = new Nexus.Administrators.Application.Services.AdministratorPaymentSearchService(payments);
        var result = await sut.SearchPaymentsAsync(new SearchPaymentsRequest
        {
            Limit = 50,
            OperationId = "op-1",
            Status = PaymentStatus.Pending,
        });

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value!.Items);
        Assert.Equal("pay-a", result.Value.Items[0].Id);
    }

    [Fact]
    public async Task AdminPay_FailsWhenPendingWithoutOperator()
    {
        var payments = new InMemoryPaymentRepository();
        await payments.CreateAsync(PaymentTestFactory.Create(
            id: "pay-pending",
            operatorId: null,
            splits: Array.Empty<PaymentSplit>()));

        var sut = new PaymentService(
            new InMemoryAccountRepository(),
            payments,
            new InMemoryOperationRepository(),
            new InMemoryTeamRepository());

        var result = await sut.PayAsync("pay-pending");

        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors, error => error.Code == PixPaymentErrorCodes.OperatorRequired);
    }

    [Fact]
    public async Task AdminPay_SucceedsWhenAggregateAllows()
    {
        var ctx = new ActorTestContext();
        await ctx.Accounts.CreateAsync(new Account(
            "operator-1",
            "operator",
            "hash",
            Array.Empty<string>(),
            Array.Empty<string>(),
            DateTime.UtcNow,
            DateTime.UtcNow));
        await ctx.Accounts.CreateAsync(new Account(
            "straw-1",
            "straw",
            "hash",
            new[] { Roles.StrawMan },
            Array.Empty<string>(),
            DateTime.UtcNow,
            DateTime.UtcNow));
        await ctx.Payments.CreateAsync(PaymentTestFactory.Create(
            id: "pay-ready",
            operatorId: "operator-1",
            strawManId: "straw-1",
            splits: new[] { new PaymentSplit("operator-1", 100m, 10m) }));

        var sut = new PaymentService(
            ctx.Accounts,
            ctx.Payments,
            ctx.Operations,
            ctx.Teams);

        var result = await sut.PayAsync("pay-ready");

        Assert.True(result.IsSuccess);
        var updated = ctx.Payments.AsQueryable().First(p => p.Id == "pay-ready");
        Assert.Equal(PaymentStatus.Paid, updated.Status);
    }
}

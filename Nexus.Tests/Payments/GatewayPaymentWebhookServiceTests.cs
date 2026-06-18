using System.Linq.Expressions;
using Nexus.Payments.Application.Contracts;
using Aidan.Core.Linq;
using Aidan.Core.Patterns;
using Microsoft.Extensions.Logging.Abstractions;
using Nexus.Payments.Aggregates;
using Nexus.Payments.Application.Services;
using Nexus.Payments.Application.Models;
using Xunit;

namespace Nexus.Tests.Payments;

public sealed class GatewayPaymentWebhookServiceTests
{
    private sealed class StubPaymentRepository : IPaymentRepository
    {
        private readonly List<Payment> _payments;

        public StubPaymentRepository(params Payment[] payments) => _payments = payments.ToList();

        public IAsyncQueryable<Payment> AsQueryable()
            => new QueryableToAsyncQueryableAdapter<Payment>(_payments.AsQueryable());

        public Task<Payment> CreateAsync(Payment entity)
        {
            var persisted = string.IsNullOrWhiteSpace(entity.Id)
                ? PaymentTestFactory.Create(
                    operationId: entity.OperationId,
                    teamId: entity.TeamId,
                    gateway: entity.Gateway,
                    gatewayPaymentId: entity.GatewayTransactionId,
                    amount: entity.Amount,
                    splits: entity.Splits,
                    status: entity.Status,
                    settlementStatus: entity.SettlementStatus,
                    operatorAccountId: entity.OperatorAccountId,
                    strawManAccountId: entity.StrawManAccountId,
                    createdAt: entity.CreatedAt,
                    paidAt: entity.PaidAt,
                    refundedAt: entity.RefundedAt,
                    diedAt: entity.DiedAt,
                    deathReason: entity.DeathReason,
                    withdrawnAt: entity.WithdrawnAt)
                : entity;

            return Task.FromResult(persisted);
        }

        async Task IRepository<Payment>.CreateAsync(Payment entity)
        {
            await CreateAsync(entity);
        }

        public Task CreateAsync(IEnumerable<Payment> entities) => Task.CompletedTask;
        public Task DeleteAsync(Payment entity) => Task.CompletedTask;
        public Task<long> DeleteAsync(Expression<Func<Payment, bool>> predicate) => Task.FromResult(0L);
        public Task UpdateAsync(Payment entity) => Task.CompletedTask;
        public Task<long> UpdateAsync(Expression expression) => Task.FromResult(0L);
    }

    private sealed class TrackingPaymentService : IPaymentService
    {
        public List<string> PayCalls { get; } = new();
        public List<string> RefundCalls { get; } = new();
        public List<(string PaymentId, string Reason)> KillCalls { get; } = new();

        public Task<IResult<Payment>> CreatePaymentAsync(CreatePaymentRequest request) =>
            throw new NotSupportedException();

        public Task<IResult> DeletePaymentAsync(string paymentId) =>
            throw new NotSupportedException();

        public Task<IResult> PayAsync(string paymentId)
        {
            PayCalls.Add(paymentId);
            return Task.FromResult<IResult>(Result.Success());
        }

        public Task<IResult> RefundAsync(string paymentId)
        {
            RefundCalls.Add(paymentId);
            return Task.FromResult<IResult>(Result.Success());
        }

        public Task<IResult> KillAsync(string paymentId, string reason)
        {
            KillCalls.Add((paymentId, reason));
            return Task.FromResult<IResult>(Result.Success());
        }

        public Task<IResult> MarkAsWithdrawnAsync(string paymentId) =>
            Task.FromResult<IResult>(Result.Success());
    }

    private static Payment CreatePayment(
        string id,
        PaymentGateway gateway,
        string gatewayTransactionId) =>
        PaymentTestFactory.Create(
            id: id,
            gateway: gateway,
            gatewayPaymentId: gatewayTransactionId);

    private static GatewayPaymentWebhookService CreateSut(
        StubPaymentRepository? payments = null,
        TrackingPaymentService? paymentService = null) =>
        new(
            payments ?? new StubPaymentRepository(),
            paymentService ?? new TrackingPaymentService(),
            NullLogger<GatewayPaymentWebhookService>.Instance);

    [Fact]
    public async Task ProcessFrendzPostbackAsync_InvalidJson_DoesNotThrow()
    {
        var paymentService = new TrackingPaymentService();
        var sut = CreateSut(paymentService: paymentService);

        await sut.ProcessFrendzPostbackAsync("{ not valid json");

        Assert.Empty(paymentService.PayCalls);
        Assert.Empty(paymentService.RefundCalls);
        Assert.Empty(paymentService.KillCalls);
    }

    [Fact]
    public async Task ProcessFrendzPostbackAsync_MissingHash_DoesNotCallPaymentService()
    {
        var paymentService = new TrackingPaymentService();
        var sut = CreateSut(paymentService: paymentService);

        await sut.ProcessFrendzPostbackAsync("""{"status":"paid"}""");

        Assert.Empty(paymentService.PayCalls);
        Assert.Empty(paymentService.RefundCalls);
        Assert.Empty(paymentService.KillCalls);
    }

    [Fact]
    public async Task ProcessFrendzPostbackAsync_PaymentNotFound_DoesNotCallPaymentService()
    {
        var paymentService = new TrackingPaymentService();
        var sut = CreateSut(new StubPaymentRepository(), paymentService);

        await sut.ProcessFrendzPostbackAsync("""{"transaction_hash":"missing-hash","status":"paid"}""");

        Assert.Empty(paymentService.PayCalls);
        Assert.Empty(paymentService.RefundCalls);
        Assert.Empty(paymentService.KillCalls);
    }

    [Fact]
    public async Task ProcessFrendzPostbackAsync_PaidStatus_CallsPayAsync()
    {
        var payment = CreatePayment("pay-1", PaymentGateway.Frendz, "hash-abc");
        var paymentService = new TrackingPaymentService();
        var sut = CreateSut(new StubPaymentRepository(payment), paymentService);

        await sut.ProcessFrendzPostbackAsync("""{"transaction_hash":"hash-abc","status":"paid"}""");

        Assert.Equal(["pay-1"], paymentService.PayCalls);
        Assert.Empty(paymentService.RefundCalls);
        Assert.Empty(paymentService.KillCalls);
    }

    [Fact]
    public async Task ProcessFrendzPostbackAsync_RefundedStatus_CallsRefundAsync()
    {
        var payment = CreatePayment("pay-2", PaymentGateway.Frendz, "hash-ref");
        var paymentService = new TrackingPaymentService();
        var sut = CreateSut(new StubPaymentRepository(payment), paymentService);

        await sut.ProcessFrendzPostbackAsync("""{"transaction_hash":"hash-ref","status":"refunded"}""");

        Assert.Empty(paymentService.PayCalls);
        Assert.Equal(["pay-2"], paymentService.RefundCalls);
        Assert.Empty(paymentService.KillCalls);
    }

    [Fact]
    public async Task ProcessFrendzPostbackAsync_CanceledStatus_CallsKillAsync()
    {
        var payment = CreatePayment("pay-3", PaymentGateway.Frendz, "hash-cancel");
        var paymentService = new TrackingPaymentService();
        var sut = CreateSut(new StubPaymentRepository(payment), paymentService);

        await sut.ProcessFrendzPostbackAsync("""{"transaction_hash":"hash-cancel","status":"canceled"}""");

        Assert.Empty(paymentService.PayCalls);
        Assert.Empty(paymentService.RefundCalls);
        Assert.Single(paymentService.KillCalls);
        Assert.Equal("pay-3", paymentService.KillCalls[0].PaymentId);
        Assert.Contains("canceled", paymentService.KillCalls[0].Reason);
    }

    [Fact]
    public async Task ProcessStandardGatewayWebhookAsync_UnsupportedGateway_DoesNotCallPaymentService()
    {
        var payment = CreatePayment("pay-std", PaymentGateway.SigiloPay, "tx-1");
        var paymentService = new TrackingPaymentService();
        var sut = CreateSut(new StubPaymentRepository(payment), paymentService);

        await sut.ProcessStandardGatewayWebhookAsync(
            PaymentGateway.Frendz,
            """{"event":"TRANSACTION_PAID","transaction":{"id":"tx-1","status":"COMPLETED"}}""");

        Assert.Empty(paymentService.PayCalls);
        Assert.Empty(paymentService.RefundCalls);
        Assert.Empty(paymentService.KillCalls);
    }

    [Fact]
    public async Task ProcessStandardGatewayWebhookAsync_TransactionPaidCompleted_CallsPayAsync()
    {
        var payment = CreatePayment("pay-sp", PaymentGateway.SigiloPay, "gw-tx-99");
        var paymentService = new TrackingPaymentService();
        var sut = CreateSut(new StubPaymentRepository(payment), paymentService);

        await sut.ProcessStandardGatewayWebhookAsync(
            PaymentGateway.SigiloPay,
            """
            {
              "event": "TRANSACTION_PAID",
              "transaction": { "id": "gw-tx-99", "status": "COMPLETED" }
            }
            """);

        Assert.Equal(["pay-sp"], paymentService.PayCalls);
        Assert.Empty(paymentService.RefundCalls);
        Assert.Empty(paymentService.KillCalls);
    }

    [Fact]
    public async Task ProcessStandardGatewayWebhookAsync_TransactionCreated_Ignored()
    {
        var payment = CreatePayment("pay-created", PaymentGateway.Wintech, "gw-created");
        var paymentService = new TrackingPaymentService();
        var sut = CreateSut(new StubPaymentRepository(payment), paymentService);

        await sut.ProcessStandardGatewayWebhookAsync(
            PaymentGateway.Wintech,
            """
            {
              "event": "TRANSACTION_CREATED",
              "transaction": { "id": "gw-created", "status": "PENDING" }
            }
            """);

        Assert.Empty(paymentService.PayCalls);
        Assert.Empty(paymentService.RefundCalls);
        Assert.Empty(paymentService.KillCalls);
    }
}


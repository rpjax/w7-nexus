using System.Linq.Expressions;
using Aidan.Core.Linq;
using Aidan.Core.Patterns;
using Aidan.Mongo.Linq;
using Nexus.Charges.Application;
using Nexus.Charges.Application.Models;
using Nexus.Charges.Infrastructure;
using Nexus.Frendz.Application;
using Nexus.Frendz.Application.Models;
using Nexus.Operations.Aggregates;
using Nexus.Operations.Application;
using Nexus.Payments.Aggregates;
using Nexus.Payments.Application;
using Nexus.Payments.Application.Models;
using Nexus.Payments.ErrorCodes;
using Xunit;

namespace Nexus.Tests.Charges;

public sealed class ChargeOrchestratorTests
{
    [Fact]
    public async Task CreatePixChargeAsync_WhenOperationMissing_ReturnsFailure()
    {
        var sut = new ChargeOrchestrator(
            new EmptyOperationRepository(),
            new StubPaymentService(),
            new StubPaymentRepository(),
            new EmptyFrendzCredentialsRepository(),
            new StubChargeServiceFactory(new StubChargeService()));

        var result = await sut.CreatePixChargeAsync(new CreatePixChargeRequest
        {
            OperationId = "missing",
            Amount = 10m
        });

        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors, e => e.Code == PixPaymentErrorCodes.OperationNotFound);
    }

    [Fact]
    public async Task CreatePixChargeAsync_WhenChargeSucceeds_ReturnsPixChargeAndUpdatesPayment()
    {
        var operation = new Operation(
            "op-1",
            "N",
            "D",
            Array.Empty<string>(),
            Array.Empty<string>(),
            DateTime.UtcNow,
            DateTime.UtcNow);

        var cred = new FrendzApiCredentials { Id = "1", Name = "c", Token = "tok" };
        var paymentRepo = new StubPaymentRepository();
        var chargeService = new StubChargeService
        {
            OnCreate = r => Task.FromResult(new PixCharge
            {
                Id = r.PaymentId,
                Code = "pix-code",
                GatewayTransactionId = "trx-1"
            })
        };

        var sut = new ChargeOrchestrator(
            new SingleOperationRepository(operation),
            new StubPaymentService(),
            paymentRepo,
            new SingleFrendzCredentialsRepository(cred),
            new StubChargeServiceFactory(chargeService));

        var result = await sut.CreatePixChargeAsync(new CreatePixChargeRequest
        {
            OperationId = "op-1",
            Amount = 10m
        });

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal("pix-code", result.Value!.Code);
        Assert.True(paymentRepo.WasUpdated);
    }

    private sealed class StubPaymentService : IPaymentService
    {
        public Task<IResult<Payment>> CreatePaymentAsync(CreatePaymentRequest request)
        {
            var id = string.IsNullOrWhiteSpace(request.ExplicitPaymentId)
                ? "pay-1"
                : request.ExplicitPaymentId!.Trim();
            var payment = new Payment(
                id,
                request.OperationId!,
                request.Gateway,
                request.GatewayPaymentId!,
                request.Amount,
                PaymentStatus.Pending,
                operatorAccountId: null,
                strawManAccountId: null,
                DateTime.UtcNow,
                paidAt: null,
                refundedAt: null,
                diedAt: null,
                deathReason: null);
            IResult<Payment> ok = Result.Create<Payment>().WithValue(payment).Build();
            return Task.FromResult(ok);
        }

        public Task<IResult> DeletePaymentAsync(string paymentId) =>
            Task.FromResult<IResult>(Result.Success());

        public Task<IResult> PayAsync(string paymentId) =>
            Task.FromResult<IResult>(Result.Success());

        public Task<IResult> RefundAsync(string paymentId) =>
            Task.FromResult<IResult>(Result.Success());

        public Task<IResult> KillAsync(string paymentId, string reason) =>
            Task.FromResult<IResult>(Result.Success());
    }

    private sealed class StubPaymentRepository : IPaymentRepository
    {
        public bool WasUpdated { get; private set; }

        public IAsyncQueryable<Payment> AsQueryable() =>
            new MongoAsyncQueryable<Payment>(Array.Empty<Payment>().AsQueryable());

        public Task CreateAsync(Payment entity) => Task.CompletedTask;
        public Task CreateAsync(IEnumerable<Payment> entities) => Task.CompletedTask;
        public Task DeleteAsync(Payment entity) => Task.CompletedTask;
        public Task<long> DeleteAsync(Expression<Func<Payment, bool>> predicate) => Task.FromResult(0L);

        public Task UpdateAsync(Payment entity)
        {
            WasUpdated = true;
            return Task.CompletedTask;
        }

        public Task<long> UpdateAsync(Expression expression) => Task.FromResult(0L);
    }

    private sealed class EmptyOperationRepository : IOperationRepository
    {
        public IAsyncQueryable<Operation> AsQueryable() =>
            new MongoAsyncQueryable<Operation>(Array.Empty<Operation>().AsQueryable());

        public Task CreateAsync(Operation entity) => Task.CompletedTask;
        public Task CreateAsync(IEnumerable<Operation> entities) => Task.CompletedTask;
        public Task DeleteAsync(Operation entity) => Task.CompletedTask;
        public Task<long> DeleteAsync(Expression<Func<Operation, bool>> predicate) => Task.FromResult(0L);
        public Task UpdateAsync(Operation entity) => Task.CompletedTask;
        public Task<long> UpdateAsync(Expression expression) => Task.FromResult(0L);
    }

    private sealed class SingleOperationRepository : IOperationRepository
    {
        private readonly Operation _operation;

        public SingleOperationRepository(Operation operation) => _operation = operation;

        public IAsyncQueryable<Operation> AsQueryable() =>
            new MongoAsyncQueryable<Operation>(new[] { _operation }.AsQueryable());

        public Task CreateAsync(Operation entity) => Task.CompletedTask;
        public Task CreateAsync(IEnumerable<Operation> entities) => Task.CompletedTask;
        public Task DeleteAsync(Operation entity) => Task.CompletedTask;
        public Task<long> DeleteAsync(Expression<Func<Operation, bool>> predicate) => Task.FromResult(0L);
        public Task UpdateAsync(Operation entity) => Task.CompletedTask;
        public Task<long> UpdateAsync(Expression expression) => Task.FromResult(0L);
    }

    private sealed class EmptyFrendzCredentialsRepository : IFrendzApiCredentialsRepository
    {
        public IAsyncQueryable<FrendzApiCredentials> AsQueryable() =>
            new MongoAsyncQueryable<FrendzApiCredentials>(Array.Empty<FrendzApiCredentials>().AsQueryable());

        public Task CreateAsync(FrendzApiCredentials entity) => throw new NotSupportedException();
        public Task CreateAsync(IEnumerable<FrendzApiCredentials> entities) => throw new NotSupportedException();
        public Task DeleteAsync(FrendzApiCredentials entity) => throw new NotSupportedException();
        public Task<long> DeleteAsync(Expression<Func<FrendzApiCredentials, bool>> predicate) => throw new NotSupportedException();
        public Task UpdateAsync(FrendzApiCredentials entity) => throw new NotSupportedException();
        public Task<long> UpdateAsync(Expression expression) => throw new NotSupportedException();
    }

    private sealed class SingleFrendzCredentialsRepository : IFrendzApiCredentialsRepository
    {
        private readonly FrendzApiCredentials _credential;

        public SingleFrendzCredentialsRepository(FrendzApiCredentials credential) => _credential = credential;

        public IAsyncQueryable<FrendzApiCredentials> AsQueryable() =>
            new MongoAsyncQueryable<FrendzApiCredentials>(new[] { _credential }.AsQueryable());

        public Task CreateAsync(FrendzApiCredentials entity) => throw new NotSupportedException();
        public Task CreateAsync(IEnumerable<FrendzApiCredentials> entities) => throw new NotSupportedException();
        public Task DeleteAsync(FrendzApiCredentials entity) => throw new NotSupportedException();
        public Task<long> DeleteAsync(Expression<Func<FrendzApiCredentials, bool>> predicate) => throw new NotSupportedException();
        public Task UpdateAsync(FrendzApiCredentials entity) => throw new NotSupportedException();
        public Task<long> UpdateAsync(Expression expression) => throw new NotSupportedException();
    }

    private sealed class StubChargeService : IChargeService
    {
        public Func<CreatePixChargeRequest, Task<PixCharge>>? OnCreate { get; init; }

        public Task<PixCharge> CreatePixChargeAsync(CreatePixChargeRequest request)
        {
            if (OnCreate is null)
                throw new InvalidOperationException();
            return OnCreate(request);
        }
    }

    private sealed class StubChargeServiceFactory : IFrendzChargeServiceFactory
    {
        private readonly IChargeService _service;

        public StubChargeServiceFactory(IChargeService service) => _service = service;

        public IChargeService Create(FrendzApiCredentials credentials) => _service;
    }
}

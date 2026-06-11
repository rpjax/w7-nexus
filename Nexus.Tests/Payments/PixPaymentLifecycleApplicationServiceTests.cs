using System.Linq.Expressions;
using Nexus.Payments.Application.Contracts;
using Nexus.Operations.Application.Contracts;
using Aidan.Core.Linq;
using Aidan.Mongo.Linq;
using Nexus.Accounts.Aggregates;
using Nexus.Operations.Aggregates;
using Nexus.Operations.Application;
using Nexus.Payments.Application.Models;
using Xunit;
using Nexus.Payments.Aggregates;
using Nexus.Payments.Application;
using Nexus.Accounts.Application.Services.Contracts;
using Nexus.Payments.Errors;

namespace Nexus.Tests.Payments;

public sealed class PixPaymentLifecycleApplicationServiceTests
{
    private sealed class InMemoryPixPaymentRepository : IPaymentRepository
    {
        private readonly List<Payment> _store = new();

        public IAsyncQueryable<Payment> AsQueryable()
            => new MongoAsyncQueryable<Payment>(_store.AsQueryable());

        public Task CreateAsync(Payment entity)
        {
            _store.Add(entity);
            return Task.CompletedTask;
        }

        public Task CreateAsync(IEnumerable<Payment> entities)
        {
            _store.AddRange(entities);
            return Task.CompletedTask;
        }

        public Task DeleteAsync(Payment entity)
        {
            _store.RemoveAll(p => p.Id == entity.Id);
            return Task.CompletedTask;
        }

        public Task<long> DeleteAsync(Expression<Func<Payment, bool>> predicate)
        {
            var compiled = predicate.Compile();
            var removed = _store.RemoveAll(p => compiled(p));
            return Task.FromResult((long)removed);
        }

        public Task UpdateAsync(Payment entity)
        {
            var index = _store.FindIndex(p => p.Id == entity.Id);
            if (index >= 0)
                _store[index] = entity;
            return Task.CompletedTask;
        }

        public Task<long> UpdateAsync(Expression expression) => Task.FromResult(0L);
    }

    private sealed class InMemoryOperationRepository : IOperationRepository
    {
        private readonly List<Operation> _store = new();

        public IAsyncQueryable<Operation> AsQueryable()
            => new MongoAsyncQueryable<Operation>(_store.AsQueryable());

        public Task CreateAsync(Operation entity)
        {
            _store.Add(entity);
            return Task.CompletedTask;
        }

        public Task CreateAsync(IEnumerable<Operation> entities)
        {
            _store.AddRange(entities);
            return Task.CompletedTask;
        }

        public Task DeleteAsync(Operation entity)
        {
            _store.RemoveAll(o => o.Id == entity.Id);
            return Task.CompletedTask;
        }

        public Task<long> DeleteAsync(Expression<Func<Operation, bool>> predicate)
        {
            var compiled = predicate.Compile();
            var removed = _store.RemoveAll(o => compiled(o));
            return Task.FromResult((long)removed);
        }

        public Task UpdateAsync(Operation entity)
        {
            var index = _store.FindIndex(o => o.Id == entity.Id);
            if (index >= 0)
                _store[index] = entity;
            return Task.CompletedTask;
        }

        public Task<long> UpdateAsync(Expression expression) => Task.FromResult(0L);
    }

    private sealed class InMemoryAccountRepository : IAccountRepository
    {
        private readonly List<Account> _store = new();

        public IAsyncQueryable<Account> AsQueryable()
            => new MongoAsyncQueryable<Account>(_store.AsQueryable());

        public Task CreateAsync(Account entity)
        {
            _store.Add(entity);
            return Task.CompletedTask;
        }

        public Task CreateAsync(IEnumerable<Account> entities)
        {
            _store.AddRange(entities);
            return Task.CompletedTask;
        }

        public Task DeleteAsync(Account entity)
        {
            _store.RemoveAll(a => a.Id == entity.Id);
            return Task.CompletedTask;
        }

        public Task<long> DeleteAsync(Expression<Func<Account, bool>> predicate)
        {
            var compiled = predicate.Compile();
            var removed = _store.RemoveAll(a => compiled(a));
            return Task.FromResult((long)removed);
        }

        public Task UpdateAsync(Account entity)
        {
            var index = _store.FindIndex(a => a.Id == entity.Id);
            if (index >= 0)
                _store[index] = entity;
            return Task.CompletedTask;
        }

        public Task<long> UpdateAsync(Expression expression) => Task.FromResult(0L);
    }

    [Fact]
    public async Task ApplicationService_CreatePayRefundAndKill_CoversCompleteLifecycle()
    {
        var payments = new InMemoryPixPaymentRepository();
        var operations = new InMemoryOperationRepository();
        var accounts = new InMemoryAccountRepository();

        var operation = new Operation("operation-1", "Main operation", "pix flow", Array.Empty<string>(), DateTime.UtcNow, DateTime.UtcNow);
        await operations.CreateAsync(operation);

        var operatorAccount = new Account("operator-1", "operator", "hash", Array.Empty<string>(), Array.Empty<string>(), DateTime.UtcNow, DateTime.UtcNow);
        var strawAccount = new Account("straw-1", "straw", "hash", Array.Empty<string>(), Array.Empty<string>(), DateTime.UtcNow, DateTime.UtcNow);
        await accounts.CreateAsync(new[] { operatorAccount, strawAccount });

        var sut = new PaymentService(accounts, payments, operations);

        var created = await sut.CreatePaymentAsync(new CreatePaymentRequest
        {
            OperationId = operation.Id,
            OperatorAccountId = operatorAccount.Id,
            StrawManAccountId = strawAccount.Id,
            Gateway = PaymentGateway.Frendz,
            Amount = 150m,
            GatewayPaymentId = "gw-777"
        });

        Assert.True(created.IsSuccess);
        var paymentId = created.Value!.Id;
        Assert.Equal(PaymentStatus.Pending, created.Value.Status);

        var paidResult = await sut.PayAsync(paymentId);
        Assert.True(paidResult.IsSuccess);
        Assert.Equal(PaymentStatus.Paid, payments.AsQueryable().First(p => p.Id == paymentId).Status);

        var refundResult = await sut.RefundAsync(paymentId);
        Assert.True(refundResult.IsSuccess);
        Assert.Equal(PaymentStatus.Refunded, payments.AsQueryable().First(p => p.Id == paymentId).Status);

        var killAfterRefund = await sut.KillAsync(paymentId, "manual-close");
        Assert.True(killAfterRefund.IsSuccess);
        Assert.Equal(PaymentStatus.Dead, payments.AsQueryable().First(p => p.Id == paymentId).Status);
    }

    [Fact]
    public async Task ApplicationService_KillFromPending_SucceedsAndBlocksFurtherPayment()
    {
        var payments = new InMemoryPixPaymentRepository();
        var operations = new InMemoryOperationRepository();
        var accounts = new InMemoryAccountRepository();

        var operation = new Operation("operation-2", "Second operation", "pix flow", Array.Empty<string>(), DateTime.UtcNow, DateTime.UtcNow);
        await operations.CreateAsync(operation);

        var operatorAccount = new Account("operator-2", "operator2", "hash", Array.Empty<string>(), Array.Empty<string>(), DateTime.UtcNow, DateTime.UtcNow);
        await accounts.CreateAsync(operatorAccount);

        var sut = new PaymentService(accounts, payments, operations);

        var created = await sut.CreatePaymentAsync(new CreatePaymentRequest
        {
            OperationId = operation.Id,
            OperatorAccountId = operatorAccount.Id,
            Gateway = PaymentGateway.FusionPay,
            Amount = 40m,
            GatewayPaymentId = "gw-778"
        });

        Assert.True(created.IsSuccess);
        var paymentId = created.Value!.Id;

        var killResult = await sut.KillAsync(paymentId, "expired");
        Assert.True(killResult.IsSuccess);

        var payAfterKill = await sut.PayAsync(paymentId);
        Assert.True(payAfterKill.IsFailure);
        Assert.Contains(payAfterKill.Errors, e => e.Code == PixPaymentErrorCodes.InvalidTransition);
    }
}

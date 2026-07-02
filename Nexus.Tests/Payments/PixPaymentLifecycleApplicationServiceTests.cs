using System.Linq.Expressions;
using Nexus.Accounts.Aggregates;
using Nexus.Authorization;
using Nexus.Payments.Application.Contracts;
using Nexus.Operations.Application.Contracts;
using Aidan.Core.Linq;
using Aidan.Core.Patterns;
using Aidan.Mongo.Linq;
using Nexus.Operations.Aggregates;
using Nexus.Operations.Application.Services;
using Nexus.Gateways.Application.Models;
using Nexus.Payments.Application.Models;
using Xunit;
using Nexus.Payments.Aggregates;
using Nexus.Charges.Application.Services;
using Nexus.Payments.Application.Services;
using Nexus.Accounts.Application.Contracts;
using Nexus.Payments.Errors;

namespace Nexus.Tests.Payments;

public sealed class PixPaymentLifecycleApplicationServiceTests
{
    private sealed class InMemoryPixPaymentRepository : IPaymentRepository
    {
        private readonly List<Payment> _store = new();

        public IAsyncQueryable<Payment> AsQueryable()
            => new MongoAsyncQueryable<Payment>(_store.AsQueryable());

        public Task<Payment> CreateAsync(Payment entity)
        {
            var persisted = string.IsNullOrWhiteSpace(entity.Id)
                ? PaymentTestFactory.Create(
                    operationId: entity.OperationId,
                    gateway: entity.Gateway,
                    gatewayPaymentId: entity.GatewayTransactionId,
                    amount: entity.Amount,
                    splits: entity.Splits,
                    status: entity.Status,
                    settlementStatus: entity.SettlementStatus,
                    distributionStatus: entity.DistributionStatus,
                    operatorId: entity.OperatorId,
                    strawManId: entity.StrawManId,
                    createdAt: entity.CreatedAt,
                    paidAt: entity.PaidAt,
                    refundedAt: entity.RefundedAt,
                    killedAt: entity.KilledAt,
                    killReason: entity.KillReason,
                    withdrawnAt: entity.WithdrawnAt,
                    distributedAt: entity.DistributedAt)
                : entity;

            _store.Add(persisted);
            return Task.FromResult(persisted);
        }

        async Task IRepository<Payment>.CreateAsync(Payment entity)
        {
            await CreateAsync(entity);
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

        public Task<Operation> CreateAsync(Operation entity)
        {
            _store.Add(entity);
            return Task.FromResult(entity);
        }

        async Task IRepository<Operation>.CreateAsync(Operation entity)
        {
            await CreateAsync(entity);
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

        public Task<Account> CreateAsync(Account entity)
        {
            var persisted = string.IsNullOrWhiteSpace(entity.Id)
                ? new Account(
                    Guid.NewGuid().ToString("N"),
                    entity.Username,
                    entity.PasswordHash,
                    entity.Roles,
                    entity.Permissions,
                    entity.CreatedAt,
                    entity.LastUpdatedAt)
                : entity;

            _store.Add(persisted);
            return Task.FromResult(persisted);
        }

        async Task IRepository<Account>.CreateAsync(Account entity)
        {
            await CreateAsync(entity);
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

    private sealed class InMemoryTeamRepository : ITeamRepository
    {
        private readonly List<Team> _store = new();

        public IAsyncQueryable<Team> AsQueryable()
            => new MongoAsyncQueryable<Team>(_store.AsQueryable());

        public Task<Team> CreateAsync(Team entity)
        {
            _store.Add(entity);
            return Task.FromResult(entity);
        }

        async Task IRepository<Team>.CreateAsync(Team entity)
        {
            await CreateAsync(entity);
        }

        public Task CreateAsync(IEnumerable<Team> entities)
        {
            _store.AddRange(entities);
            return Task.CompletedTask;
        }

        public Task DeleteAsync(Team entity)
        {
            _store.RemoveAll(t => t.Id == entity.Id);
            return Task.CompletedTask;
        }

        public Task<long> DeleteAsync(Expression<Func<Team, bool>> predicate)
        {
            var compiled = predicate.Compile();
            var removed = _store.RemoveAll(t => compiled(t));
            return Task.FromResult((long)removed);
        }

        public Task UpdateAsync(Team entity)
        {
            var index = _store.FindIndex(t => t.Id == entity.Id);
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
        var teams = new InMemoryTeamRepository();

        var operation = new Operation(
            "operation-1",
            "Main operation",
            "pix flow",
            Array.Empty<string>(),
            Array.Empty<string>(),
            GatewaySelectionStrategy.PerStrawman,
            Array.Empty<string>(),
            Array.Empty<string>(),
            DateTime.UtcNow,
            DateTime.UtcNow);
        await operations.CreateAsync(operation);

        var operatorAccount = new Account("operator-1", "operator", "hash", Array.Empty<string>(), Array.Empty<string>(), DateTime.UtcNow, DateTime.UtcNow);
        var strawAccount = new Account("straw-1", "straw", "hash", new[] { Roles.StrawMan }, Array.Empty<string>(), DateTime.UtcNow, DateTime.UtcNow);
        await accounts.CreateAsync(new[] { operatorAccount, strawAccount });

        var team = TeamTestFactory.WithOperatorProfitShare("team-1", operation.Id, operatorAccount.Id, strawAccount.Id, (operatorAccount.Id, 100m));
        await teams.CreateAsync(team);

        var resolver = new ChargeProfitShareResolver(accounts, operations, teams);
        var sut = new PaymentService(accounts, payments, operations);

        var splitsResult = await resolver.ResolveSplitsAsync(operation.Id, operatorAccount.Id, 150m);
        Assert.True(splitsResult.IsSuccess);

        var created = await sut.CreatePaymentAsync(new CreatePaymentRequest
        {
            OperationId = operation.Id,
            OperatorId = operatorAccount.Id,
            StrawManId = strawAccount.Id,
            Gateway = PaymentGateway.Frendz,
            Amount = 150m,
            GatewayPaymentId = "gw-777",
            Splits = splitsResult.Value!,
        });

        Assert.True(created.IsSuccess);
        var paymentId = created.Value!.Id;
        Assert.Equal(PaymentStatus.Pending, created.Value.Status);
        Assert.Single(created.Value.Splits);

        var paidResult = await sut.PayAsync(paymentId);
        Assert.True(paidResult.IsSuccess);
        Assert.Equal(PaymentStatus.Paid, payments.AsQueryable().First(p => p.Id == paymentId).Status);

        var refundResult = await sut.RefundAsync(paymentId);
        Assert.True(refundResult.IsSuccess);
        Assert.Equal(PaymentStatus.Refunded, payments.AsQueryable().First(p => p.Id == paymentId).Status);

        var killAfterRefund = await sut.KillAsync(paymentId, "manual-close");
        Assert.True(killAfterRefund.IsSuccess);
        Assert.Equal(PaymentStatus.Killed, payments.AsQueryable().First(p => p.Id == paymentId).Status);
    }

    [Fact]
    public async Task ApplicationService_PayWithdrawAndRefund_BlocksRefundAfterWithdrawn()
    {
        var payments = new InMemoryPixPaymentRepository();
        var operations = new InMemoryOperationRepository();
        var accounts = new InMemoryAccountRepository();
        var teams = new InMemoryTeamRepository();

        var operation = new Operation(
            "operation-3",
            "Withdraw flow",
            "pix flow",
            Array.Empty<string>(),
            Array.Empty<string>(),
            GatewaySelectionStrategy.PerStrawman,
            Array.Empty<string>(),
            Array.Empty<string>(),
            DateTime.UtcNow,
            DateTime.UtcNow);
        await operations.CreateAsync(operation);

        var operatorAccount = new Account("operator-3", "operator3", "hash", Array.Empty<string>(), Array.Empty<string>(), DateTime.UtcNow, DateTime.UtcNow);
        var strawAccount = new Account("straw-3", "straw3", "hash", new[] { Roles.StrawMan }, Array.Empty<string>(), DateTime.UtcNow, DateTime.UtcNow);
        await accounts.CreateAsync(new[] { operatorAccount, strawAccount });

        var team = TeamTestFactory.WithOperatorProfitShare("team-3", operation.Id, operatorAccount.Id, strawAccount.Id, (operatorAccount.Id, 100m));
        await teams.CreateAsync(team);

        var resolver = new ChargeProfitShareResolver(accounts, operations, teams);
        var sut = new PaymentService(accounts, payments, operations);

        var splitsResult = await resolver.ResolveSplitsAsync(operation.Id, operatorAccount.Id, 80m);
        Assert.True(splitsResult.IsSuccess);

        var created = await sut.CreatePaymentAsync(new CreatePaymentRequest
        {
            OperationId = operation.Id,
            OperatorId = operatorAccount.Id,
            StrawManId = strawAccount.Id,
            Gateway = PaymentGateway.Frendz,
            Amount = 80m,
            GatewayPaymentId = "gw-880",
            Splits = splitsResult.Value!,
        });

        Assert.True(created.IsSuccess);
        var paymentId = created.Value!.Id;

        Assert.True((await sut.PayAsync(paymentId)).IsSuccess);

        var stored = payments.AsQueryable().First(p => p.Id == paymentId);
        Assert.True(stored.MarkAsWithdrawn().IsSuccess);
        await payments.UpdateAsync(stored);
        stored = payments.AsQueryable().First(p => p.Id == paymentId);
        Assert.Equal(PaymentSettlementStatus.Withdrawn, stored.SettlementStatus);
        Assert.NotNull(stored.WithdrawnAt);

        var refundResult = await sut.RefundAsync(paymentId);
        Assert.True(refundResult.IsFailure);
        Assert.Contains(refundResult.Errors, e => e.Code == PixPaymentErrorCodes.InvalidTransition);
    }

    [Fact]
    public async Task ApplicationService_KillFromPending_SucceedsAndBlocksFurtherPayment()
    {
        var payments = new InMemoryPixPaymentRepository();
        var operations = new InMemoryOperationRepository();
        var accounts = new InMemoryAccountRepository();
        var teams = new InMemoryTeamRepository();

        var operation = new Operation(
            "operation-2",
            "Second operation",
            "pix flow",
            Array.Empty<string>(),
            Array.Empty<string>(),
            GatewaySelectionStrategy.PerStrawman,
            Array.Empty<string>(),
            Array.Empty<string>(),
            DateTime.UtcNow,
            DateTime.UtcNow);
        await operations.CreateAsync(operation);

        var adminAccount = new Account("admin-2", "admin", "hash", new[] { Roles.Administrator }, Array.Empty<string>(), DateTime.UtcNow, DateTime.UtcNow);
        var strawAccount = new Account("straw-2", "straw2", "hash", new[] { Roles.StrawMan }, Array.Empty<string>(), DateTime.UtcNow, DateTime.UtcNow);
        await accounts.CreateAsync(new[] { adminAccount, strawAccount });

        var resolver = new ChargeProfitShareResolver(accounts, operations, teams);
        var sut = new PaymentService(accounts, payments, operations);

        var splitsResult = await resolver.ResolveSplitsAsync(operation.Id, null, 40m);
        Assert.True(splitsResult.IsSuccess);

        var created = await sut.CreatePaymentAsync(new CreatePaymentRequest
        {
            OperationId = operation.Id,
            StrawManId = strawAccount.Id,
            Gateway = PaymentGateway.FusionPay,
            Amount = 40m,
            GatewayPaymentId = "gw-778",
            Splits = splitsResult.Value!,
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

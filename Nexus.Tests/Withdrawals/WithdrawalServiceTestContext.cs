using System.Linq.Expressions;
using Aidan.Core.Linq;
using Aidan.Core.Patterns;
using Aidan.Mongo.Linq;
using Nexus.Accounts.Aggregates;
using Nexus.Accounts.Application.Contracts;
using Nexus.Authorization;
using Nexus.Operations.Aggregates;
using Nexus.Operations.Application.Contracts;
using Nexus.Payments.Aggregates;
using Nexus.Payments.Application.Contracts;
using Nexus.Tests.Payments;
using Nexus.Withdrawals.Aggregates;
using Nexus.Withdrawals.Application.Contracts;
using Nexus.Withdrawals.Application.Services;

namespace Nexus.Tests.Withdrawals;

internal static class WithdrawalTestSupport
{
    internal sealed class InMemoryWithdrawalRepository : IWithdrawalRepository
    {
        private readonly List<Withdrawal> _store = new();

        public IAsyncQueryable<Withdrawal> AsQueryable() =>
            new MongoAsyncQueryable<Withdrawal>(_store.AsQueryable());

        public Task<Withdrawal> CreateAsync(Withdrawal entity)
        {
            var persisted = string.IsNullOrWhiteSpace(entity.Id)
                ? new Withdrawal(
                    Guid.NewGuid().ToString("N"),
                    entity.OperationId,
                    entity.Type,
                    entity.StrawManAccountId,
                    entity.BankAccountId,
                    entity.CryptoWalletId,
                    entity.PaymentIds,
                    entity.CostDescription,
                    entity.CostAmount,
                    entity.PixProof,
                    entity.CryptoProof,
                    entity.PaymentsTotalAmount,
                    entity.NetAmount,
                    entity.CreatedAt)
                : entity;
            _store.Add(persisted);
            return Task.FromResult(persisted);
        }

        async Task IRepository<Withdrawal>.CreateAsync(Withdrawal entity) => await CreateAsync(entity);

        public Task CreateAsync(IEnumerable<Withdrawal> entities)
        {
            _store.AddRange(entities);
            return Task.CompletedTask;
        }

        public Task DeleteAsync(Withdrawal entity)
        {
            _store.RemoveAll(w => w.Id == entity.Id);
            return Task.CompletedTask;
        }

        public Task<long> DeleteAsync(Expression<Func<Withdrawal, bool>> predicate) =>
            Task.FromResult((long)_store.RemoveAll(w => predicate.Compile()(w)));

        public Task UpdateAsync(Withdrawal entity) => Task.CompletedTask;

        public Task<long> UpdateAsync(Expression expression) => Task.FromResult(0L);
    }

    internal sealed class InMemoryBankAccountRepository : IBankAccountRepository
    {
        private readonly List<BankAccount> _store = new();

        public IAsyncQueryable<BankAccount> AsQueryable() =>
            new MongoAsyncQueryable<BankAccount>(_store.AsQueryable());

        public Task<BankAccount> CreateAsync(BankAccount entity) => Task.FromResult(entity);

        async Task IRepository<BankAccount>.CreateAsync(BankAccount entity) => await CreateAsync(entity);

        public Task CreateAsync(IEnumerable<BankAccount> entities)
        {
            _store.AddRange(entities);
            return Task.CompletedTask;
        }

        public Task DeleteAsync(BankAccount entity)
        {
            _store.RemoveAll(a => a.Id == entity.Id);
            return Task.CompletedTask;
        }

        public Task<long> DeleteAsync(Expression<Func<BankAccount, bool>> predicate) =>
            Task.FromResult((long)_store.RemoveAll(a => predicate.Compile()(a)));

        public Task UpdateAsync(BankAccount entity) => Task.CompletedTask;

        public Task<long> UpdateAsync(Expression expression) => Task.FromResult(0L);

        public void Seed(BankAccount account) => _store.Add(account);
    }

    internal sealed class InMemoryAccountRepository : IAccountRepository
    {
        private readonly List<Account> _store = new();

        public IAsyncQueryable<Account> AsQueryable() =>
            new MongoAsyncQueryable<Account>(_store.AsQueryable());

        public Task<Account> CreateAsync(Account entity)
        {
            _store.Add(entity);
            return Task.FromResult(entity);
        }

        async Task IRepository<Account>.CreateAsync(Account entity) => await CreateAsync(entity);

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

        public Task<long> DeleteAsync(Expression<Func<Account, bool>> predicate) =>
            Task.FromResult((long)_store.RemoveAll(a => predicate.Compile()(a)));

        public Task UpdateAsync(Account entity) => Task.CompletedTask;

        public Task<long> UpdateAsync(Expression expression) => Task.FromResult(0L);

        public void Seed(Account account) => _store.Add(account);
    }

    internal sealed class InMemoryOperationRepository : IOperationRepository
    {
        private readonly List<Operation> _store = new();

        public IAsyncQueryable<Operation> AsQueryable() =>
            new MongoAsyncQueryable<Operation>(_store.AsQueryable());

        public Task<Operation> CreateAsync(Operation entity)
        {
            _store.Add(entity);
            return Task.FromResult(entity);
        }

        async Task IRepository<Operation>.CreateAsync(Operation entity) => await CreateAsync(entity);

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

        public Task<long> DeleteAsync(Expression<Func<Operation, bool>> predicate) =>
            Task.FromResult((long)_store.RemoveAll(o => predicate.Compile()(o)));

        public Task UpdateAsync(Operation entity) => Task.CompletedTask;

        public Task<long> UpdateAsync(Expression expression) => Task.FromResult(0L);

        public void Seed(Operation operation) => _store.Add(operation);
    }

    internal sealed class InMemoryPaymentRepository : IPaymentRepository
    {
        private readonly List<Payment> _store = new();

        public IAsyncQueryable<Payment> AsQueryable() =>
            new MongoAsyncQueryable<Payment>(_store.AsQueryable());

        public Task<Payment> CreateAsync(Payment entity) => Task.FromResult(entity);

        async Task IRepository<Payment>.CreateAsync(Payment entity) => await CreateAsync(entity);

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

        public Task<long> DeleteAsync(Expression<Func<Payment, bool>> predicate) =>
            Task.FromResult((long)_store.RemoveAll(p => predicate.Compile()(p)));

        public Task UpdateAsync(Payment entity)
        {
            var index = _store.FindIndex(p => p.Id == entity.Id);
            if (index >= 0)
                _store[index] = entity;
            return Task.CompletedTask;
        }

        public Task<long> UpdateAsync(Expression expression) => Task.FromResult(0L);

        public void Seed(Payment payment) => _store.Add(payment);
    }

    internal sealed class StubCryptoWalletRepository : ICryptoWalletRepository
    {
        public IAsyncQueryable<CryptoWallet> AsQueryable() =>
            new MongoAsyncQueryable<CryptoWallet>(Array.Empty<CryptoWallet>().AsQueryable());

        public Task<CryptoWallet> CreateAsync(CryptoWallet entity) => throw new NotImplementedException();

        Task IRepository<CryptoWallet>.CreateAsync(CryptoWallet entity) => CreateAsync(entity);

        public Task CreateAsync(IEnumerable<CryptoWallet> entities) => Task.CompletedTask;

        public Task DeleteAsync(CryptoWallet entity) => Task.CompletedTask;

        public Task<long> DeleteAsync(Expression<Func<CryptoWallet, bool>> predicate) => Task.FromResult(0L);

        public Task UpdateAsync(CryptoWallet entity) => Task.CompletedTask;

        public Task<long> UpdateAsync(Expression expression) => Task.FromResult(0L);
    }
}

internal sealed class WithdrawalServiceTestContext
{
    public const string OperationId = "operation-wd-test";

    public WithdrawalTestSupport.InMemoryAccountRepository Accounts { get; } = new();
    public WithdrawalTestSupport.InMemoryOperationRepository Operations { get; } = new();
    public WithdrawalTestSupport.InMemoryPaymentRepository Payments { get; } = new();
    public WithdrawalTestSupport.InMemoryBankAccountRepository BankAccounts { get; } = new();
    public WithdrawalTestSupport.InMemoryWithdrawalRepository Withdrawals { get; } = new();

    public static WithdrawalServiceTestContext Create() => new();

    public void SeedOperation(IReadOnlyList<string> strawManIds)
    {
        Operations.Seed(new Operation(
            OperationId,
            "Withdraw test op",
            "desc",
            Array.Empty<string>(),
            strawManIds,
            GatewaySelectionStrategy.PerStrawman,
            Array.Empty<string>(),
            Array.Empty<string>(),
            DateTime.UtcNow,
            DateTime.UtcNow));
    }

    public void SeedStrawMan(string strawManId)
    {
        Accounts.Seed(new Account(
            strawManId,
            strawManId,
            "hash",
            new[] { Roles.StrawMan },
            Array.Empty<string>(),
            DateTime.UtcNow,
            DateTime.UtcNow));
    }

    public Payment SeedPaidPayment(
        string operationId = OperationId,
        decimal amount = 100m,
        PaymentSettlementStatus settlementStatus = PaymentSettlementStatus.Unsettled,
        DateTime? withdrawnAt = null)
    {
        var payment = PaymentTestFactory.Create(
            operationId: operationId,
            amount: amount,
            status: PaymentStatus.Paid,
            settlementStatus: settlementStatus,
            paidAt: DateTime.UtcNow,
            withdrawnAt: withdrawnAt);
        Payments.Seed(payment);
        return payment;
    }

    public WithdrawalService CreateService() =>
        new(
            Accounts,
            Operations,
            Payments,
            BankAccounts,
            new WithdrawalTestSupport.StubCryptoWalletRepository(),
            Withdrawals);
}

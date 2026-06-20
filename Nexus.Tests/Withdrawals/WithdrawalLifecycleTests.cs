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
using Nexus.Payments.Errors;
using Nexus.Tests.Payments;
using Nexus.Withdrawals.Aggregates;
using Nexus.Withdrawals.Application.Contracts;
using Nexus.Withdrawals.Application.Services;
using Nexus.Withdrawals.Errors;
using Xunit;

namespace Nexus.Tests.Withdrawals;

public sealed class WithdrawalLifecycleTests
{
    private sealed class InMemoryWithdrawalRepository : IWithdrawalRepository
    {
        private readonly List<Withdrawal> _store = new();

        public IAsyncQueryable<Withdrawal> AsQueryable() =>
            new MongoAsyncQueryable<Withdrawal>(_store.AsQueryable());

        public Task<Withdrawal> CreateAsync(Withdrawal entity)
        {
            var persisted = string.IsNullOrWhiteSpace(entity.Id)
                ? Clone(entity, Guid.NewGuid().ToString("N"))
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

        public Task<long> DeleteAsync(Expression<Func<Withdrawal, bool>> predicate)
        {
            var compiled = predicate.Compile();
            return Task.FromResult((long)_store.RemoveAll(w => compiled(w)));
        }

        public Task UpdateAsync(Withdrawal entity) => Task.CompletedTask;

        public Task<long> UpdateAsync(Expression expression) => Task.FromResult(0L);

        private static Withdrawal Clone(Withdrawal source, string id) =>
            new(
                id,
                source.OperationId,
                source.Type,
                source.StrawManAccountId,
                source.BankAccountId,
                source.CryptoWalletId,
                source.PaymentIds,
                source.CostDescription,
                source.CostAmount,
                source.PixProof,
                source.CryptoProof,
                source.PaymentsTotalAmount,
                source.NetAmount,
                source.CreatedAt);
    }

    private sealed class InMemoryBankAccountRepository : IBankAccountRepository
    {
        private readonly List<BankAccount> _store = new();

        public IAsyncQueryable<BankAccount> AsQueryable() =>
            new MongoAsyncQueryable<BankAccount>(_store.AsQueryable());

        public Task<BankAccount> CreateAsync(BankAccount entity)
        {
            var persisted = string.IsNullOrWhiteSpace(entity.Id)
                ? WithdrawalTestFactory.CreateBankAccount(
                    strawManAccountId: entity.StrawManAccountId,
                    bank: entity.Bank,
                    agency: entity.Agency,
                    accountNumber: entity.AccountNumber,
                    accountDigit: entity.AccountDigit,
                    accountType: entity.AccountType,
                    pixKey: entity.PixKey,
                    label: entity.Label,
                    createdAt: entity.CreatedAt,
                    updatedAt: entity.UpdatedAt)
                : entity;
            _store.Add(persisted);
            return Task.FromResult(persisted);
        }

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

    private sealed class InMemoryAccountRepository : IAccountRepository
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

    private sealed class InMemoryOperationRepository : IOperationRepository
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

    private sealed class InMemoryPaymentRepository : IPaymentRepository
    {
        private readonly List<Payment> _store = new();

        public IAsyncQueryable<Payment> AsQueryable() =>
            new MongoAsyncQueryable<Payment>(_store.AsQueryable());

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
            _store.Add(persisted);
            return Task.FromResult(persisted);
        }

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

    private sealed class StubCryptoWalletRepository : ICryptoWalletRepository
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

    private sealed class StubTeamRepository : ITeamRepository
    {
        public IAsyncQueryable<Team> AsQueryable() =>
            new MongoAsyncQueryable<Team>(Array.Empty<Team>().AsQueryable());

        public Task<Team> CreateAsync(Team entity) => throw new NotImplementedException();

        Task IRepository<Team>.CreateAsync(Team entity) => CreateAsync(entity);

        public Task CreateAsync(IEnumerable<Team> entities) => Task.CompletedTask;

        public Task DeleteAsync(Team entity) => Task.CompletedTask;

        public Task<long> DeleteAsync(Expression<Func<Team, bool>> predicate) => Task.FromResult(0L);

        public Task UpdateAsync(Team entity) => Task.CompletedTask;

        public Task<long> UpdateAsync(Expression expression) => Task.FromResult(0L);
    }

    [Fact]
    public async Task CreateWithdrawal_MarksPaymentsAsWithdrawn()
    {
        var accounts = new InMemoryAccountRepository();
        var operations = new InMemoryOperationRepository();
        var payments = new InMemoryPaymentRepository();
        var bankAccounts = new InMemoryBankAccountRepository();
        var withdrawals = new InMemoryWithdrawalRepository();

        const string operationId = "operation-wd-1";
        const string strawManId = "straw-wd-1";

        operations.Seed(new Operation(
            operationId,
            "Withdraw op",
            "desc",
            Array.Empty<string>(),
            new[] { strawManId },
            GatewaySelectionStrategy.PerStrawman,
            Array.Empty<string>(),
            Array.Empty<string>(),
            DateTime.UtcNow,
            DateTime.UtcNow));

        accounts.Seed(new Account(
            strawManId,
            "laranja1",
            "hash",
            new[] { Roles.StrawMan },
            Array.Empty<string>(),
            DateTime.UtcNow,
            DateTime.UtcNow));

        var bankAccount = WithdrawalTestFactory.CreateBankAccount(strawManAccountId: strawManId);
        bankAccounts.Seed(bankAccount);

        var payment = PaymentTestFactory.Create(
            operationId: operationId,
            amount: 120m,
            status: PaymentStatus.Paid,
            settlementStatus: PaymentSettlementStatus.Unsettled,
            paidAt: DateTime.UtcNow);
        payments.Seed(payment);

        var sut = new WithdrawalService(
            accounts,
            operations,
            payments,
            bankAccounts,
            new StubCryptoWalletRepository(),
            withdrawals);

        var result = await sut.CreateWithdrawalAsync(new CreateWithdrawalRequest
        {
            OperationId = operationId,
            Type = WithdrawalType.Pix,
            StrawManAccountId = strawManId,
            BankAccountId = bankAccount.Id,
            PaymentIds = new[] { payment.Id },
            CostDescription = "Taxa PIX",
            CostAmount = 2m,
            PixTransactionId = "pix-e2e",
            PixAuthenticationCode = "auth-123",
        });

        Assert.True(result.IsSuccess);
        Assert.Equal(118m, result.Value!.NetAmount);
        Assert.Single(withdrawals.AsQueryable().ToList());

        var storedPayment = payments.AsQueryable().First(p => p.Id == payment.Id);
        Assert.Equal(PaymentSettlementStatus.Withdrawn, storedPayment.SettlementStatus);
        Assert.NotNull(storedPayment.WithdrawnAt);
    }

    [Fact]
    public async Task CreateWithdrawal_RejectsAlreadyLinkedPayment()
    {
        var accounts = new InMemoryAccountRepository();
        var operations = new InMemoryOperationRepository();
        var payments = new InMemoryPaymentRepository();
        var bankAccounts = new InMemoryBankAccountRepository();
        var withdrawals = new InMemoryWithdrawalRepository();

        const string operationId = "operation-wd-2";
        const string strawManId = "straw-wd-2";

        operations.Seed(new Operation(
            operationId,
            "Withdraw op 2",
            "desc",
            Array.Empty<string>(),
            new[] { strawManId },
            GatewaySelectionStrategy.PerStrawman,
            Array.Empty<string>(),
            Array.Empty<string>(),
            DateTime.UtcNow,
            DateTime.UtcNow));

        accounts.Seed(new Account(
            strawManId,
            "laranja2",
            "hash",
            new[] { Roles.StrawMan },
            Array.Empty<string>(),
            DateTime.UtcNow,
            DateTime.UtcNow));

        var bankAccount = WithdrawalTestFactory.CreateBankAccount(strawManAccountId: strawManId);
        bankAccounts.Seed(bankAccount);

        var payment = PaymentTestFactory.Create(
            operationId: operationId,
            amount: 50m,
            status: PaymentStatus.Paid,
            settlementStatus: PaymentSettlementStatus.Unsettled,
            paidAt: DateTime.UtcNow);
        payments.Seed(payment);

        await withdrawals.CreateAsync(new Withdrawal(
            Guid.NewGuid().ToString("N"),
            operationId,
            WithdrawalType.Pix,
            strawManId,
            bankAccount.Id,
            null,
            new[] { payment.Id },
            null,
            0m,
            null,
            null,
            50m,
            50m,
            DateTime.UtcNow));

        var sut = new WithdrawalService(
            accounts,
            operations,
            payments,
            bankAccounts,
            new StubCryptoWalletRepository(),
            withdrawals);

        var result = await sut.CreateWithdrawalAsync(new CreateWithdrawalRequest
        {
            OperationId = operationId,
            Type = WithdrawalType.Pix,
            StrawManAccountId = strawManId,
            BankAccountId = bankAccount.Id,
            PaymentIds = new[] { payment.Id },
        });

        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors, e => e.Code == WithdrawalErrorCodes.PaymentAlreadyLinked);
    }

    [Fact]
    public async Task CreateWithdrawal_BlocksRefundAfterWithdrawn()
    {
        var accounts = new InMemoryAccountRepository();
        var operations = new InMemoryOperationRepository();
        var payments = new InMemoryPaymentRepository();
        var bankAccounts = new InMemoryBankAccountRepository();
        var withdrawals = new InMemoryWithdrawalRepository();
        var teams = new StubTeamRepository();

        const string operationId = "operation-wd-3";
        const string strawManId = "straw-wd-3";

        operations.Seed(new Operation(
            operationId,
            "Withdraw op 3",
            "desc",
            Array.Empty<string>(),
            new[] { strawManId },
            GatewaySelectionStrategy.PerStrawman,
            Array.Empty<string>(),
            Array.Empty<string>(),
            DateTime.UtcNow,
            DateTime.UtcNow));

        accounts.Seed(new Account(
            strawManId,
            "laranja3",
            "hash",
            new[] { Roles.StrawMan },
            Array.Empty<string>(),
            DateTime.UtcNow,
            DateTime.UtcNow));

        var bankAccount = WithdrawalTestFactory.CreateBankAccount(strawManAccountId: strawManId);
        bankAccounts.Seed(bankAccount);

        var payment = PaymentTestFactory.Create(
            operationId: operationId,
            amount: 80m,
            status: PaymentStatus.Paid,
            settlementStatus: PaymentSettlementStatus.Unsettled,
            paidAt: DateTime.UtcNow);
        payments.Seed(payment);

        var withdrawalService = new WithdrawalService(
            accounts,
            operations,
            payments,
            bankAccounts,
            new StubCryptoWalletRepository(),
            withdrawals);

        var paymentService = new Nexus.Payments.Application.Services.PaymentService(
            accounts,
            payments,
            operations,
            teams);

        var withdrawalResult = await withdrawalService.CreateWithdrawalAsync(new CreateWithdrawalRequest
        {
            OperationId = operationId,
            Type = WithdrawalType.Pix,
            StrawManAccountId = strawManId,
            BankAccountId = bankAccount.Id,
            PaymentIds = new[] { payment.Id },
        });

        Assert.True(withdrawalResult.IsSuccess);

        var refundResult = await paymentService.RefundAsync(payment.Id);
        Assert.True(refundResult.IsFailure);
        Assert.Contains(refundResult.Errors, e => e.Code == PixPaymentErrorCodes.InvalidTransition);
    }
}

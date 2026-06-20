using System.Linq.Expressions;
using Aidan.Core.Linq;
using Aidan.Mongo.Linq;
using Nexus.Withdrawals.Aggregates;
using Nexus.Withdrawals.Application.Contracts;
using Nexus.Withdrawals.Application.Services;
using Nexus.Withdrawals.Errors;
using Xunit;

namespace Nexus.Tests.Withdrawals;

public sealed class BankAccountServiceTests
{
    private sealed class InMemoryBankAccountRepository : IBankAccountRepository
    {
        private readonly List<BankAccount> _store = new();

        public IAsyncQueryable<BankAccount> AsQueryable() =>
            new MongoAsyncQueryable<BankAccount>(_store.AsQueryable());

        public Task<BankAccount> CreateAsync(BankAccount entity)
        {
            _store.Add(entity);
            return Task.FromResult(entity);
        }

        async Task IRepository<BankAccount>.CreateAsync(BankAccount entity) => await CreateAsync(entity);

        public Task CreateAsync(IEnumerable<BankAccount> entities)
        {
            _store.AddRange(entities);
            return Task.CompletedTask;
        }

        public Task DeleteAsync(BankAccount entity) => Task.CompletedTask;

        public Task<long> DeleteAsync(Expression<Func<BankAccount, bool>> predicate) =>
            Task.FromResult(0L);

        public Task UpdateAsync(BankAccount entity) => Task.CompletedTask;

        public Task<long> UpdateAsync(Expression expression) => Task.FromResult(0L);

        public void Seed(BankAccount account) => _store.Add(account);
    }

    [Fact]
    public async Task UpdateLabelAsync_ExistingAccount_UpdatesLabel()
    {
        var account = WithdrawalTestFactory.CreateBankAccount(id: "bank-1", label: "Antigo");
        var repo = new InMemoryBankAccountRepository();
        repo.Seed(account);
        var service = new BankAccountService(null!, repo);

        var result = await service.UpdateLabelAsync("bank-1", "Conta principal");

        Assert.True(result.IsSuccess);
        Assert.Equal("Conta principal", result.Value!.Label);
    }

    [Fact]
    public async Task UpdateLabelAsync_MissingAccount_ReturnsNotFound()
    {
        var repo = new InMemoryBankAccountRepository();
        var service = new BankAccountService(null!, repo);

        var result = await service.UpdateLabelAsync("missing", "Novo");

        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors, e => e.Code == BankAccountErrorCodes.BankAccountNotFound);
    }
}

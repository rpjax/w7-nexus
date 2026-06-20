using System.Linq.Expressions;
using Aidan.Core.Linq;
using Aidan.Core.Patterns;
using Aidan.Mongo.Linq;
using Nexus.Accounts.Aggregates;
using Nexus.Accounts.Application.Contracts;
using Nexus.Authorization;
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
            var persisted = string.IsNullOrWhiteSpace(entity.Id)
                ? WithdrawalTestFactory.CreateBankAccount(
                    strawManAccountId: entity.StrawManAccountId,
                    bank: entity.Bank,
                    agency: entity.Agency,
                    accountNumber: entity.AccountNumber,
                    accountDigit: entity.AccountDigit,
                    accountType: entity.AccountType,
                    pixKeyType: entity.PixKeyType,
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

        public Task DeleteAsync(BankAccount entity) => Task.CompletedTask;

        public Task<long> DeleteAsync(Expression<Func<BankAccount, bool>> predicate) =>
            Task.FromResult(0L);

        public Task UpdateAsync(BankAccount entity) => Task.CompletedTask;

        public Task<long> UpdateAsync(Expression expression) => Task.FromResult(0L);

        public BankAccount? FindById(string id) => _store.FirstOrDefault(a => a.Id == id);

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

        public Task DeleteAsync(Account entity) => Task.CompletedTask;

        public Task<long> DeleteAsync(Expression<Func<Account, bool>> predicate) =>
            Task.FromResult(0L);

        public Task UpdateAsync(Account entity) => Task.CompletedTask;

        public Task<long> UpdateAsync(Expression expression) => Task.FromResult(0L);

        public void Seed(Account account) => _store.Add(account);
    }

    private static (BankAccountService Service, InMemoryBankAccountRepository BankAccounts) CreateSut(
        string strawManId = "straw-1")
    {
        var accounts = new InMemoryAccountRepository();
        accounts.Seed(new Account(
            strawManId,
            "laranja1",
            "hash",
            new[] { Roles.StrawMan },
            Array.Empty<string>(),
            DateTime.UtcNow,
            DateTime.UtcNow));

        var bankAccounts = new InMemoryBankAccountRepository();
        return (new BankAccountService(accounts, bankAccounts), bankAccounts);
    }

    private static CreateBankAccountRequest ValidRequest(string strawManId = "straw-1") => new()
    {
        StrawManAccountId = strawManId,
        Bank = BrazilianBank.BancodoBrasilSA_001,
        Agency = "1234",
        AccountNumber = "56789",
        AccountDigit = "0",
        AccountType = BankAccountType.Checking,
        PixKeyType = PixKeyType.Email,
        PixKey = "  Conta@Example.COM ",
        Label = "Principal",
    };

    private static CreateBankAccountRequest BuildRequest(
        string strawManId = "straw-1",
        PixKeyType pixKeyType = PixKeyType.Email,
        string pixKey = "conta@example.com") =>
        new()
        {
            StrawManAccountId = strawManId,
            Bank = BrazilianBank.BancodoBrasilSA_001,
            Agency = "1234",
            AccountNumber = "56789",
            AccountDigit = "0",
            AccountType = BankAccountType.Checking,
            PixKeyType = pixKeyType,
            PixKey = pixKey,
        };

    [Fact]
    public async Task CreateAsync_ValidRequest_PersistsNormalizedPixKey()
    {
        var (service, bankAccounts) = CreateSut();

        var result = await service.CreateAsync(ValidRequest());

        Assert.True(result.IsSuccess);
        Assert.Equal(PixKeyType.Email, result.Value!.PixKeyType);
        Assert.Equal("conta@example.com", result.Value.PixKey);
        Assert.NotNull(bankAccounts.FindById(result.Value.Id));
    }

    [Fact]
    public async Task CreateAsync_MissingPixKey_ReturnsPixKeyRequired()
    {
        var (service, _) = CreateSut();
        var request = BuildRequest(pixKey: "  ");

        var result = await service.CreateAsync(request);

        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors, e => e.Code == BankAccountErrorCodes.PixKeyRequired);
    }

    [Fact]
    public async Task CreateAsync_InvalidPixKey_ReturnsPixKeyInvalid()
    {
        var (service, _) = CreateSut();
        var request = BuildRequest(pixKeyType: PixKeyType.Cpf, pixKey: "111.111.111-11");

        var result = await service.CreateAsync(request);

        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors, e => e.Code == BankAccountErrorCodes.PixKeyInvalid);
    }

    [Fact]
    public async Task CreateAsync_UnknownStrawMan_ReturnsNotFound()
    {
        var (service, _) = CreateSut();
        var request = ValidRequest(strawManId: "missing-straw");

        var result = await service.CreateAsync(request);

        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors, e => e.Code == BankAccountErrorCodes.StrawManNotFound);
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

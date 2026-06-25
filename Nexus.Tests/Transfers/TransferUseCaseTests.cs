using System.Linq.Expressions;
using Aidan.Core.Linq;
using Aidan.Core.Patterns;
using Aidan.Mongo.Linq;
using Nexus.BankAccounts.Aggregates;
using Nexus.BankAccounts.Application.Contracts;
using Nexus.BankAccounts.Application.Services;
using Nexus.BankAccounts.Infrastructure.Mapping;
using Nexus.CryptoWallets.Aggregates;
using Nexus.CryptoWallets.Application.Contracts;
using Nexus.CryptoWallets.Application.Services;
using Nexus.CryptoWallets.Infrastructure.Mapping;
using Nexus.Accounts.Aggregates;
using Nexus.Accounts.Application.Contracts;
using Nexus.Authorization;
using Nexus.Payments.Aggregates;
using Nexus.Payments.Application.Contracts;
using Nexus.StrawMen.Application.Contracts;
using Nexus.Tests.Payments;
using Nexus.Transfers.Aggregates;
using Nexus.Transfers.Application.Contracts;
using Nexus.Transfers.Application.Models;
using Nexus.Transfers.Application.Requests;
using Nexus.Transfers.Application.Services;
using Nexus.Transfers.Errors;
using Nexus.Transfers.Infrastructure.Mapping;
using Xunit;

namespace Nexus.Tests.Transfers;

public sealed class TransferUseCaseTests
{
    private static TransferService CreateSut(TransferTestContext ctx) =>
        new(
            ctx.Accounts,
            ctx.Payments,
            ctx.BankAccounts,
            ctx.CryptoWallets,
            ctx.BankBalanceService,
            ctx.CryptoBalanceService,
            ctx.Transfers,
            ctx.SplitCalculation,
            new StubTransferTimelineQueryService());

    [Fact]
    public async Task PayoutTransfer_WithoutProof_Fails()
    {
        var ctx = await TransferTestContext.CreateAsync();
        var bank = await ctx.SeedBankAccountAsync("straw-1");
        var destination = await ctx.SeedBankAccountAsync("straw-1", "dest");
        var balance = await ctx.SeedBankBalanceAsync(bank, 500m);

        var sut = CreateSut(ctx);

        var result = await sut.ExecutePayoutAsync(new PayoutTransferRequest
        {
            SourceBalanceId = balance.Id,
            Amount = 100m,
            DestinationBankAccountId = destination.Id,
        });

        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors, e => e.Code == TransferErrorCodes.ProofRequired);
    }

    [Fact]
    public async Task WithdrawalTransfer_IdenticalSplits_CreatesSingleBalance()
    {
        var ctx = await TransferTestContext.CreateAsync();
        var bank = await ctx.SeedBankAccountAsync("straw-1");
        var splits = PaymentSplit.AllocateFromCuts(100m, new[] { ("op-1", 100m) });

        await ctx.SeedPaymentAsync("pay-1", 60m, splits);
        await ctx.SeedPaymentAsync("pay-2", 40m, splits);

        var sut = CreateSut(ctx);

        var result = await sut.ExecuteWithdrawalAsync(new WithdrawalTransferRequest
        {
            DestinationBankAccountId = bank.Id,
            PaymentIds = new[] { "pay-1", "pay-2" },
        });

        Assert.True(result.IsSuccess, result.IsFailure ? string.Join("; ", result.Errors.Select(e => e.Code)) : null);
        var balances = ctx.BankBalances.AsQueryable().Where(b => b.BankAccountId == bank.Id).ToList();
        Assert.Single(balances);
        Assert.Equal(100m, balances[0].AmountBrl);
    }

    [Fact]
    public async Task WithdrawalTransfer_DifferentSplits_CreatesSeparateBalances()
    {
        var ctx = await TransferTestContext.CreateAsync();
        var bank = await ctx.SeedBankAccountAsync("straw-1");

        await ctx.SeedPaymentAsync(
            "pay-1",
            60m,
            PaymentSplit.AllocateFromCuts(60m, new[] { ("op-1", 100m) }));
        await ctx.SeedPaymentAsync(
            "pay-2",
            40m,
            PaymentSplit.AllocateFromCuts(40m, new[] { ("op-2", 100m) }));

        var sut = CreateSut(ctx);

        var result = await sut.ExecuteWithdrawalAsync(new WithdrawalTransferRequest
        {
            DestinationBankAccountId = bank.Id,
            PaymentIds = new[] { "pay-1", "pay-2" },
        });

        Assert.True(result.IsSuccess, result.IsFailure ? string.Join("; ", result.Errors.Select(e => e.Code)) : null);
        var balances = ctx.BankBalances.AsQueryable().Where(b => b.BankAccountId == bank.Id).ToList();
        Assert.Equal(2, balances.Count);
        Assert.Equal(100m, balances.Sum(l => l.AmountBrl));
    }

    [Fact]
    public async Task BankAccountMovement_PartialDebit_LeavesRemainderOnSource()
    {
        var ctx = await TransferTestContext.CreateAsync();
        var source = await ctx.SeedBankAccountAsync("straw-1", "bank-source");
        var dest = await ctx.SeedBankAccountAsync("straw-1", "bank-dest");
        var balance = await ctx.SeedBankBalanceAsync(source, 1000m);

        var sut = CreateSut(ctx);

        var result = await sut.ExecuteBankAccountMovementAsync(new BankAccountMovementRequest
        {
            SourceBalanceId = balance.Id,
            Amount = 400m,
            DestinationBankAccountId = dest.Id,
        });

        Assert.True(result.IsSuccess, result.IsFailure ? string.Join("; ", result.Errors.Select(e => e.Code)) : null);

        var sourceBalances = ctx.BankBalances.AsQueryable().Where(b => b.BankAccountId == source.Id).ToList();
        var destBalances = ctx.BankBalances.AsQueryable().Where(b => b.BankAccountId == dest.Id).ToList();

        Assert.Single(sourceBalances);
        Assert.Equal(600m, sourceBalances[0].AmountBrl);
        Assert.Single(destBalances);
        Assert.Equal(400m, destBalances[0].AmountBrl);
        Assert.Equal(balance.Id, result.Value!.SourceBalanceId);
    }

    [Fact]
    public async Task PayoutTransfer_PersistsSourceBalanceId()
    {
        var ctx = await TransferTestContext.CreateAsync();
        var bank = await ctx.SeedBankAccountAsync("straw-1");
        var destination = await ctx.SeedBankAccountAsync("straw-1", "dest");
        var balance = await ctx.SeedBankBalanceAsync(bank, 500m);

        var sut = CreateSut(ctx);

        var result = await sut.ExecutePayoutAsync(new PayoutTransferRequest
        {
            SourceBalanceId = balance.Id,
            Amount = 100m,
            DestinationBankAccountId = destination.Id,
            Proof = new TransferProofRequest { PixTransactionId = "pix-123" },
        });

        Assert.True(result.IsSuccess, result.IsFailure ? string.Join("; ", result.Errors.Select(e => e.Code)) : null);
        Assert.Equal(balance.Id, result.Value!.SourceBalanceId);
    }

    [Fact]
    public async Task WithdrawalTransfer_CryptoWithBtcOnPolygon_FailsAssetChainMismatch()
    {
        var ctx = await TransferTestContext.CreateAsync();
        var wallet = await ctx.SeedCryptoWalletAsync("straw-1", AddressNamespace.Evm, "0xabc");
        await ctx.SeedPaymentAsync("pay-1", 100m, PaymentSplit.AllocateFromCuts(100m, new[] { ("op-1", 100m) }));

        var sut = CreateSut(ctx);

        var result = await sut.ExecuteWithdrawalAsync(new WithdrawalTransferRequest
        {
            DestinationCryptoWalletId = wallet.Id,
            PaymentIds = new[] { "pay-1" },
            OnrampingMethod = OnrampingMethod.Pix,
            ProducedAmount = 0.01m,
            ProducedAsset = CryptoAsset.Btc,
            ProducedChain = Chain.Polygon,
        });

        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors, e => e.Code == TransferErrorCodes.AssetChainMismatch);
    }

    [Fact]
    public async Task WithdrawalTransfer_CryptoWithoutNamespaceAddress_Fails()
    {
        var ctx = await TransferTestContext.CreateAsync();
        var wallet = await ctx.SeedCryptoWalletAsync("straw-1", AddressNamespace.Tron, "TXyz");
        await ctx.SeedPaymentAsync("pay-1", 100m, PaymentSplit.AllocateFromCuts(100m, new[] { ("op-1", 100m) }));

        var sut = CreateSut(ctx);

        var result = await sut.ExecuteWithdrawalAsync(new WithdrawalTransferRequest
        {
            DestinationCryptoWalletId = wallet.Id,
            PaymentIds = new[] { "pay-1" },
            OnrampingMethod = OnrampingMethod.Pix,
            ProducedAmount = 100m,
            ProducedAsset = CryptoAsset.Usdt,
            ProducedChain = Chain.Polygon,
        });

        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors, e => e.Code == TransferErrorCodes.ProducedChainNamespaceMismatch);
    }

    private sealed class TransferTestContext
    {
        public InMemoryAccountRepository Accounts { get; } = new();
        public InMemoryPaymentRepository Payments { get; } = new();
        public InMemoryBankAccountRepository BankAccounts { get; } = new();
        public InMemoryCryptoWalletRepository CryptoWallets { get; } = new();
        public InMemoryBankBalanceRepository BankBalances { get; } = new();
        public InMemoryCryptoBalanceRepository CryptoBalances { get; } = new();
        public InMemoryTransferRepository Transfers { get; } = new();
        public BankBalanceService BankBalanceService { get; private set; } = null!;
        public CryptoBalanceService CryptoBalanceService { get; private set; } = null!;
        public BalanceSplitCalculationService SplitCalculation { get; private set; } = null!;

        public static async Task<TransferTestContext> CreateAsync()
        {
            var ctx = new TransferTestContext();
            await ctx.Accounts.CreateAsync(new Account(
                "straw-1",
                "straw",
                "hash",
                new[] { Roles.StrawMan },
                Array.Empty<string>(),
                DateTime.UtcNow,
                DateTime.UtcNow));
            ctx.BankBalanceService = new BankBalanceService(ctx.BankBalances);
            ctx.CryptoBalanceService = new CryptoBalanceService(ctx.CryptoBalances);
            ctx.SplitCalculation = new BalanceSplitCalculationService(new StubStrawManSettingsQueryService());
            return ctx;
        }

        public async Task<BankAccount> SeedBankAccountAsync(string strawManId, string suffix = "main")
        {
            var created = BankAccount.Create(
                strawManId,
                BrazilianBank.BancodoBrasilSA_001,
                "1234",
                suffix,
                null,
                BankAccountType.Checking,
                null).Value!;
            return await BankAccounts.CreateAsync(created);
        }

        public async Task<CryptoWallet> SeedCryptoWalletAsync(
            string strawManId,
            AddressNamespace addressNamespace,
            string address)
        {
            var walletAddress = CryptoWalletAddress.Create(addressNamespace, address, null).Value!;
            var created = CryptoWallet.Create(strawManId, new[] { walletAddress }, null).Value!;
            return await CryptoWallets.CreateAsync(created);
        }

        public async Task<BankBalance> SeedBankBalanceAsync(BankAccount account, decimal amount)
        {
            var origin = BankBalanceOrigin.Create("op-1", "operator-1").Value!;
            var split = BankBalanceSplit.Create("operator-1", 100m, amount, BankSplitKind.ProfitShare).Value!;
            var balance = BankBalance.Create(
                account.Id,
                amount,
                "seed-transfer",
                new[] { split },
                origin).Value!;
            return await BankBalances.CreateAsync(balance);
        }

        public async Task SeedPaymentAsync(string id, decimal amount, IReadOnlyList<PaymentSplit> splits)
        {
            var payment = PaymentTestFactory.Create(
                id: id,
                operationId: "op-1",
                amount: amount,
                splits: splits,
                status: PaymentStatus.Paid,
                operatorId: "operator-1",
                strawManId: "straw-1",
                paidAt: DateTime.UtcNow);
            await Payments.CreateAsync(payment);
        }
    }

    private sealed class StubTransferTimelineQueryService : ITransferTimelineQueryService
    {
        public Task<IResult<TransferTimelineDetails>> GetTimelineAsync(string transferId) =>
            throw new NotSupportedException();
    }

    private sealed class StubStrawManSettingsQueryService : IStrawManSettingsQueryService
    {
        public Task<decimal> GetMovementFeePercentageAsync(string strawManId, CancellationToken cancellationToken = default) =>
            Task.FromResult(0m);

        public Task<IResult<StrawManSettingsDetails>> GetSettingsAsync(
            string strawManId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IResult<StrawManSettingsDetails>>(Result<StrawManSettingsDetails>.Success(new StrawManSettingsDetails
            {
                StrawManId = strawManId,
                MovementFeePercentage = 0m,
            }));
    }

    private sealed class InMemoryAccountRepository : IAccountRepository
    {
        private readonly List<Account> _store = new();

        public IAsyncQueryable<Account> AsQueryable() =>
            new QueryableToAsyncQueryableAdapter<Account>(_store.AsQueryable());

        public Task<Account> CreateAsync(Account entity)
        {
            _store.Add(entity);
            return Task.FromResult(entity);
        }

        async Task IRepository<Account>.CreateAsync(Account entity) => await CreateAsync(entity);
        public Task CreateAsync(IEnumerable<Account> entities) { _store.AddRange(entities); return Task.CompletedTask; }
        public Task DeleteAsync(Account entity) { _store.Remove(entity); return Task.CompletedTask; }
        public Task<long> DeleteAsync(Expression<Func<Account, bool>> predicate) => Task.FromResult(0L);
        public Task UpdateAsync(Account entity) => Task.CompletedTask;
        public Task<long> UpdateAsync(Expression expression) => Task.FromResult(0L);
    }

    private sealed class InMemoryPaymentRepository : IPaymentRepository
    {
        private readonly List<Payment> _store = new();

        public IAsyncQueryable<Payment> AsQueryable() =>
            new QueryableToAsyncQueryableAdapter<Payment>(_store.AsQueryable());

        public Task<Payment> CreateAsync(Payment entity)
        {
            _store.Add(entity);
            return Task.FromResult(entity);
        }

        async Task IRepository<Payment>.CreateAsync(Payment entity) => await CreateAsync(entity);
        public Task CreateAsync(IEnumerable<Payment> entities) { _store.AddRange(entities); return Task.CompletedTask; }
        public Task DeleteAsync(Payment entity) { _store.Remove(entity); return Task.CompletedTask; }
        public Task<long> DeleteAsync(Expression<Func<Payment, bool>> predicate) => Task.FromResult(0L);
        public Task UpdateAsync(Payment entity)
        {
            var index = _store.FindIndex(p => p.Id == entity.Id);
            if (index >= 0) _store[index] = entity;
            return Task.CompletedTask;
        }

        public Task<long> UpdateAsync(Expression expression) => Task.FromResult(0L);
    }

    private sealed class InMemoryBankAccountRepository : IBankAccountRepository
    {
        private readonly List<BankAccount> _store = new();

        public IAsyncQueryable<BankAccount> AsQueryable() =>
            new QueryableToAsyncQueryableAdapter<BankAccount>(_store.AsQueryable());

        public Task<BankAccount> CreateAsync(BankAccount entity)
        {
            var record = BankAccountRecordMapping.ToRecord(entity);
            var persisted = BankAccountRecordMapping.ToBankAccount(record);
            _store.Add(persisted);
            return Task.FromResult(persisted);
        }

        async Task IRepository<BankAccount>.CreateAsync(BankAccount entity) => await CreateAsync(entity);
        public Task CreateAsync(IEnumerable<BankAccount> entities) => Task.CompletedTask;
        public Task DeleteAsync(BankAccount entity) { _store.RemoveAll(b => b.Id == entity.Id); return Task.CompletedTask; }
        public Task<long> DeleteAsync(Expression<Func<BankAccount, bool>> predicate) => Task.FromResult(0L);
        public Task UpdateAsync(BankAccount entity)
        {
            var index = _store.FindIndex(b => b.Id == entity.Id);
            if (index >= 0) _store[index] = entity;
            return Task.CompletedTask;
        }

        public Task<long> UpdateAsync(Expression expression) => Task.FromResult(0L);
    }

    private sealed class InMemoryCryptoWalletRepository : ICryptoWalletRepository
    {
        private readonly List<CryptoWallet> _store = new();

        public IAsyncQueryable<CryptoWallet> AsQueryable() =>
            new QueryableToAsyncQueryableAdapter<CryptoWallet>(_store.AsQueryable());

        public Task<CryptoWallet> CreateAsync(CryptoWallet entity)
        {
            var record = CryptoWalletRecordMapping.ToRecord(entity);
            var persisted = CryptoWalletRecordMapping.ToCryptoWallet(record);
            _store.Add(persisted);
            return Task.FromResult(persisted);
        }

        async Task IRepository<CryptoWallet>.CreateAsync(CryptoWallet entity) => await CreateAsync(entity);
        public Task CreateAsync(IEnumerable<CryptoWallet> entities) => Task.CompletedTask;
        public Task DeleteAsync(CryptoWallet entity) { _store.RemoveAll(w => w.Id == entity.Id); return Task.CompletedTask; }
        public Task<long> DeleteAsync(Expression<Func<CryptoWallet, bool>> predicate) => Task.FromResult(0L);
        public Task UpdateAsync(CryptoWallet entity)
        {
            var index = _store.FindIndex(w => w.Id == entity.Id);
            if (index >= 0) _store[index] = entity;
            return Task.CompletedTask;
        }

        public Task<long> UpdateAsync(Expression expression) => Task.FromResult(0L);
    }

    private sealed class InMemoryBankBalanceRepository : IBankBalanceRepository
    {
        private readonly List<BankBalance> _store = new();

        public IAsyncQueryable<BankBalance> AsQueryable() =>
            new QueryableToAsyncQueryableAdapter<BankBalance>(_store.AsQueryable());

        public Task<BankBalance> CreateAsync(BankBalance entity)
        {
            _store.Add(entity);
            return Task.FromResult(entity);
        }

        async Task IRepository<BankBalance>.CreateAsync(BankBalance entity) => await CreateAsync(entity);
        public Task CreateAsync(IEnumerable<BankBalance> entities) { _store.AddRange(entities); return Task.CompletedTask; }
        public Task DeleteAsync(BankBalance entity) { _store.RemoveAll(b => b.Id == entity.Id); return Task.CompletedTask; }
        public Task<long> DeleteAsync(Expression<Func<BankBalance, bool>> predicate) => Task.FromResult(0L);
        public Task UpdateAsync(BankBalance entity)
        {
            var index = _store.FindIndex(b => b.Id == entity.Id);
            if (index >= 0) _store[index] = entity;
            return Task.CompletedTask;
        }

        public Task<long> UpdateAsync(Expression expression) => Task.FromResult(0L);
    }

    private sealed class InMemoryCryptoBalanceRepository : ICryptoBalanceRepository
    {
        private readonly List<CryptoBalance> _store = new();

        public IAsyncQueryable<CryptoBalance> AsQueryable() =>
            new QueryableToAsyncQueryableAdapter<CryptoBalance>(_store.AsQueryable());

        public Task<CryptoBalance> CreateAsync(CryptoBalance entity)
        {
            _store.Add(entity);
            return Task.FromResult(entity);
        }

        async Task IRepository<CryptoBalance>.CreateAsync(CryptoBalance entity) => await CreateAsync(entity);
        public Task CreateAsync(IEnumerable<CryptoBalance> entities) { _store.AddRange(entities); return Task.CompletedTask; }
        public Task DeleteAsync(CryptoBalance entity) { _store.RemoveAll(b => b.Id == entity.Id); return Task.CompletedTask; }
        public Task<long> DeleteAsync(Expression<Func<CryptoBalance, bool>> predicate) => Task.FromResult(0L);
        public Task UpdateAsync(CryptoBalance entity)
        {
            var index = _store.FindIndex(b => b.Id == entity.Id);
            if (index >= 0) _store[index] = entity;
            return Task.CompletedTask;
        }

        public Task<long> UpdateAsync(Expression expression) => Task.FromResult(0L);
    }

    private sealed class InMemoryTransferRepository : ITransferRepository
    {
        private readonly List<Transfer> _store = new();

        public IAsyncQueryable<Transfer> AsQueryable() =>
            new QueryableToAsyncQueryableAdapter<Transfer>(_store.AsQueryable());

        public Task<Transfer> CreateAsync(Transfer entity)
        {
            var record = TransferRecordMapping.ToRecord(entity);
            var persisted = TransferRecordMapping.ToTransfer(record);
            _store.Add(persisted);
            return Task.FromResult(persisted);
        }

        async Task IRepository<Transfer>.CreateAsync(Transfer entity) => await CreateAsync(entity);
        public Task CreateAsync(IEnumerable<Transfer> entities) { _store.AddRange(entities); return Task.CompletedTask; }
        public Task DeleteAsync(Transfer entity) { _store.Remove(entity); return Task.CompletedTask; }
        public Task<long> DeleteAsync(Expression<Func<Transfer, bool>> predicate) => Task.FromResult(0L);
        public Task UpdateAsync(Transfer entity) => Task.CompletedTask;
        public Task<long> UpdateAsync(Expression expression) => Task.FromResult(0L);
    }
}

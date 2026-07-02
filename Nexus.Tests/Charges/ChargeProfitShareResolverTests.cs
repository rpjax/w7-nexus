using System.Linq.Expressions;
using Aidan.Core.Linq;
using Aidan.Core.Patterns;
using Aidan.Mongo.Linq;
using Xunit;
using Nexus.Accounts.Aggregates;
using Nexus.Authorization;
using Nexus.Charges.Application.Services;
using Nexus.Operations.Aggregates;
using Nexus.Operations.Application.Contracts;
using Nexus.Payments.Aggregates;
using Nexus.Payments.Errors;
using Nexus.Accounts.Application.Contracts;
using Nexus.Tests.Payments;

namespace Nexus.Tests.Charges;

public sealed class ChargeProfitShareResolverTests
{
    private sealed class StubOperationRepository : IOperationRepository
    {
        private readonly Operation[] _operations;

        public StubOperationRepository(params Operation[] operations) => _operations = operations;

        public StubOperationRepository(params string[] operationIds)
        {
            _operations = operationIds
                .Select(id => new Operation(
                    id,
                    $"Operation {id}",
                    "desc",
                    Array.Empty<string>(),
                    Array.Empty<string>(),
                    GatewaySelectionStrategy.PerStrawman,
                    Array.Empty<string>(),
                    Array.Empty<string>(),
                    DateTime.UtcNow,
                    DateTime.UtcNow))
                .ToArray();
        }

        public IAsyncQueryable<Operation> AsQueryable()
            => new MongoAsyncQueryable<Operation>(_operations.AsQueryable());

        public Task<Operation> CreateAsync(Operation entity) => Task.FromResult(entity);

        async Task IRepository<Operation>.CreateAsync(Operation entity) => await CreateAsync(entity);

        public Task CreateAsync(IEnumerable<Operation> entities) => Task.CompletedTask;
        public Task DeleteAsync(Operation entity) => Task.CompletedTask;
        public Task<long> DeleteAsync(Expression<Func<Operation, bool>> predicate) => Task.FromResult(0L);
        public Task UpdateAsync(Operation entity) => Task.CompletedTask;
        public Task<long> UpdateAsync(Expression expression) => Task.FromResult(0L);
    }

    private sealed class StubTeamRepository : ITeamRepository
    {
        private readonly Team[] _teams;

        public StubTeamRepository(params Team[] teams) => _teams = teams;

        public IAsyncQueryable<Team> AsQueryable()
            => new MongoAsyncQueryable<Team>(_teams.AsQueryable());

        public Task<Team> CreateAsync(Team entity) => Task.FromResult(entity);

        async Task IRepository<Team>.CreateAsync(Team entity) => await CreateAsync(entity);

        public Task CreateAsync(IEnumerable<Team> entities) => Task.CompletedTask;
        public Task DeleteAsync(Team entity) => Task.CompletedTask;
        public Task<long> DeleteAsync(Expression<Func<Team, bool>> predicate) => Task.FromResult(0L);
        public Task UpdateAsync(Team entity) => Task.CompletedTask;
        public Task<long> UpdateAsync(Expression expression) => Task.FromResult(0L);
    }

    private sealed class StubAccountRepository : IAccountRepository
    {
        private readonly Account[] _accounts;

        public StubAccountRepository(params string[] accountIds)
        {
            _accounts = accountIds
                .Select(id => new Account(
                    id,
                    $"user-{id}",
                    "hash",
                    id.StartsWith("admin", StringComparison.Ordinal)
                        ? new[] { Roles.Administrator }
                        : Array.Empty<string>(),
                    Array.Empty<string>(),
                    DateTime.UtcNow,
                    DateTime.UtcNow))
                .ToArray();
        }

        public IAsyncQueryable<Account> AsQueryable()
            => new MongoAsyncQueryable<Account>(_accounts.AsQueryable());

        public Task<Account> CreateAsync(Account entity) => Task.FromResult(entity);

        async Task IRepository<Account>.CreateAsync(Account entity) => await CreateAsync(entity);

        public Task CreateAsync(IEnumerable<Account> entities) => Task.CompletedTask;
        public Task DeleteAsync(Account entity) => Task.CompletedTask;
        public Task<long> DeleteAsync(Expression<Func<Account, bool>> predicate) => Task.FromResult(0L);
        public Task UpdateAsync(Account entity) => Task.CompletedTask;
        public Task<long> UpdateAsync(Expression expression) => Task.FromResult(0L);
    }

    private static ChargeProfitShareResolver CreateSut(
        StubAccountRepository accounts,
        StubOperationRepository operations,
        StubTeamRepository? teams = null) =>
        new(accounts, operations, teams ?? new StubTeamRepository());

    [Fact]
    public async Task ResolveSplitsAsync_OperatorWithoutMatchingTeam_ReturnsTeamNotFound()
    {
        var sut = CreateSut(
            new StubAccountRepository("op-42"),
            new StubOperationRepository("op-1"));

        var result = await sut.ResolveSplitsAsync("op-1", "op-42", 10m);

        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors, e => e.Code == PixPaymentErrorCodes.TeamNotFound);
    }

    [Fact]
    public async Task ResolveSplitsAsync_WithoutOperatorAndNoRecipients_ReturnsProfitShareRecipientsNotFound()
    {
        var sut = CreateSut(new StubAccountRepository(), new StubOperationRepository("op-1"));

        var result = await sut.ResolveSplitsAsync("op-1", null, 10m);

        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors, e => e.Code == PixPaymentErrorCodes.ProfitShareRecipientsNotFound);
    }

    [Fact]
    public async Task ResolveSplitsAsync_WithoutOperator_SplitsAmongOperationAdministrators()
    {
        var operation = new Operation(
            "op-1",
            "Operation",
            "desc",
            new[] { "admin-1", "admin-2" },
            Array.Empty<string>(),
            GatewaySelectionStrategy.PerStrawman,
            Array.Empty<string>(),
            Array.Empty<string>(),
            DateTime.UtcNow,
            DateTime.UtcNow);

        var sut = CreateSut(
            new StubAccountRepository(),
            new StubOperationRepository(operation));

        var result = await sut.ResolveSplitsAsync("op-1", null, 100m);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value!.Count);
        Assert.Equal(100m, result.Value.Sum(split => split.Amount));
    }

    [Fact]
    public async Task ResolveSplitsAsync_WithoutOperator_FallsBackToSystemAdministrators()
    {
        var operation = new Operation(
            "op-1",
            "Operation",
            "desc",
            Array.Empty<string>(),
            Array.Empty<string>(),
            GatewaySelectionStrategy.PerStrawman,
            Array.Empty<string>(),
            Array.Empty<string>(),
            DateTime.UtcNow,
            DateTime.UtcNow);

        var sut = CreateSut(
            new StubAccountRepository("admin-1", "admin-2"),
            new StubOperationRepository(operation));

        var result = await sut.ResolveSplitsAsync("op-1", null, 100m);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value!.Count);
        Assert.Equal(100m, result.Value.Sum(split => split.Amount));
    }

    [Fact]
    public async Task ResolveSplitsAsync_OperatorExists_SnapshotsProfitShare()
    {
        var team = TeamTestFactory.WithOperatorProfitShare("team-1", "op-1", "op-42", "straw-1", ("op-42", 100m));
        var sut = CreateSut(
            new StubAccountRepository("op-42"),
            new StubOperationRepository("op-1"),
            new StubTeamRepository(team));

        var result = await sut.ResolveSplitsAsync("op-1", "op-42", 100m);

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value!);
        Assert.Equal(100m, result.Value.Sum(split => split.Amount));
    }

    [Fact]
    public async Task ResolveSplitsAsync_OperatorAndMultipleCuts_SnapshotsAllSplits()
    {
        var team = TeamTestFactory.WithOperatorProfitShare("team-1", "op-1", "op", "sm", ("op", 60m), ("partner", 40m));
        var sut = CreateSut(
            new StubAccountRepository("op"),
            new StubOperationRepository("op-1"),
            new StubTeamRepository(team));

        var result = await sut.ResolveSplitsAsync("op-1", "op", 3m);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value!.Count);
        Assert.Equal(3m, result.Value.Sum(split => split.Amount));
    }

    [Fact]
    public async Task ResolveSplitsAsync_OperationNotFound_ReturnsOperationNotFound()
    {
        var sut = CreateSut(new StubAccountRepository(), new StubOperationRepository("op-1"));

        var result = await sut.ResolveSplitsAsync("missing-operation", null, 10m);

        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors, e => e.Code == PixPaymentErrorCodes.OperationNotFound);
    }
}

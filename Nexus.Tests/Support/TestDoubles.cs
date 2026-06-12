using System.Linq.Expressions;
using Aidan.Core.Linq;
using Aidan.Core.Patterns;
using Aidan.Mongo.Linq;
using Nexus.Accounts.Application.Services;
using Nexus.Accounts.Infrastructure.Password;
using Nexus.Actors;
using Nexus.Database.Models;
using Nexus.Gateways.Application.Services;
using Nexus.Gateways.Application.Services.Contracts;
using Nexus.Gateways.Aggregates;
using Nexus.Operations.Aggregates;
using Nexus.Operations.Application.Services;
using Nexus.Operations.Application.Services.Contracts;
using Nexus.Tests.Accounts;
using Nexus.Tests.Payments;

namespace Nexus.Tests.Support;

internal sealed class InMemoryOperationRepository : IOperationRepository
{
    private readonly List<Operation> _store = new();

    public IAsyncQueryable<Operation> AsQueryable()
        => new QueryableToAsyncQueryableAdapter<Operation>(_store.AsQueryable());

    public Task<Operation> CreateAsync(Operation entity)
    {
        var persisted = string.IsNullOrWhiteSpace(entity.Id)
            ? new Operation(
                Guid.NewGuid().ToString("N"),
                entity.Name,
                entity.Description,
                entity.AdministratorIds,
                entity.CreatedAt,
                entity.UpdatedAt)
            : entity;

        _store.Add(persisted);
        return Task.FromResult(persisted);
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
        _store.RemoveAll(x => x.Id == entity.Id);
        return Task.CompletedTask;
    }

    public Task<long> DeleteAsync(Expression<Func<Operation, bool>> predicate)
    {
        var compiled = predicate.Compile();
        var removed = _store.RemoveAll(x => compiled(x));
        return Task.FromResult((long)removed);
    }

    public Task UpdateAsync(Operation entity)
    {
        var index = _store.FindIndex(x => x.Id == entity.Id);
        if (index >= 0)
            _store[index] = entity;
        return Task.CompletedTask;
    }

    public Task<long> UpdateAsync(Expression expression) => Task.FromResult(0L);
}

internal sealed class InMemoryTeamRepository : ITeamRepository
{
    private readonly List<Team> _store = new();

    public IAsyncQueryable<Team> AsQueryable()
        => new QueryableToAsyncQueryableAdapter<Team>(_store.AsQueryable());

    public Task<Team> CreateAsync(Team entity)
    {
        var persisted = string.IsNullOrWhiteSpace(entity.Id)
            ? new Team(
                Guid.NewGuid().ToString("N"),
                entity.OperationId,
                entity.Name,
                entity.TeamLeaderId,
                entity.OperatorIds,
                entity.StrawManIds,
                entity.GatewaySelectionStrategy,
                entity.GatewayCredentialsIds,
                entity.GatewayCredentialsGroupIds,
                entity.OperatorProfitShareRules.ToList(),
                entity.CreatedAt,
                entity.UpdatedAt)
            : entity;

        _store.Add(persisted);
        return Task.FromResult(persisted);
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
        _store.RemoveAll(x => x.Id == entity.Id);
        return Task.CompletedTask;
    }

    public Task<long> DeleteAsync(Expression<Func<Team, bool>> predicate)
    {
        var compiled = predicate.Compile();
        var removed = _store.RemoveAll(x => compiled(x));
        return Task.FromResult((long)removed);
    }

    public Task UpdateAsync(Team entity)
    {
        var index = _store.FindIndex(x => x.Id == entity.Id);
        if (index >= 0)
            _store[index] = entity;
        return Task.CompletedTask;
    }

    public Task<long> UpdateAsync(Expression expression) => Task.FromResult(0L);
}

internal sealed class InMemoryGatewayCredentialsGroupRepository : IGatewayCredentialsGroupRepository
{
    private readonly List<GatewayCredentialsGroup> _store = new();

    public IAsyncQueryable<GatewayCredentialsGroup> AsQueryable()
        => new MongoAsyncQueryable<GatewayCredentialsGroup>(_store.AsQueryable());

    public Task<GatewayCredentialsGroup> CreateAsync(GatewayCredentialsGroup entity)
    {
        var persisted = string.IsNullOrWhiteSpace(entity.Id)
            ? new GatewayCredentialsGroup(
                Guid.NewGuid().ToString("N"),
                entity.Name,
                entity.GatewayCredentialsIds,
                entity.CreatedAt,
                entity.UpdatedAt)
            : entity;

        _store.Add(persisted);
        return Task.FromResult(persisted);
    }

    async Task IRepository<GatewayCredentialsGroup>.CreateAsync(GatewayCredentialsGroup entity)
    {
        await CreateAsync(entity);
    }

    public Task CreateAsync(IEnumerable<GatewayCredentialsGroup> entities)
    {
        _store.AddRange(entities);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(GatewayCredentialsGroup entity)
    {
        _store.RemoveAll(x => x.Id == entity.Id);
        return Task.CompletedTask;
    }

    public Task<long> DeleteAsync(Expression<Func<GatewayCredentialsGroup, bool>> predicate)
    {
        var compiled = predicate.Compile();
        var removed = _store.RemoveAll(x => compiled(x));
        return Task.FromResult((long)removed);
    }

    public Task UpdateAsync(GatewayCredentialsGroup entity)
    {
        var index = _store.FindIndex(x => x.Id == entity.Id);
        if (index >= 0)
            _store[index] = entity;
        return Task.CompletedTask;
    }

    public Task<long> UpdateAsync(Expression expression) => Task.FromResult(0L);
}

internal sealed class FakeGatewayCredentialsIdValidator : IGatewayCredentialsIdValidator
{
    private readonly HashSet<string> _existingIds;

    public FakeGatewayCredentialsIdValidator(IEnumerable<string>? existingIds = null)
    {
        _existingIds = existingIds is null
            ? new HashSet<string>(StringComparer.Ordinal)
            : new HashSet<string>(existingIds, StringComparer.Ordinal);
    }

    public void AddExisting(string id) => _existingIds.Add(id);

    public Task<bool> ExistsAsync(string credentialsId) =>
        Task.FromResult(_existingIds.Contains(credentialsId));
}

internal sealed class ActorTestContext
{
    public InMemoryOperationRepository Operations { get; } = new();
    public InMemoryTeamRepository Teams { get; } = new();
    public InMemoryGatewayCredentialsGroupRepository GatewayGroups { get; } = new();
    public FakeAccountIdValidator AccountIdValidator { get; } = new();
    public FakeGatewayCredentialsIdValidator GatewayCredentialsIdValidator { get; } = new();

    public OperationService CreateOperationService()
        => new(Operations, AccountIdValidator);

    public TeamService CreateTeamService()
        => new(
            Teams,
            Operations,
            AccountIdValidator,
            GatewayGroups,
            GatewayCredentialsIdValidator);

    public Administrator CreateAdministrator()
        => new(CreateOperationService(), Operations);

    public OperationAdministrator CreateOperationAdministrator()
        => new(CreateTeamService());

    public TeamLeader CreateTeamLeader()
        => new(CreateTeamService());

    public Operator CreateOperator(string operatorAccountId)
        => new(operatorAccountId, Operations, Teams);

    public UnauthenticatedUser CreateUnauthenticatedUser(InMemoryAccountRepository? accounts = null)
    {
        var repo = accounts ?? new InMemoryAccountRepository();
        var creator = new AccountCreator(
            repo,
            new UsernameValidator(repo),
            new PasswordValidator(),
            new PasswordHasher());
        return new UnauthenticatedUser(creator);
    }

    public async Task<Operation> SeedOperationAsync(
        string name = "Test Operation",
        string? description = null,
        string[]? administratorIds = null,
        string? id = null)
    {
        var now = DateTime.UtcNow;
        var operation = new Operation(
            Id: id ?? Guid.NewGuid().ToString("N"),
            Name: name,
            Description: description,
            AdministratorIds: administratorIds ?? Array.Empty<string>(),
            CreatedAt: now,
            UpdatedAt: now);
        return await Operations.CreateAsync(operation);
    }

    public async Task<Team> SeedTeamAsync(
        string operationId,
        string name = "Test Team",
        GatewaySelectionStrategy strategy = GatewaySelectionStrategy.PerStrawman,
        string? id = null,
        string[]? operatorIds = null)
    {
        var now = DateTime.UtcNow;
        var team = new Team(
            Id: id ?? Guid.NewGuid().ToString("N"),
            OperationId: operationId,
            Name: name,
            TeamLeaderId: null,
            OperatorIds: operatorIds ?? Array.Empty<string>(),
            StrawManIds: Array.Empty<string>(),
            GatewaySelectionStrategy: strategy,
            GatewayCredentialsIds: Array.Empty<string>(),
            GatewayCredentialsGroupIds: Array.Empty<string>(),
            OperatorProfitShareRules: Array.Empty<OperatorProfitShareRuleRecord>(),
            CreatedAt: now,
            UpdatedAt: now);
        return await Teams.CreateAsync(team);
    }

    public async Task<GatewayCredentialsGroup> SeedGatewayGroupAsync(
        string name = "Gateway Group",
        string? id = null)
    {
        var now = DateTime.UtcNow;
        var group = new GatewayCredentialsGroup(
            Id: id ?? Guid.NewGuid().ToString("N"),
            Name: name,
            GatewayCredentialsIds: Array.Empty<string>(),
            CreatedAt: now,
            UpdatedAt: now);
        return await GatewayGroups.CreateAsync(group);
    }

    public void RegisterAccount(string accountId) => AccountIdValidator.AddExisting(accountId);

    public void RegisterGatewayCredential(string credentialsId) =>
        GatewayCredentialsIdValidator.AddExisting(credentialsId);
}


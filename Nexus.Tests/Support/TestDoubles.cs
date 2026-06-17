using System.Linq.Expressions;
using Aidan.Core.Linq;
using Aidan.Core.Patterns;
using Aidan.Mongo.Linq;
using Nexus.Accounts.Application.Services;
using Nexus.Accounts.Aggregates;
using Nexus.Accounts.Infrastructure.Password;
using OperationAdministratorRole = Nexus.OperationAdministrator.Application.Services.OperationAdministrator;
using Nexus.TeamLeader.Application.Services;
using TeamLeaderRole = Nexus.TeamLeader.Application.Services.TeamLeader;
using AdministratorRole = Nexus.Administrator.Application.Services.Administrator;
using Nexus.Authorization;
using OperatorRole = Nexus.Operator.Application.Services.Operator;
using Nexus.Authentication.Application.Services;
using Nexus.Administrator.Application.Contracts;
using AdministratorTeamGatewayDetailsLoader = Nexus.Administrator.Application.Contracts.ITeamGatewayDetailsLoader;
using AdministratorTeamGatewayLookup = Nexus.Administrator.Application.Models.TeamGatewayLookup;
using Nexus.OperationAdministrator.Application.Contracts;
using OperationAdministratorTeamGatewayLookup = Nexus.OperationAdministrator.Application.Models.TeamGatewayLookup;
using Nexus.Administrator.Application.Services;
using Nexus.Database.Models;
using Nexus.Gateways.Application.Services;
using Nexus.Gateways.Application.Contracts;
using Nexus.Gateways.Aggregates;
using Nexus.Authorization.Application.Models;
using Nexus.OperationAdministrator.Application.Services;
using Nexus.Operator.Application.Services;
using Nexus.Operations.Aggregates;
using Nexus.Operations.Application.Services;
using Nexus.Operations.Application.Contracts;
using Nexus.Payments.Aggregates;
using Nexus.Payments.Application.Contracts;
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

internal sealed class InMemoryPaymentRepository : IPaymentRepository
{
    private readonly List<Payment> _store = new();

    public IAsyncQueryable<Payment> AsQueryable()
        => new QueryableToAsyncQueryableAdapter<Payment>(_store.AsQueryable());

    public Task<Payment> CreateAsync(Payment entity)
    {
        var persisted = string.IsNullOrWhiteSpace(entity.Id)
            ? new Payment(
                Guid.NewGuid().ToString("N"),
                entity.OperationId,
                entity.Gateway,
                entity.GatewayTransactionId,
                entity.Amount,
                entity.Status,
                entity.OperatorAccountId,
                entity.StrawManAccountId,
                entity.CreatedAt,
                entity.PaidAt,
                entity.RefundedAt,
                entity.DiedAt,
                entity.DeathReason)
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
        _store.RemoveAll(x => x.Id == entity.Id);
        return Task.CompletedTask;
    }

    public Task<long> DeleteAsync(Expression<Func<Payment, bool>> predicate)
    {
        var compiled = predicate.Compile();
        var removed = _store.RemoveAll(x => compiled(x));
        return Task.FromResult((long)removed);
    }

    public Task UpdateAsync(Payment entity)
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
    public InMemoryPaymentRepository Payments { get; } = new();
    public InMemoryGatewayCredentialsGroupRepository GatewayGroups { get; } = new();
    public InMemoryAccountRepository Accounts { get; } = new();
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

    public AdministratorRole CreateAdministrator()
        => new AdministratorRole(
            new AdministratorAccessPolicy(),
            CreateOperationService(),
            Operations,
            Accounts,
            Teams,
            new EmptyTeamGatewayDetailsLoader());

    public OperationAdministratorRole CreateOperationAdministrator()
        => new(
            new OperationAdministratorAccessPolicy(Operations, Teams),
            CreateTeamService(),
            Operations,
            Teams,
            Accounts,
            new EmptyOperationAdministratorTeamGatewayDetailsLoader());

    public RequesterIdentity CreateRequesterIdentity(
        string accountId = "op-admin-1",
        bool isGlobalAdministrator = false,
        params string[] additionalRoles)
    {
        var roles = new List<string>(additionalRoles);
        if (isGlobalAdministrator && !roles.Contains(Roles.Administrator, StringComparer.Ordinal))
            roles.Add(Roles.Administrator);

        return new RequesterIdentity(accountId, roles, Array.Empty<string>());
    }

    public TeamLeaderRole CreateTeamLeader()
        => new(
            new TeamLeaderAccessPolicy(Teams),
            CreateTeamService(),
            Operations,
            Teams,
            Accounts);

    public OperatorRole CreateOperator()
        => new(new OperatorAccessPolicy(), Operations, Teams, Accounts);

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
        string[]? operatorIds = null,
        string? teamLeaderId = null)
    {
        var now = DateTime.UtcNow;
        var team = new Team(
            Id: id ?? Guid.NewGuid().ToString("N"),
            OperationId: operationId,
            Name: name,
            TeamLeaderId: teamLeaderId,
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

    public async Task<Payment> SeedPaymentAsync(
        string operationId,
        string? operatorAccountId = null,
        string? id = null)
    {
        var now = DateTime.UtcNow;
        var payment = new Payment(
            Id: id ?? Guid.NewGuid().ToString("N"),
            OperationId: operationId,
            Gateway: PaymentGateway.None,
            GatewayTransactionId: string.Empty,
            Amount: 100m,
            Status: PaymentStatus.Pending,
            OperatorAccountId: operatorAccountId,
            StrawManAccountId: null,
            CreatedAt: now,
            PaidAt: null,
            RefundedAt: null,
            DiedAt: null,
            DeathReason: null);
        return await Payments.CreateAsync(payment);
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

    public async Task<Account> SeedAccountAsync(
        string username = "testuser",
        string? id = null,
        string[]? roles = null,
        string[]? permissions = null)
    {
        var now = DateTime.UtcNow;
        var account = new Account(
            Id: id ?? Guid.NewGuid().ToString("N"),
            Username: username,
            PasswordHash: "hash",
            Roles: roles ?? Array.Empty<string>(),
            Permissions: permissions ?? Array.Empty<string>(),
            CreatedAt: now,
            LastUpdatedAt: now);
        return await Accounts.CreateAsync(account);
    }

    public void RegisterGatewayCredential(string credentialsId) =>
        GatewayCredentialsIdValidator.AddExisting(credentialsId);
}

internal sealed class EmptyTeamGatewayDetailsLoader : AdministratorTeamGatewayDetailsLoader
{
    public Task<AdministratorTeamGatewayLookup> LoadAsync(
        IReadOnlyList<Team> teams,
        CancellationToken cancellationToken = default)
        => Task.FromResult(new AdministratorTeamGatewayLookup());
}

internal sealed class EmptyOperationAdministratorTeamGatewayDetailsLoader
    : Nexus.OperationAdministrator.Application.Contracts.ITeamGatewayDetailsLoader
{
    public Task<OperationAdministratorTeamGatewayLookup> LoadAsync(
        IReadOnlyList<Team> teams,
        CancellationToken cancellationToken = default)
        => Task.FromResult(new OperationAdministratorTeamGatewayLookup());
}


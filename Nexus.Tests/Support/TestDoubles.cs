using System.Linq.Expressions;
using Aidan.Core.Linq;
using Aidan.Core.Patterns;
using Aidan.Mongo.Linq;
using Nexus.Accounts.Application.Services;
using Nexus.Accounts.Aggregates;
using Nexus.Accounts.Infrastructure.Password;
using OperationAdministratorRole = Nexus.OperationAdministrators.Application.Services.OperationAdministrator;
using Nexus.TeamLeaders.Application.Services;
using TeamLeaderRole = Nexus.TeamLeaders.Application.Services.TeamLeader;
using AdministratorRole = Nexus.Administrators.Application.Services.Administrator;
using OperatorRole = Nexus.Operators.Application.Services.Operator;
using Nexus.Administrators.Application.Contracts;
using AdministratorTeamGatewayDetailsLoader = Nexus.Administrators.Application.Contracts.ITeamGatewayDetailsLoader;
using AdministratorTeamGatewayLookup = Nexus.Administrators.Application.Models.TeamGatewayLookup;
using Nexus.OperationAdministrators.Application.Contracts;
using OperationAdministratorTeamGatewayLookup = Nexus.OperationAdministrators.Application.Models.TeamGatewayLookup;
using Nexus.Administrators.Application.Services;
using Nexus.Database.Models;
using Nexus.Gateways.Application.Services;
using Nexus.Gateways.Application.Contracts;
using Nexus.Gateways.Aggregates;
using Nexus.OperationAdministrators.Application.Services;
using Nexus.Operators.Application.Services;
using Nexus.Operations.Aggregates;
using Nexus.Operations.Application.Services;
using Nexus.Operations.Application.Contracts;
using Nexus.Payments.Aggregates;
using Nexus.Payments.Application.Contracts;
using Nexus.Payments.Application.Services;
using Nexus.Tests.Payments;
using Nexus.Tests.Accounts;
using Nexus.Authentication.Application.Services;
using Nexus.Authorization;
using Nexus.Authorization.Application.Models;

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
                entity.StrawManIds,
                entity.GatewaySelectionStrategy,
                entity.GatewayCredentialsIds,
                entity.GatewayCredentialsGroupIds,
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
            ? PaymentTestFactory.Create(
                operationId: entity.OperationId,
                gateway: entity.Gateway,
                gatewayPaymentId: entity.GatewayTransactionId,
                amount: entity.Amount,
                splits: entity.Splits,
                status: entity.Status,
                settlementStatus: entity.SettlementStatus,
                operatorId: entity.OperatorId,
                strawManId: entity.StrawManId,
                createdAt: entity.CreatedAt,
                paidAt: entity.PaidAt,
                refundedAt: entity.RefundedAt,
                killedAt: entity.KilledAt,
                killReason: entity.KillReason,
                withdrawnAt: entity.WithdrawnAt)
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
        => new(
            Operations,
            Teams,
            AccountIdValidator,
            GatewayGroups,
            GatewayCredentialsIdValidator);

    public TeamService CreateTeamService()
        => new(
            Teams,
            Operations,
            AccountIdValidator,
            GatewayGroups,
            GatewayCredentialsIdValidator);

    public AccountUpdater CreateAccountUpdater()
        => new(
            Accounts,
            new UsernameValidator(Accounts),
            new PasswordValidator(),
            new PasswordHasher());

    public AdministratorRole CreateAdministrator()
    {
        var teamGatewayLoader = new EmptyTeamGatewayDetailsLoader();
        return new AdministratorRole(
            new AdministratorAccessPolicy(),
            new AdministratorOperationSearchService(Operations, Teams, Accounts, teamGatewayLoader),
            new AdministratorAccountSearchService(Accounts),
            new AdministratorAccountCommandService(CreateAccountUpdater()),
            new AdministratorOperationCommandService(CreateOperationService()),
            new AdministratorTeamCommandService(CreateTeamService()),
            new AdministratorTeamOperatorCommandService(CreateTeamService()),
            new AdministratorOperatorAssignmentSearchService(Accounts),
            new AdministratorProfitShareAccountSearchService(Accounts),
            new AdministratorOperationPickerSearchService(Operations),
            new StubAdministratorAccountNodeCommandService(),
            new StubAdministratorTransferCommandService(),
            new AdministratorPaymentSearchService(Payments),
            new AdministratorPaymentCommandService(new PaymentService(
                Accounts,
                Payments,
                Operations,
                Teams)),
            new StubAdministratorStrawManSettingsCommandService());
    }

    public OperationAdministratorRole CreateOperationAdministrator()
    {
        var teamGatewayLoader = new EmptyOperationAdministratorTeamGatewayDetailsLoader();
        var accountSearch = new OperationAdministratorAccountSearchService(Accounts);
        return new OperationAdministratorRole(
            new OperationAdministratorAccessPolicy(Operations, Teams),
            new OperationAdministratorOperationSearchService(Operations, Teams, Accounts, teamGatewayLoader),
            new OperationAdministratorTeamCommandService(CreateTeamService()),
            new OperationAdministratorOperationCommandService(CreateOperationService()),
            new OperationAdministratorTeamLeaderCandidateSearchService(accountSearch),
            new OperationAdministratorStrawManAssignmentSearchService(Accounts));
    }

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
            new TeamLeaderLedTeamsSearchService(Operations, Teams, Accounts),
            new TeamLeaderTeamCommandService(CreateTeamService()),
            new TeamLeaderOperatorAssignmentSearchService(Teams, Accounts),
            new TeamLeaderProfitShareAccountSearchService(Teams, Accounts));

    public OperatorRole CreateOperator()
        => new(
            new OperatorAccessPolicy(),
            new OperatorOperationSearchService(Operations, Teams, Accounts),
            new OperatorPaymentSearchService(Payments, Teams));

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
            StrawManIds: Array.Empty<string>(),
            GatewaySelectionStrategy: GatewaySelectionStrategy.PerStrawman,
            GatewayCredentialsIds: Array.Empty<string>(),
            GatewayCredentialsGroupIds: Array.Empty<string>(),
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
        string? operatorId = null,
        string? id = null)
    {
        var payment = PaymentTestFactory.Create(
            id: id,
            operationId: operationId,
            operatorId: operatorId,
            splits: operatorId is null
                ? Array.Empty<PaymentSplit>()
                : PaymentSplit.CreateSnapshot(100m, new[] { (operatorId, 100m) }));
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
        IReadOnlyList<Operation>? operations = null,
        CancellationToken cancellationToken = default)
        => Task.FromResult(new AdministratorTeamGatewayLookup());
}

internal sealed class EmptyOperationAdministratorTeamGatewayDetailsLoader
    : Nexus.OperationAdministrators.Application.Contracts.ITeamGatewayDetailsLoader
{
    public Task<OperationAdministratorTeamGatewayLookup> LoadAsync(
        IReadOnlyList<Team> teams,
        IReadOnlyList<Operation>? operations = null,
        CancellationToken cancellationToken = default)
        => Task.FromResult(new OperationAdministratorTeamGatewayLookup());
}

internal sealed class StubAdministratorAccountNodeCommandService : IAdministratorAccountNodeCommandService
{
    public Task<IResult<Nexus.AccountNodes.Aggregates.BankAccount>> CreateBankAccountAsync(
        Nexus.AccountNodes.Application.Contracts.CreateBankAccountRequest request) =>
        throw new NotImplementedException();

    public Task<IResult<Nexus.AccountNodes.Aggregates.CryptoWallet>> CreateCryptoWalletAsync(
        Nexus.AccountNodes.Application.Contracts.CreateCryptoWalletRequest request) =>
        throw new NotImplementedException();

    public Task<IResult<Nexus.AccountNodes.Aggregates.CryptoWallet>> UpsertCryptoWalletAddressAsync(
        Nexus.AccountNodes.Application.Contracts.UpsertCryptoWalletAddressRequest request) =>
        throw new NotImplementedException();

    public Task<IResult<Nexus.AccountNodes.Aggregates.BankAccount>> GetBankAccountAsync(string bankAccountId) =>
        throw new NotImplementedException();

    public Task<IResult<Nexus.AccountNodes.Aggregates.CryptoWallet>> GetCryptoWalletAsync(string cryptoWalletId) =>
        throw new NotImplementedException();

    public Task<IResult<Nexus.AccountNodes.Aggregates.BankAccount>> UpdateBankAccountLabelAsync(
        string bankAccountId,
        string? label) =>
        throw new NotImplementedException();

    public Task<IResult<SearchBankAccountsResponse>> SearchBankAccountsAsync(SearchBankAccountsRequest? request) =>
        throw new NotImplementedException();

    public Task<IResult<SearchCryptoWalletsResponse>> SearchCryptoWalletsAsync(SearchCryptoWalletsRequest? request) =>
        throw new NotImplementedException();
}

internal sealed class StubAdministratorTransferCommandService : IAdministratorTransferCommandService
{
    public Task<IResult<Nexus.Transfers.Aggregates.Transfer>> ExecuteWithdrawalAsync(
        Nexus.Transfers.Application.Contracts.WithdrawalTransferRequest request,
        CancellationToken cancellationToken = default) =>
        throw new NotImplementedException();

    public Task<IResult<Nexus.Transfers.Aggregates.Transfer>> ExecuteMovementAsync(
        Nexus.Transfers.Application.Contracts.MovementTransferRequest request,
        CancellationToken cancellationToken = default) =>
        throw new NotImplementedException();

    public Task<IResult<Nexus.Transfers.Aggregates.Transfer>> ExecutePayoutAsync(
        Nexus.Transfers.Application.Contracts.PayoutTransferRequest request,
        CancellationToken cancellationToken = default) =>
        throw new NotImplementedException();

    public Task<IResult<Nexus.Transfers.Aggregates.Transfer>> GetTransferAsync(string transferId) =>
        throw new NotImplementedException();

    public Task<IResult<SearchTransfersResponse>> SearchTransfersAsync(SearchTransfersRequest? request) =>
        throw new NotImplementedException();

    public Task<IResult<Nexus.Transfers.Application.Models.TransferTimelineDetails>> GetTransferTimelineAsync(string transferId) =>
        throw new NotImplementedException();
}

internal sealed class StubAdministratorStrawManSettingsCommandService : IAdministratorStrawManSettingsCommandService
{
    public Task<IResult<Nexus.StrawMen.Application.Contracts.StrawManSettingsDetails>> UpsertStrawManSettingsAsync(
        Nexus.Authorization.Application.Models.RequesterIdentity identity,
        string strawManId,
        decimal movementFeePercentage) =>
        throw new NotImplementedException();
}


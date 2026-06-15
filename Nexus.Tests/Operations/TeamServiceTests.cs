using System.Linq.Expressions;
using Nexus.Gateways.Application.Services.Contracts;
using Nexus.Operations.Application.Services.Contracts;
using Aidan.Core.Linq;
using Aidan.Core.Patterns;
using Aidan.Mongo.Linq;
using Nexus.Database.Models;
using Nexus.Gateways.Application.Services;
using Nexus.Gateways.Aggregates;
using Nexus.Operations.Aggregates;
using Nexus.Operations.Application.Services;
using Nexus.Tests.Payments;
using Nexus.Tests.Support;
using Xunit;
using Nexus.Operations.Errors;

namespace Nexus.Tests.Operations;

public sealed class TeamServiceTests
{
    private const string OperationId = "op-1";
    private const string TeamId = "team-1";
    private const string OtherTeamId = "team-2";
    private const string GroupId = "group-1";
    private const string CredentialId = "cred-1";
    private const string LeaderId = "leader-1";
    private const string OperatorId = "operator-1";
    private const string OtherOperatorId = "operator-2";
    private const string StrawManId = "straw-1";
    private const string AccountA = "account-a";
    private const string AccountB = "account-b";

    private static readonly DateTime SeedTime = new(2024, 1, 1, 12, 0, 0, DateTimeKind.Utc);

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

        public Team? FindById(string id) => _store.FirstOrDefault(t => t.Id == id);
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

    private sealed class InMemoryGatewayCredentialsGroupRepository : IGatewayCredentialsGroupRepository
    {
        private readonly List<GatewayCredentialsGroup> _store = new();

        public IAsyncQueryable<GatewayCredentialsGroup> AsQueryable()
            => new MongoAsyncQueryable<GatewayCredentialsGroup>(_store.AsQueryable());

        public Task<GatewayCredentialsGroup> CreateAsync(GatewayCredentialsGroup entity)
        {
            _store.Add(entity);
            return Task.FromResult(entity);
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

    private sealed class TestContext
    {
        public InMemoryTeamRepository Teams { get; } = new();
        public InMemoryOperationRepository Operations { get; } = new();
        public InMemoryGatewayCredentialsGroupRepository Groups { get; } = new();
        public FakeAccountIdValidator AccountValidator { get; } = new();
        public FakeGatewayCredentialsIdValidator CredentialValidator { get; } = new();

        public TeamService CreateSut() => new(
            Teams,
            Operations,
            AccountValidator,
            Groups,
            CredentialValidator);
    }

    private static TestContext CreateContextWithOperation()
    {
        var ctx = new TestContext();
        SeedOperation(ctx.Operations);
        return ctx;
    }

    private static TestContext CreateContextWithTeam(
        GatewaySelectionStrategy strategy = GatewaySelectionStrategy.PerStrawman,
        string teamId = TeamId)
    {
        var ctx = CreateContextWithOperation();
        SeedTeam(ctx.Teams, teamId: teamId, strategy: strategy);
        return ctx;
    }

    private static void SeedOperation(InMemoryOperationRepository repo, string id = OperationId)
    {
        var operation = new Operation(
            id,
            "Test Operation",
            "Description",
            Array.Empty<string>(),
            SeedTime,
            SeedTime);
        repo.CreateAsync(operation).GetAwaiter().GetResult();
    }

    private static void SeedTeam(
        InMemoryTeamRepository repo,
        string teamId = TeamId,
        string operationId = OperationId,
        string name = "Team Alpha",
        GatewaySelectionStrategy strategy = GatewaySelectionStrategy.PerStrawman,
        string? teamLeaderId = null,
        IReadOnlyList<string>? operatorIds = null,
        IReadOnlyList<string>? strawManIds = null,
        IReadOnlyList<string>? gatewayCredentialsIds = null,
        IReadOnlyList<string>? gatewayCredentialsGroupIds = null)
    {
        var team = new Team(
            teamId,
            operationId,
            name,
            teamLeaderId,
            operatorIds ?? Array.Empty<string>(),
            strawManIds ?? Array.Empty<string>(),
            strategy,
            gatewayCredentialsIds ?? Array.Empty<string>(),
            gatewayCredentialsGroupIds ?? Array.Empty<string>(),
            Array.Empty<OperatorProfitShareRuleRecord>(),
            SeedTime,
            SeedTime);
        repo.CreateAsync(team).GetAwaiter().GetResult();
    }

    private static void SeedGroup(
        InMemoryGatewayCredentialsGroupRepository repo,
        string id = GroupId,
        IReadOnlyList<string>? credentialIds = null)
    {
        var group = new GatewayCredentialsGroup(
            id,
            "Test Group",
            credentialIds ?? Array.Empty<string>(),
            SeedTime,
            SeedTime);
        repo.CreateAsync(group).GetAwaiter().GetResult();
    }

    // CreateTeamAsync

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task CreateTeamAsync_NameMissing_ReturnsNameInvalid(string? name)
    {
        var ctx = CreateContextWithOperation();
        var sut = ctx.CreateSut();

        var result = await sut.CreateTeamAsync(OperationId, name);

        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors, e => e.Code == TeamErrorCodes.NameInvalid);
    }

    [Fact]
    public async Task CreateTeamAsync_NameTooLong_ReturnsError()
    {
        var ctx = CreateContextWithOperation();
        var sut = ctx.CreateSut();
        var tooLongName = new string('A', Team.MaxNameLength + 1);

        var result = await sut.CreateTeamAsync(OperationId, tooLongName);

        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors, e => e.Code == TeamErrorCodes.NameTooLong);
    }

    [Fact]
    public async Task CreateTeamAsync_NameAlreadyExists_IgnoresCaseAndSpaces()
    {
        var ctx = CreateContextWithOperation();
        SeedTeam(ctx.Teams, name: "Existing Team");
        var sut = ctx.CreateSut();

        var result = await sut.CreateTeamAsync(OperationId, "  existing team  ");

        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors, e => e.Code == TeamErrorCodes.NameAlreadyExists);
    }

    [Fact]
    public async Task CreateTeamAsync_OperationNotFound_ReturnsError()
    {
        var ctx = new TestContext();
        var sut = ctx.CreateSut();

        var result = await sut.CreateTeamAsync("missing-op", "New Team");

        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors, e => e.Code == TeamErrorCodes.OperationNotFound);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task CreateTeamAsync_OperationIdInvalid_ReturnsError(string? operationId)
    {
        var ctx = CreateContextWithOperation();
        var sut = ctx.CreateSut();

        var result = await sut.CreateTeamAsync(operationId!, "New Team");

        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors, e => e.Code == TeamErrorCodes.OperationIdInvalid);
    }

    [Fact]
    public async Task CreateTeamAsync_ValidInput_CreatesTeam()
    {
        var ctx = CreateContextWithOperation();
        var sut = ctx.CreateSut();

        var result = await sut.CreateTeamAsync(OperationId, "  New Team  ");

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(OperationId, result.Value!.OperationId);
        Assert.Equal("New Team", result.Value.Name);
        Assert.Null(result.Value.TeamLeader);

        var persisted = ctx.Teams.AsQueryable().FirstOrDefault(t => t.Name == "New Team");
        Assert.NotNull(persisted);
        Assert.Equal(GatewaySelectionStrategy.PerStrawman, persisted.GatewaySelectionStrategy);
    }

    // DeleteTeamAsync

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task DeleteTeamAsync_TeamIdInvalid_ReturnsError(string? teamId)
    {
        var ctx = CreateContextWithTeam();
        var sut = ctx.CreateSut();

        var result = await sut.DeleteTeamAsync(teamId!);

        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors, e => e.Code == TeamErrorCodes.TeamIdInvalid);
    }

    [Fact]
    public async Task DeleteTeamAsync_TeamNotFound_ReturnsError()
    {
        var ctx = CreateContextWithOperation();
        var sut = ctx.CreateSut();

        var result = await sut.DeleteTeamAsync("missing-team");

        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors, e => e.Code == TeamErrorCodes.TeamNotFound);
    }

    [Fact]
    public async Task DeleteTeamAsync_ExistingTeam_DeletesTeam()
    {
        var ctx = CreateContextWithTeam();
        var sut = ctx.CreateSut();

        var result = await sut.DeleteTeamAsync(TeamId);

        Assert.True(result.IsSuccess);
        Assert.Null(ctx.Teams.FindById(TeamId));
    }

    // AssignTeamLeaderAsync

    [Fact]
    public async Task AssignTeamLeaderAsync_TeamNotFound_ReturnsError()
    {
        var ctx = CreateContextWithOperation();
        ctx.AccountValidator.AddExisting(LeaderId);
        var sut = ctx.CreateSut();

        var result = await sut.AssignTeamLeaderAsync("missing", LeaderId);

        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors, e => e.Code == TeamErrorCodes.TeamNotFound);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task AssignTeamLeaderAsync_LeaderIdInvalid_ReturnsError(string? leaderId)
    {
        var ctx = CreateContextWithTeam();
        var sut = ctx.CreateSut();

        var result = await sut.AssignTeamLeaderAsync(TeamId, leaderId!);

        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors, e => e.Code == TeamErrorCodes.TeamLeaderInvalid);
    }

    [Fact]
    public async Task AssignTeamLeaderAsync_LeaderAccountNotFound_ReturnsError()
    {
        var ctx = CreateContextWithTeam();
        var sut = ctx.CreateSut();

        var result = await sut.AssignTeamLeaderAsync(TeamId, LeaderId);

        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors, e => e.Code == TeamErrorCodes.TeamLeaderAccountNotFound);
    }

    [Fact]
    public async Task AssignTeamLeaderAsync_LeaderAlreadyAssigned_ReturnsError()
    {
        var ctx = CreateContextWithOperation();
        ctx.AccountValidator.AddExisting(LeaderId);
        ctx.AccountValidator.AddExisting("other-leader");
        SeedTeam(ctx.Teams);
        var sut = ctx.CreateSut();
        await sut.AssignTeamLeaderAsync(TeamId, LeaderId);

        var result = await sut.AssignTeamLeaderAsync(TeamId, "other-leader");

        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors, e => e.Code == TeamErrorCodes.TeamLeaderAlreadyAssigned);
    }

    [Fact]
    public async Task AssignTeamLeaderAsync_ValidLeader_AssignsLeader()
    {
        var ctx = CreateContextWithTeam();
        ctx.AccountValidator.AddExisting(LeaderId);
        var sut = ctx.CreateSut();

        var result = await sut.AssignTeamLeaderAsync(TeamId, $"  {LeaderId}  ");

        Assert.True(result.IsSuccess);
        var team = ctx.Teams.FindById(TeamId);
        Assert.Equal(LeaderId, team!.TeamLeaderId);
    }

    // UnassignTeamLeaderAsync

    [Fact]
    public async Task UnassignTeamLeaderAsync_NoLeaderAssigned_ReturnsError()
    {
        var ctx = CreateContextWithTeam();
        var sut = ctx.CreateSut();

        var result = await sut.UnassignTeamLeaderAsync(TeamId);

        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors, e => e.Code == TeamErrorCodes.TeamLeaderNotAssigned);
    }

    [Fact]
    public async Task UnassignTeamLeaderAsync_LeaderAssigned_UnassignsLeader()
    {
        var ctx = CreateContextWithOperation();
        ctx.AccountValidator.AddExisting(LeaderId);
        SeedTeam(ctx.Teams);
        var sut = ctx.CreateSut();
        await sut.AssignTeamLeaderAsync(TeamId, LeaderId);

        var result = await sut.UnassignTeamLeaderAsync(TeamId);

        Assert.True(result.IsSuccess);
        Assert.Null(ctx.Teams.FindById(TeamId)!.TeamLeaderId);
    }

    // AssignOperatorAsync

    [Fact]
    public async Task AssignOperatorAsync_OperatorAccountNotFound_ReturnsError()
    {
        var ctx = CreateContextWithTeam();
        var sut = ctx.CreateSut();

        var result = await sut.AssignOperatorAsync(TeamId, OperatorId);

        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors, e => e.Code == TeamErrorCodes.OperatorAccountNotFound);
    }

    [Fact]
    public async Task AssignOperatorAsync_OperatorAlreadyAssignedToAnotherTeam_ReturnsError()
    {
        var ctx = CreateContextWithTeam();
        ctx.AccountValidator.AddExisting(OperatorId);
        SeedTeam(ctx.Teams, teamId: OtherTeamId, operatorIds: new[] { OperatorId });
        var sut = ctx.CreateSut();

        var result = await sut.AssignOperatorAsync(TeamId, OperatorId);

        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors, e => e.Code == TeamErrorCodes.OperatorAlreadyAssignedToAnotherTeam);
    }

    [Fact]
    public async Task AssignOperatorAsync_OperatorAlreadyOnSameTeam_ReturnsError()
    {
        var ctx = CreateContextWithOperation();
        ctx.AccountValidator.AddExisting(OperatorId);
        SeedTeam(ctx.Teams, operatorIds: new[] { OperatorId });
        var sut = ctx.CreateSut();

        var result = await sut.AssignOperatorAsync(TeamId, OperatorId);

        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors, e => e.Code == TeamErrorCodes.OperatorAlreadyAssigned);
    }

    [Fact]
    public async Task AssignOperatorAsync_ValidOperator_AssignsOperator()
    {
        var ctx = CreateContextWithTeam();
        ctx.AccountValidator.AddExisting(OperatorId);
        var sut = ctx.CreateSut();

        var result = await sut.AssignOperatorAsync(TeamId, OperatorId);

        Assert.True(result.IsSuccess);
        var team = ctx.Teams.FindById(TeamId);
        Assert.Contains(OperatorId, team!.OperatorIds);
        Assert.True(team.OperatorProfitShareRules.Any(r => r.OperatorId == OperatorId));
    }

    // UnassignOperatorAsync

    [Fact]
    public async Task UnassignOperatorAsync_OperatorNotAssigned_ReturnsError()
    {
        var ctx = CreateContextWithTeam();
        var sut = ctx.CreateSut();

        var result = await sut.UnassignOperatorAsync(TeamId, OperatorId);

        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors, e => e.Code == TeamErrorCodes.OperatorNotAssigned);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task UnassignOperatorAsync_OperatorIdInvalid_ReturnsError(string? operatorId)
    {
        var ctx = CreateContextWithTeam();
        var sut = ctx.CreateSut();

        var result = await sut.UnassignOperatorAsync(TeamId, operatorId!);

        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors, e => e.Code == TeamErrorCodes.OperatorInvalid);
    }

    [Fact]
    public async Task UnassignOperatorAsync_AssignedOperator_UnassignsOperator()
    {
        var ctx = CreateContextWithOperation();
        SeedTeam(ctx.Teams, operatorIds: new[] { OperatorId });
        var sut = ctx.CreateSut();

        var result = await sut.UnassignOperatorAsync(TeamId, OperatorId);

        Assert.True(result.IsSuccess);
        var team = ctx.Teams.FindById(TeamId);
        Assert.DoesNotContain(OperatorId, team!.OperatorIds);
        Assert.False(team.OperatorProfitShareRules.Any(r => r.OperatorId == OperatorId));
    }

    // AssignStrawManAsync

    [Fact]
    public async Task AssignStrawManAsync_StrawManAccountNotFound_ReturnsError()
    {
        var ctx = CreateContextWithTeam();
        var sut = ctx.CreateSut();

        var result = await sut.AssignStrawManAsync(TeamId, StrawManId);

        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors, e => e.Code == TeamErrorCodes.StrawManAccountNotFound);
    }

    [Fact]
    public async Task AssignStrawManAsync_StrawManAlreadyAssigned_ReturnsError()
    {
        var ctx = CreateContextWithOperation();
        ctx.AccountValidator.AddExisting(StrawManId);
        SeedTeam(ctx.Teams, strawManIds: new[] { StrawManId });
        var sut = ctx.CreateSut();

        var result = await sut.AssignStrawManAsync(TeamId, StrawManId);

        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors, e => e.Code == TeamErrorCodes.StrawManAlreadyAssigned);
    }

    [Fact]
    public async Task AssignStrawManAsync_ValidStrawMan_AssignsStrawMan()
    {
        var ctx = CreateContextWithTeam();
        ctx.AccountValidator.AddExisting(StrawManId);
        var sut = ctx.CreateSut();

        var result = await sut.AssignStrawManAsync(TeamId, StrawManId);

        Assert.True(result.IsSuccess);
        Assert.Contains(StrawManId, ctx.Teams.FindById(TeamId)!.StrawManIds);
    }

    // UnassignStrawManAsync

    [Fact]
    public async Task UnassignStrawManAsync_StrawManNotAssigned_ReturnsError()
    {
        var ctx = CreateContextWithTeam();
        var sut = ctx.CreateSut();

        var result = await sut.UnassignStrawManAsync(TeamId, StrawManId);

        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors, e => e.Code == TeamErrorCodes.StrawManNotAssigned);
    }

    [Fact]
    public async Task UnassignStrawManAsync_AssignedStrawMan_UnassignsStrawMan()
    {
        var ctx = CreateContextWithOperation();
        SeedTeam(ctx.Teams, strawManIds: new[] { StrawManId });
        var sut = ctx.CreateSut();

        var result = await sut.UnassignStrawManAsync(TeamId, StrawManId);

        Assert.True(result.IsSuccess);
        Assert.DoesNotContain(StrawManId, ctx.Teams.FindById(TeamId)!.StrawManIds);
    }

    // SetGatewaySelectionStrategyAsync

    [Fact]
    public async Task SetGatewaySelectionStrategyAsync_TeamNotFound_ReturnsError()
    {
        var ctx = CreateContextWithOperation();
        var sut = ctx.CreateSut();

        var result = await sut.SetGatewaySelectionStrategyAsync("missing", GatewaySelectionStrategy.Manual);

        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors, e => e.Code == TeamErrorCodes.TeamNotFound);
    }

    [Fact]
    public async Task SetGatewaySelectionStrategyAsync_InvalidStrategy_ReturnsError()
    {
        var ctx = CreateContextWithTeam();
        var sut = ctx.CreateSut();
        var invalidStrategy = (GatewaySelectionStrategy)999;

        var result = await sut.SetGatewaySelectionStrategyAsync(TeamId, invalidStrategy);

        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors, e => e.Code == TeamErrorCodes.GatewaySelectionStrategyInvalid);
    }

    [Fact]
    public async Task SetGatewaySelectionStrategyAsync_ValidStrategy_UpdatesStrategy()
    {
        var ctx = CreateContextWithTeam();
        var sut = ctx.CreateSut();

        var result = await sut.SetGatewaySelectionStrategyAsync(TeamId, GatewaySelectionStrategy.PerGroup);

        Assert.True(result.IsSuccess);
        Assert.Equal(GatewaySelectionStrategy.PerGroup, ctx.Teams.FindById(TeamId)!.GatewaySelectionStrategy);
    }

    // AssignGatewayCredentialsGroupAsync

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task AssignGatewayCredentialsGroupAsync_GroupIdInvalid_ReturnsError(string? groupId)
    {
        var ctx = CreateContextWithTeam(strategy: GatewaySelectionStrategy.PerGroup);
        var sut = ctx.CreateSut();

        var result = await sut.AssignGatewayCredentialsGroupAsync(TeamId, groupId!);

        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors, e => e.Code == TeamErrorCodes.GatewayCredentialsGroupInvalid);
    }

    [Fact]
    public async Task AssignGatewayCredentialsGroupAsync_GroupNotFound_ReturnsError()
    {
        var ctx = CreateContextWithTeam(strategy: GatewaySelectionStrategy.PerGroup);
        var sut = ctx.CreateSut();

        var result = await sut.AssignGatewayCredentialsGroupAsync(TeamId, "missing-group");

        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors, e => e.Code == TeamErrorCodes.GatewayCredentialsGroupNotFound);
    }

    [Fact]
    public async Task AssignGatewayCredentialsGroupAsync_StrategyMismatch_ReturnsError()
    {
        var ctx = CreateContextWithTeam(strategy: GatewaySelectionStrategy.PerStrawman);
        SeedGroup(ctx.Groups);
        var sut = ctx.CreateSut();

        var result = await sut.AssignGatewayCredentialsGroupAsync(TeamId, GroupId);

        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors, e => e.Code == TeamErrorCodes.GatewayCredentialsGroupStrategyMismatch);
    }

    [Fact]
    public async Task AssignGatewayCredentialsGroupAsync_AlreadyAssigned_ReturnsError()
    {
        var ctx = CreateContextWithOperation();
        SeedTeam(
            ctx.Teams,
            strategy: GatewaySelectionStrategy.PerGroup,
            gatewayCredentialsGroupIds: new[] { GroupId });
        SeedGroup(ctx.Groups);
        var sut = ctx.CreateSut();

        var result = await sut.AssignGatewayCredentialsGroupAsync(TeamId, GroupId);

        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors, e => e.Code == TeamErrorCodes.GatewayCredentialsGroupAlreadyAssigned);
    }

    [Fact]
    public async Task AssignGatewayCredentialsGroupAsync_PerGroupStrategy_AssignsGroup()
    {
        var ctx = CreateContextWithTeam(strategy: GatewaySelectionStrategy.PerGroup);
        SeedGroup(ctx.Groups);
        var sut = ctx.CreateSut();

        var result = await sut.AssignGatewayCredentialsGroupAsync(TeamId, $"  {GroupId}  ");

        Assert.True(result.IsSuccess);
        Assert.Contains(GroupId, ctx.Teams.FindById(TeamId)!.GatewayCredentialsGroupIds);
    }

    // UnassignGatewayCredentialsGroupAsync

    [Fact]
    public async Task UnassignGatewayCredentialsGroupAsync_StrategyMismatch_ReturnsError()
    {
        var ctx = CreateContextWithTeam(strategy: GatewaySelectionStrategy.PerStrawman);
        var sut = ctx.CreateSut();

        var result = await sut.UnassignGatewayCredentialsGroupAsync(TeamId, GroupId);

        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors, e => e.Code == TeamErrorCodes.GatewayCredentialsGroupStrategyMismatch);
    }

    [Fact]
    public async Task UnassignGatewayCredentialsGroupAsync_GroupNotAssigned_ReturnsError()
    {
        var ctx = CreateContextWithTeam(strategy: GatewaySelectionStrategy.PerGroup);
        var sut = ctx.CreateSut();

        var result = await sut.UnassignGatewayCredentialsGroupAsync(TeamId, GroupId);

        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors, e => e.Code == TeamErrorCodes.GatewayCredentialsGroupNotAssigned);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task UnassignGatewayCredentialsGroupAsync_GroupIdInvalid_ReturnsError(string? groupId)
    {
        var ctx = CreateContextWithTeam(strategy: GatewaySelectionStrategy.PerGroup);
        var sut = ctx.CreateSut();

        var result = await sut.UnassignGatewayCredentialsGroupAsync(TeamId, groupId!);

        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors, e => e.Code == TeamErrorCodes.GatewayCredentialsGroupInvalid);
    }

    [Fact]
    public async Task UnassignGatewayCredentialsGroupAsync_AssignedGroup_UnassignsWithTrimmedGroupId()
    {
        var ctx = CreateContextWithOperation();
        SeedTeam(
            ctx.Teams,
            strategy: GatewaySelectionStrategy.PerGroup,
            gatewayCredentialsGroupIds: new[] { GroupId });
        var sut = ctx.CreateSut();

        var result = await sut.UnassignGatewayCredentialsGroupAsync(TeamId, $"  {GroupId}  ");

        Assert.True(result.IsSuccess);
        Assert.DoesNotContain(GroupId, ctx.Teams.FindById(TeamId)!.GatewayCredentialsGroupIds);
    }

    // AssignGatewayCredentialsAsync

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task AssignGatewayCredentialsAsync_CredentialIdInvalid_ReturnsError(string? credentialId)
    {
        var ctx = CreateContextWithTeam(strategy: GatewaySelectionStrategy.Manual);
        var sut = ctx.CreateSut();

        var result = await sut.AssignGatewayCredentialsAsync(TeamId, credentialId!);

        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors, e => e.Code == TeamErrorCodes.GatewayCredentialInvalid);
    }

    [Fact]
    public async Task AssignGatewayCredentialsAsync_CredentialNotFound_ReturnsError()
    {
        var ctx = CreateContextWithTeam(strategy: GatewaySelectionStrategy.Manual);
        var sut = ctx.CreateSut();

        var result = await sut.AssignGatewayCredentialsAsync(TeamId, CredentialId);

        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors, e => e.Code == TeamErrorCodes.GatewayCredentialInvalid);
    }

    [Fact]
    public async Task AssignGatewayCredentialsAsync_ManualStrategyDisabled_ReturnsError()
    {
        var ctx = CreateContextWithTeam(strategy: GatewaySelectionStrategy.PerStrawman);
        ctx.CredentialValidator.AddExisting(CredentialId);
        var sut = ctx.CreateSut();

        var result = await sut.AssignGatewayCredentialsAsync(TeamId, CredentialId);

        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors, e => e.Code == TeamErrorCodes.ManualGatewayCredentialsDisabled);
    }

    [Fact]
    public async Task AssignGatewayCredentialsAsync_ManualStrategy_AssignsCredential()
    {
        var ctx = CreateContextWithTeam(strategy: GatewaySelectionStrategy.Manual);
        ctx.CredentialValidator.AddExisting(CredentialId);
        var sut = ctx.CreateSut();

        var result = await sut.AssignGatewayCredentialsAsync(TeamId, $"  {CredentialId}  ");

        Assert.True(result.IsSuccess);
        Assert.Contains(CredentialId, ctx.Teams.FindById(TeamId)!.GatewayCredentialsIds);
    }

    // UnassignGatewayCredentialsAsync

    [Fact]
    public async Task UnassignGatewayCredentialsAsync_NotManualStrategy_ReturnsError()
    {
        var ctx = CreateContextWithTeam(strategy: GatewaySelectionStrategy.PerStrawman);
        var sut = ctx.CreateSut();

        var result = await sut.UnassignGatewayCredentialsAsync(TeamId, CredentialId);

        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors, e => e.Code == TeamErrorCodes.ManualGatewayCredentialsDisabled);
    }

    [Fact]
    public async Task UnassignGatewayCredentialsAsync_CredentialNotAssigned_ReturnsError()
    {
        var ctx = CreateContextWithTeam(strategy: GatewaySelectionStrategy.Manual);
        var sut = ctx.CreateSut();

        var result = await sut.UnassignGatewayCredentialsAsync(TeamId, CredentialId);

        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors, e => e.Code == TeamErrorCodes.GatewayCredentialNotAssigned);
    }

    [Fact]
    public async Task UnassignGatewayCredentialsAsync_AssignedCredential_UnassignsCredential()
    {
        var ctx = CreateContextWithOperation();
        SeedTeam(
            ctx.Teams,
            strategy: GatewaySelectionStrategy.Manual,
            gatewayCredentialsIds: new[] { CredentialId });
        var sut = ctx.CreateSut();

        var result = await sut.UnassignGatewayCredentialsAsync(TeamId, CredentialId);

        Assert.True(result.IsSuccess);
        Assert.DoesNotContain(CredentialId, ctx.Teams.FindById(TeamId)!.GatewayCredentialsIds);
    }

    // SetOperatorProfitShareRuleAsync

    [Fact]
    public async Task SetOperatorProfitShareRuleAsync_OperatorNotAssigned_ReturnsError()
    {
        var ctx = CreateContextWithTeam();
        ctx.AccountValidator.AddExisting(AccountA);
        var sut = ctx.CreateSut();

        var cuts = new[] { new ProfitSplit(AccountA, 100m) };
        var result = await sut.SetOperatorProfitShareRuleAsync(TeamId, OperatorId, cuts);

        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors, e => e.Code == TeamErrorCodes.OperatorNotAssigned);
    }

    [Fact]
    public async Task SetOperatorProfitShareRuleAsync_EmptyCuts_ReturnsError()
    {
        var ctx = CreateContextWithOperation();
        SeedTeam(ctx.Teams, operatorIds: new[] { OperatorId });
        var sut = ctx.CreateSut();

        var result = await sut.SetOperatorProfitShareRuleAsync(TeamId, OperatorId, Array.Empty<ProfitSplit>());

        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors, e => e.Code == TeamErrorCodes.ProfitShareRuleEmpty);
    }

    [Fact]
    public async Task SetOperatorProfitShareRuleAsync_CutAccountNotFound_ReturnsError()
    {
        var ctx = CreateContextWithOperation();
        SeedTeam(ctx.Teams, operatorIds: new[] { OperatorId });
        var sut = ctx.CreateSut();

        var cuts = new[] { new ProfitSplit("missing-account", 100m) };
        var result = await sut.SetOperatorProfitShareRuleAsync(TeamId, OperatorId, cuts);

        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors, e => e.Code == TeamErrorCodes.ProfitShareCutAccountNotFound);
    }

    [Fact]
    public async Task SetOperatorProfitShareRuleAsync_EmptyCutAccountId_ReturnsError()
    {
        var ctx = CreateContextWithOperation();
        SeedTeam(ctx.Teams, operatorIds: new[] { OperatorId });
        var sut = ctx.CreateSut();

        var cuts = new[] { new ProfitSplit("   ", 100m) };
        var result = await sut.SetOperatorProfitShareRuleAsync(TeamId, OperatorId, cuts);

        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors, e => e.Code == TeamErrorCodes.ProfitShareCutAccountInvalid);
    }

    [Fact]
    public async Task SetOperatorProfitShareRuleAsync_DuplicateCutAccounts_ReturnsError()
    {
        var ctx = CreateContextWithOperation();
        ctx.AccountValidator.AddExisting(AccountA);
        SeedTeam(ctx.Teams, operatorIds: new[] { OperatorId });
        var sut = ctx.CreateSut();

        var cuts = new[]
        {
            new ProfitSplit(AccountA, 50m),
            new ProfitSplit(AccountA, 50m)
        };
        var result = await sut.SetOperatorProfitShareRuleAsync(TeamId, OperatorId, cuts);

        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors, e => e.Code == TeamErrorCodes.ProfitShareCutDuplicateAccount);
    }

    [Fact]
    public async Task SetOperatorProfitShareRuleAsync_CutsDoNotTotal100Percent_ReturnsError()
    {
        var ctx = CreateContextWithOperation();
        ctx.AccountValidator.AddExisting(AccountA);
        SeedTeam(ctx.Teams, operatorIds: new[] { OperatorId });
        var sut = ctx.CreateSut();

        var cuts = new[] { new ProfitSplit(AccountA, 50m) };
        var result = await sut.SetOperatorProfitShareRuleAsync(TeamId, OperatorId, cuts);

        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors, e => e.Code == TeamErrorCodes.ProfitShareCutsMustTotal100Percent);
    }

    [Fact]
    public async Task SetOperatorProfitShareRuleAsync_InvalidPercentage_ReturnsError()
    {
        var ctx = CreateContextWithOperation();
        ctx.AccountValidator.AddExisting(AccountA);
        SeedTeam(ctx.Teams, operatorIds: new[] { OperatorId });
        var sut = ctx.CreateSut();

        var cuts = new[] { new ProfitSplit(AccountA, 0m) };
        var result = await sut.SetOperatorProfitShareRuleAsync(TeamId, OperatorId, cuts);

        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors, e => e.Code == TeamErrorCodes.ProfitShareCutPercentageInvalid);
    }

    [Fact]
    public async Task SetOperatorProfitShareRuleAsync_ValidCuts_SetsRule()
    {
        var ctx = CreateContextWithOperation();
        ctx.AccountValidator.AddExisting(AccountA);
        ctx.AccountValidator.AddExisting(AccountB);
        SeedTeam(ctx.Teams, operatorIds: new[] { OperatorId });
        var sut = ctx.CreateSut();

        var cuts = new[]
        {
            new ProfitSplit(AccountA, 60m),
            new ProfitSplit(AccountB, 40m)
        };
        var result = await sut.SetOperatorProfitShareRuleAsync(TeamId, OperatorId, cuts);

        Assert.True(result.IsSuccess);
        var team = ctx.Teams.FindById(TeamId)!;
        var rule = team.OperatorProfitShareRules.First(r => r.OperatorId == OperatorId);
        Assert.Equal(60m, rule.Cuts.First(c => c.AccountId == AccountA).Percentage);
        Assert.Equal(40m, rule.Cuts.First(c => c.AccountId == AccountB).Percentage);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task SetOperatorProfitShareRuleAsync_OperatorIdInvalid_ReturnsError(string? operatorId)
    {
        var ctx = CreateContextWithTeam();
        var sut = ctx.CreateSut();

        var cuts = new[] { new ProfitSplit(AccountA, 100m) };
        var result = await sut.SetOperatorProfitShareRuleAsync(TeamId, operatorId!, cuts);

        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors, e => e.Code == TeamErrorCodes.OperatorInvalid);
    }
}

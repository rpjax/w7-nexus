using System.Linq.Expressions;
using Aidan.Core.Linq;
using Aidan.Core.Patterns;
using Aidan.Mongo.Linq;
using Nexus.Charges.Application.Contracts;
using Nexus.Charges.Application.Requests;
using Nexus.Charges.Application.Services;
using Nexus.Database.Models;
using Nexus.Gateways.Aggregates;
using Nexus.Gateways.Application.Contracts;
using Nexus.Gateways.Application.Models;
using Nexus.Gateways.Frendz.Application.Contracts;
using Nexus.Gateways.Frendz.Application.Models;
using Nexus.Gateways.SigiloPay.Application.Contracts;
using Nexus.Gateways.SigiloPay.Application.Models;
using Nexus.Gateways.Wintech.Application.Contracts;
using Nexus.Gateways.Wintech.Application.Models;
using Nexus.Operations.Aggregates;
using Nexus.Operations.Application.Contracts;
using Nexus.Payments.Errors;
using Xunit;

namespace Nexus.Tests.Charges;

public sealed class GatewayCredentialsResolverTests
{
    [Fact]
    public async Task ResolveCredentialsAsync_WhenOperationMissing_ReturnsFailure()
    {
        var sut = CreateSut(new EmptyOperationRepository(), new EmptyTeamRepository());

        var result = await sut.ResolveCredentialsAsync(new ResolveCredentialsRequest
        {
            OperationId = "missing",
            OperatorId = "operator-1",
        });

        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors, e => e.Code == PixPaymentErrorCodes.OperationNotFound);
    }

    [Fact]
    public async Task ResolveCredentialsAsync_WhenOperatorHasNoTeam_ReturnsTeamNotFound()
    {
        var operation = new Operation(
            "op-1",
            "N",
            "D",
            Array.Empty<string>(),
            Array.Empty<string>(),
            GatewaySelectionStrategy.Manual,
            new[] { "cred-op-1" },
            Array.Empty<string>(),
            DateTime.UtcNow,
            DateTime.UtcNow);

        var sut = CreateSut(
            new SingleOperationRepository(operation),
            new EmptyTeamRepository(),
            frendz: new SingleFrendzCredentialsRepository(
                new FrendzApiCredentials { Id = "cred-op-1", Name = "c", Token = "tok", StrawManId = "straw-1" }));

        var result = await sut.ResolveCredentialsAsync(new ResolveCredentialsRequest
        {
            OperationId = "op-1",
            OperatorId = "operator-without-team",
        });

        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors, e => e.Code == PixPaymentErrorCodes.TeamNotFound);
    }

    [Fact]
    public async Task ResolveCredentialsAsync_WithoutOperator_UsesOperationDefaultCredentials()
    {
        var operation = new Operation(
            "op-1",
            "N",
            "D",
            Array.Empty<string>(),
            Array.Empty<string>(),
            GatewaySelectionStrategy.Manual,
            new[] { "cred-op-1" },
            Array.Empty<string>(),
            DateTime.UtcNow,
            DateTime.UtcNow);

        var sut = CreateSut(
            new SingleOperationRepository(operation),
            new EmptyTeamRepository(),
            frendz: new SingleFrendzCredentialsRepository(
                new FrendzApiCredentials { Id = "cred-op-1", Name = "c", Token = "tok", StrawManId = "straw-1" }));

        var result = await sut.ResolveCredentialsAsync(new ResolveCredentialsRequest
        {
            OperationId = "op-1",
        });

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value!.Credentials);
        Assert.Equal("cred-op-1", result.Value.Credentials[0].CredentialId);
        Assert.Equal("straw-1", result.Value.StrawManIdByCredentialId["cred-op-1"]);
    }

    [Fact]
    public async Task ResolveCredentialsAsync_WhenPerGroupStrategy_ReturnsCredentialsFromAssignedGroups()
    {
        var operation = new Operation(
            "op-1",
            "N",
            "D",
            Array.Empty<string>(),
            Array.Empty<string>(),
            GatewaySelectionStrategy.PerStrawman,
            Array.Empty<string>(),
            Array.Empty<string>(),
            DateTime.UtcNow,
            DateTime.UtcNow);

        var team = new Team(
            "team-1",
            "op-1",
            "Team A",
            null,
            new[] { "operator-1" },
            Array.Empty<string>(),
            GatewaySelectionStrategy.PerGroup,
            Array.Empty<string>(),
            new[] { "grp-1" },
            Array.Empty<OperatorProfitShareRuleRecord>(),
            DateTime.UtcNow,
            DateTime.UtcNow);

        var group = new GatewayCredentialsGroup(
            "grp-1",
            "Group A",
            new[] { "cred-1" },
            DateTime.UtcNow,
            DateTime.UtcNow);

        var sut = CreateSut(
            new SingleOperationRepository(operation),
            new SingleTeamRepository(team),
            frendz: new SingleFrendzCredentialsRepository(
                new FrendzApiCredentials { Id = "cred-1", Name = "c", Token = "tok", StrawManId = "straw-1" }),
            groups: new SingleGatewayCredentialsGroupRepository(group));

        var result = await sut.ResolveCredentialsAsync(new ResolveCredentialsRequest
        {
            OperationId = "op-1",
            OperatorId = "operator-1",
        });

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value!.Credentials);
        Assert.Equal(PaymentGateway.Frendz, result.Value.Credentials[0].Gateway);
        Assert.Equal("cred-1", result.Value.Credentials[0].CredentialId);
    }

    [Fact]
    public async Task ResolveCredentialsAsync_WhenPerStrawmanWithEmptyStrawManIds_ReturnsNoGatewayServicesAvailable()
    {
        var operation = new Operation(
            "op-1",
            "N",
            "D",
            Array.Empty<string>(),
            Array.Empty<string>(),
            GatewaySelectionStrategy.PerStrawman,
            Array.Empty<string>(),
            Array.Empty<string>(),
            DateTime.UtcNow,
            DateTime.UtcNow);

        var sut = CreateSut(
            new SingleOperationRepository(operation),
            new EmptyTeamRepository(),
            frendz: new SingleFrendzCredentialsRepository(
                new FrendzApiCredentials { Id = "cred-1", Name = "c", Token = "tok", StrawManId = "straw-1" }));

        var result = await sut.ResolveCredentialsAsync(new ResolveCredentialsRequest
        {
            OperationId = "op-1",
        });

        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors, e => e.Code == PixPaymentErrorCodes.NoGatewayServicesAvailable);
    }

    [Fact]
    public async Task ResolveCredentialsAsync_WhenManualCredentialHasNoStrawManOwner_ReturnsNoGatewayServicesAvailable()
    {
        var operation = new Operation(
            "op-1",
            "N",
            "D",
            Array.Empty<string>(),
            Array.Empty<string>(),
            GatewaySelectionStrategy.Manual,
            new[] { "cred-1" },
            Array.Empty<string>(),
            DateTime.UtcNow,
            DateTime.UtcNow);

        var sut = CreateSut(
            new SingleOperationRepository(operation),
            new EmptyTeamRepository(),
            frendz: new SingleFrendzCredentialsRepository(
                new FrendzApiCredentials { Id = "cred-1", Name = "c", Token = "tok", StrawManId = null }));

        var result = await sut.ResolveCredentialsAsync(new ResolveCredentialsRequest
        {
            OperationId = "op-1",
        });

        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors, e => e.Code == PixPaymentErrorCodes.NoGatewayServicesAvailable);
    }

    [Fact]
    public async Task ResolveCredentialsAsync_WhenPerStrawman_OnlyUsesCredentialsFromLinkedStrawMen()
    {
        var operation = new Operation(
            "op-1",
            "N",
            "D",
            Array.Empty<string>(),
            new[] { "straw-1" },
            GatewaySelectionStrategy.PerStrawman,
            Array.Empty<string>(),
            Array.Empty<string>(),
            DateTime.UtcNow,
            DateTime.UtcNow);

        var sut = CreateSut(
            new SingleOperationRepository(operation),
            new EmptyTeamRepository(),
            frendz: new MultiFrendzCredentialsRepository(
                new FrendzApiCredentials { Id = "cred-linked", Name = "linked", Token = "tok", StrawManId = "straw-1" },
                new FrendzApiCredentials { Id = "cred-unlinked", Name = "unlinked", Token = "tok2", StrawManId = "straw-2" },
                new FrendzApiCredentials { Id = "cred-generic", Name = "generic", Token = "tok3", StrawManId = null }));

        var result = await sut.ResolveCredentialsAsync(new ResolveCredentialsRequest
        {
            OperationId = "op-1",
        });

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value!.Credentials);
        Assert.Equal("cred-linked", result.Value.Credentials[0].CredentialId);
    }

    [Fact]
    public async Task ResolveCredentialsAsync_WithOperator_UsesTeamCredentialScopeOverOperation()
    {
        var operation = new Operation(
            "op-1",
            "N",
            "D",
            Array.Empty<string>(),
            Array.Empty<string>(),
            GatewaySelectionStrategy.Manual,
            new[] { "cred-op" },
            Array.Empty<string>(),
            DateTime.UtcNow,
            DateTime.UtcNow);

        var team = new Team(
            "team-1",
            "op-1",
            "Team A",
            null,
            new[] { "operator-1" },
            Array.Empty<string>(),
            GatewaySelectionStrategy.Manual,
            new[] { "cred-team" },
            Array.Empty<string>(),
            Array.Empty<OperatorProfitShareRuleRecord>(),
            DateTime.UtcNow,
            DateTime.UtcNow);

        var sut = CreateSut(
            new SingleOperationRepository(operation),
            new SingleTeamRepository(team),
            frendz: new MultiFrendzCredentialsRepository(
                new FrendzApiCredentials { Id = "cred-op", Name = "op", Token = "tok-op", StrawManId = "straw-op" },
                new FrendzApiCredentials { Id = "cred-team", Name = "team", Token = "tok-team", StrawManId = "straw-team" }));

        var result = await sut.ResolveCredentialsAsync(new ResolveCredentialsRequest
        {
            OperationId = "op-1",
            OperatorId = "operator-1",
        });

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value!.Credentials);
        Assert.Equal("cred-team", result.Value.Credentials[0].CredentialId);
    }

    private static GatewayCredentialsResolver CreateSut(
        IOperationRepository operations,
        ITeamRepository teams,
        IFrendzApiCredentialsRepository? frendz = null,
        ISigiloPayApiCredentialsRepository? sigiloPay = null,
        IWintechApiCredentialsRepository? wintech = null,
        IGatewayCredentialsGroupRepository? groups = null)
    {
        return new GatewayCredentialsResolver(
            operations,
            teams,
            frendz ?? new EmptyFrendzCredentialsRepository(),
            sigiloPay ?? new EmptySigiloPayCredentialsRepository(),
            wintech ?? new EmptyWintechCredentialsRepository(),
            groups ?? new EmptyGatewayCredentialsGroupRepository());
    }

    private sealed class EmptyOperationRepository : IOperationRepository
    {
        public IAsyncQueryable<Operation> AsQueryable() =>
            new QueryableToAsyncQueryableAdapter<Operation>(Array.Empty<Operation>().AsQueryable());

        public Task<Operation> CreateAsync(Operation entity) => Task.FromResult(entity);
        async Task IRepository<Operation>.CreateAsync(Operation entity) { await CreateAsync(entity); }
        public Task CreateAsync(IEnumerable<Operation> entities) => Task.CompletedTask;
        public Task DeleteAsync(Operation entity) => Task.CompletedTask;
        public Task<long> DeleteAsync(Expression<Func<Operation, bool>> predicate) => Task.FromResult(0L);
        public Task UpdateAsync(Operation entity) => Task.CompletedTask;
        public Task<long> UpdateAsync(Expression expression) => Task.FromResult(0L);
    }

    private sealed class SingleOperationRepository(Operation operation) : IOperationRepository
    {
        public IAsyncQueryable<Operation> AsQueryable() =>
            new QueryableToAsyncQueryableAdapter<Operation>(new[] { operation }.AsQueryable());

        public Task<Operation> CreateAsync(Operation entity) => Task.FromResult(entity);
        async Task IRepository<Operation>.CreateAsync(Operation entity) { await CreateAsync(entity); }
        public Task CreateAsync(IEnumerable<Operation> entities) => Task.CompletedTask;
        public Task DeleteAsync(Operation entity) => Task.CompletedTask;
        public Task<long> DeleteAsync(Expression<Func<Operation, bool>> predicate) => Task.FromResult(0L);
        public Task UpdateAsync(Operation entity) => Task.CompletedTask;
        public Task<long> UpdateAsync(Expression expression) => Task.FromResult(0L);
    }

    private sealed class EmptyTeamRepository : ITeamRepository
    {
        public IAsyncQueryable<Team> AsQueryable() =>
            new QueryableToAsyncQueryableAdapter<Team>(Array.Empty<Team>().AsQueryable());

        public Task<Team> CreateAsync(Team entity) => Task.FromResult(entity);
        async Task IRepository<Team>.CreateAsync(Team entity) { await CreateAsync(entity); }
        public Task CreateAsync(IEnumerable<Team> entities) => Task.CompletedTask;
        public Task DeleteAsync(Team entity) => Task.CompletedTask;
        public Task<long> DeleteAsync(Expression<Func<Team, bool>> predicate) => Task.FromResult(0L);
        public Task UpdateAsync(Team entity) => Task.CompletedTask;
        public Task<long> UpdateAsync(Expression expression) => Task.FromResult(0L);
    }

    private sealed class SingleTeamRepository(Team team) : ITeamRepository
    {
        public IAsyncQueryable<Team> AsQueryable() =>
            new QueryableToAsyncQueryableAdapter<Team>(new[] { team }.AsQueryable());

        public Task<Team> CreateAsync(Team entity) => Task.FromResult(entity);
        async Task IRepository<Team>.CreateAsync(Team entity) { await CreateAsync(entity); }
        public Task CreateAsync(IEnumerable<Team> entities) => Task.CompletedTask;
        public Task DeleteAsync(Team entity) => Task.CompletedTask;
        public Task<long> DeleteAsync(Expression<Func<Team, bool>> predicate) => Task.FromResult(0L);
        public Task UpdateAsync(Team entity) => Task.CompletedTask;
        public Task<long> UpdateAsync(Expression expression) => Task.FromResult(0L);
    }

    private sealed class EmptyFrendzCredentialsRepository : IFrendzApiCredentialsRepository
    {
        public IAsyncQueryable<FrendzApiCredentials> AsQueryable() =>
            new QueryableToAsyncQueryableAdapter<FrendzApiCredentials>(Array.Empty<FrendzApiCredentials>().AsQueryable());

        public Task<FrendzApiCredentials> CreateAsync(FrendzApiCredentials entity) => throw new NotSupportedException();
        async Task IRepository<FrendzApiCredentials>.CreateAsync(FrendzApiCredentials entity) { await CreateAsync(entity); }
        public Task CreateAsync(IEnumerable<FrendzApiCredentials> entities) => throw new NotSupportedException();
        public Task DeleteAsync(FrendzApiCredentials entity) => throw new NotSupportedException();
        public Task<long> DeleteAsync(Expression<Func<FrendzApiCredentials, bool>> predicate) => throw new NotSupportedException();
        public Task UpdateAsync(FrendzApiCredentials entity) => throw new NotSupportedException();
        public Task<long> UpdateAsync(Expression expression) => throw new NotSupportedException();
    }

    private sealed class SingleFrendzCredentialsRepository(FrendzApiCredentials credential) : IFrendzApiCredentialsRepository
    {
        public IAsyncQueryable<FrendzApiCredentials> AsQueryable() =>
            new QueryableToAsyncQueryableAdapter<FrendzApiCredentials>(new[] { credential }.AsQueryable());

        public Task<FrendzApiCredentials> CreateAsync(FrendzApiCredentials entity) => throw new NotSupportedException();
        async Task IRepository<FrendzApiCredentials>.CreateAsync(FrendzApiCredentials entity) { await CreateAsync(entity); }
        public Task CreateAsync(IEnumerable<FrendzApiCredentials> entities) => throw new NotSupportedException();
        public Task DeleteAsync(FrendzApiCredentials entity) => throw new NotSupportedException();
        public Task<long> DeleteAsync(Expression<Func<FrendzApiCredentials, bool>> predicate) => throw new NotSupportedException();
        public Task UpdateAsync(FrendzApiCredentials entity) => throw new NotSupportedException();
        public Task<long> UpdateAsync(Expression expression) => throw new NotSupportedException();
    }

    private sealed class MultiFrendzCredentialsRepository(params FrendzApiCredentials[] credentials) : IFrendzApiCredentialsRepository
    {
        public IAsyncQueryable<FrendzApiCredentials> AsQueryable() =>
            new QueryableToAsyncQueryableAdapter<FrendzApiCredentials>(credentials.AsQueryable());

        public Task<FrendzApiCredentials> CreateAsync(FrendzApiCredentials entity) => throw new NotSupportedException();
        async Task IRepository<FrendzApiCredentials>.CreateAsync(FrendzApiCredentials entity) { await CreateAsync(entity); }
        public Task CreateAsync(IEnumerable<FrendzApiCredentials> entities) => throw new NotSupportedException();
        public Task DeleteAsync(FrendzApiCredentials entity) => throw new NotSupportedException();
        public Task<long> DeleteAsync(Expression<Func<FrendzApiCredentials, bool>> predicate) => throw new NotSupportedException();
        public Task UpdateAsync(FrendzApiCredentials entity) => throw new NotSupportedException();
        public Task<long> UpdateAsync(Expression expression) => throw new NotSupportedException();
    }

    private sealed class EmptySigiloPayCredentialsRepository : ISigiloPayApiCredentialsRepository
    {
        public IAsyncQueryable<SigiloPayApiCredentials> AsQueryable() =>
            new QueryableToAsyncQueryableAdapter<SigiloPayApiCredentials>(Array.Empty<SigiloPayApiCredentials>().AsQueryable());

        public Task<SigiloPayApiCredentials> CreateAsync(SigiloPayApiCredentials entity) => throw new NotSupportedException();
        async Task IRepository<SigiloPayApiCredentials>.CreateAsync(SigiloPayApiCredentials entity) { await CreateAsync(entity); }
        public Task CreateAsync(IEnumerable<SigiloPayApiCredentials> entities) => throw new NotSupportedException();
        public Task DeleteAsync(SigiloPayApiCredentials entity) => throw new NotSupportedException();
        public Task<long> DeleteAsync(Expression<Func<SigiloPayApiCredentials, bool>> predicate) => throw new NotSupportedException();
        public Task UpdateAsync(SigiloPayApiCredentials entity) => throw new NotSupportedException();
        public Task<long> UpdateAsync(Expression expression) => throw new NotSupportedException();
    }

    private sealed class EmptyWintechCredentialsRepository : IWintechApiCredentialsRepository
    {
        public IAsyncQueryable<WintechApiCredentials> AsQueryable() =>
            new QueryableToAsyncQueryableAdapter<WintechApiCredentials>(Array.Empty<WintechApiCredentials>().AsQueryable());

        public Task<WintechApiCredentials> CreateAsync(WintechApiCredentials entity) => throw new NotSupportedException();
        async Task IRepository<WintechApiCredentials>.CreateAsync(WintechApiCredentials entity) { await CreateAsync(entity); }
        public Task CreateAsync(IEnumerable<WintechApiCredentials> entities) => throw new NotSupportedException();
        public Task DeleteAsync(WintechApiCredentials entity) => throw new NotSupportedException();
        public Task<long> DeleteAsync(Expression<Func<WintechApiCredentials, bool>> predicate) => throw new NotSupportedException();
        public Task UpdateAsync(WintechApiCredentials entity) => throw new NotSupportedException();
        public Task<long> UpdateAsync(Expression expression) => throw new NotSupportedException();
    }

    private sealed class EmptyGatewayCredentialsGroupRepository : IGatewayCredentialsGroupRepository
    {
        public IAsyncQueryable<GatewayCredentialsGroup> AsQueryable() =>
            new QueryableToAsyncQueryableAdapter<GatewayCredentialsGroup>(Array.Empty<GatewayCredentialsGroup>().AsQueryable());

        public Task<GatewayCredentialsGroup> CreateAsync(GatewayCredentialsGroup entity) => throw new NotSupportedException();
        async Task IRepository<GatewayCredentialsGroup>.CreateAsync(GatewayCredentialsGroup entity) { await CreateAsync(entity); }
        public Task CreateAsync(IEnumerable<GatewayCredentialsGroup> entities) => throw new NotSupportedException();
        public Task DeleteAsync(GatewayCredentialsGroup entity) => throw new NotSupportedException();
        public Task<long> DeleteAsync(Expression<Func<GatewayCredentialsGroup, bool>> predicate) => throw new NotSupportedException();
        public Task UpdateAsync(GatewayCredentialsGroup entity) => throw new NotSupportedException();
        public Task<long> UpdateAsync(Expression expression) => throw new NotSupportedException();
    }

    private sealed class SingleGatewayCredentialsGroupRepository(GatewayCredentialsGroup group) : IGatewayCredentialsGroupRepository
    {
        public IAsyncQueryable<GatewayCredentialsGroup> AsQueryable() =>
            new QueryableToAsyncQueryableAdapter<GatewayCredentialsGroup>(new[] { group }.AsQueryable());

        public Task<GatewayCredentialsGroup> CreateAsync(GatewayCredentialsGroup entity) => throw new NotSupportedException();
        async Task IRepository<GatewayCredentialsGroup>.CreateAsync(GatewayCredentialsGroup entity) { await CreateAsync(entity); }
        public Task CreateAsync(IEnumerable<GatewayCredentialsGroup> entities) => throw new NotSupportedException();
        public Task DeleteAsync(GatewayCredentialsGroup entity) => throw new NotSupportedException();
        public Task<long> DeleteAsync(Expression<Func<GatewayCredentialsGroup, bool>> predicate) => throw new NotSupportedException();
        public Task UpdateAsync(GatewayCredentialsGroup entity) => throw new NotSupportedException();
        public Task<long> UpdateAsync(Expression expression) => throw new NotSupportedException();
    }
}

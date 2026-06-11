using System.Linq.Expressions;
using Aidan.Core.Linq;
using Aidan.Mongo.Linq;
using Nexus.Gateways.Application;
using Nexus.Gateways.Entities;
using Nexus.Gateways.ErrorCodes;
using Nexus.Gateways.Infrastructure;
using Nexus.Tests.Support;
using Xunit;

namespace Nexus.Tests.Gateways;

public sealed class GatewayCredentialsGroupServiceTests
{
    private sealed class InMemoryGatewayCredentialsGroupRepository : IGatewayCredentialsGroupRepository
    {
        private readonly List<GatewayCredentialsGroup> _store = new();

        public IAsyncQueryable<GatewayCredentialsGroup> AsQueryable()
            => new MongoAsyncQueryable<GatewayCredentialsGroup>(_store.AsQueryable());

        public Task CreateAsync(GatewayCredentialsGroup entity)
        {
            _store.Add(entity);
            return Task.CompletedTask;
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

    private static GatewayCredentialsGroupService CreateSut(
        InMemoryGatewayCredentialsGroupRepository? repo = null,
        FakeGatewayCredentialsIdValidator? validator = null)
    {
        return new GatewayCredentialsGroupService(
            repo ?? new InMemoryGatewayCredentialsGroupRepository(),
            validator ?? new FakeGatewayCredentialsIdValidator());
    }

    [Fact]
    public async Task CreateGroupAsync_ValidName_ReturnsGroupDetails()
    {
        var sut = CreateSut();

        var result = await sut.CreateGroupAsync("Payments Group");

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal("Payments Group", result.Value!.Name);
    }

    [Fact]
    public async Task CreateGroupAsync_EmptyName_ReturnsNameInvalid()
    {
        var sut = CreateSut();

        var result = await sut.CreateGroupAsync("   ");

        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors, e => e.Code == GatewayCredentialsGroupErrorCodes.NameInvalid);
    }

    [Fact]
    public async Task CreateGroupAsync_NameTooLong_ReturnsNameTooLong()
    {
        var sut = CreateSut();
        var tooLongName = new string('G', GatewayCredentialsGroup.MaxNameLength + 1);

        var result = await sut.CreateGroupAsync(tooLongName);

        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors, e => e.Code == GatewayCredentialsGroupErrorCodes.NameTooLong);
    }

    [Fact]
    public async Task CreateGroupAsync_NameAlreadyExists_IgnoresCaseAndSpaces()
    {
        var repo = new InMemoryGatewayCredentialsGroupRepository();
        var sut = CreateSut(repo);

        var first = await sut.CreateGroupAsync("Primary");
        Assert.True(first.IsSuccess);

        var duplicate = await sut.CreateGroupAsync("  primary  ");

        Assert.True(duplicate.IsFailure);
        Assert.Contains(duplicate.Errors, e => e.Code == GatewayCredentialsGroupErrorCodes.NameAlreadyExists);
    }

    [Fact]
    public async Task AssignGatewayCredentialsAsync_ValidIds_ReturnsSuccess()
    {
        var repo = new InMemoryGatewayCredentialsGroupRepository();
        var validator = new FakeGatewayCredentialsIdValidator(["cred-1"]);
        var sut = CreateSut(repo, validator);
        var created = await sut.CreateGroupAsync("Group A");
        Assert.True(created.IsSuccess);

        var result = await sut.AssignGatewayCredentialsAsync(created.Value!.Id, "cred-1");

        Assert.True(result.IsSuccess);
        var group = repo.AsQueryable().First();
        Assert.Contains("cred-1", group.GatewayCredentialsIds);
    }

    [Fact]
    public async Task AssignGatewayCredentialsAsync_InvalidGroupId_ReturnsGroupIdInvalid()
    {
        var validator = new FakeGatewayCredentialsIdValidator(["cred-1"]);
        var sut = CreateSut(validator: validator);

        var result = await sut.AssignGatewayCredentialsAsync("  ", "cred-1");

        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors, e => e.Code == GatewayCredentialsGroupErrorCodes.GroupIdInvalid);
    }

    [Fact]
    public async Task AssignGatewayCredentialsAsync_GroupNotFound_ReturnsGroupNotFound()
    {
        var validator = new FakeGatewayCredentialsIdValidator(["cred-1"]);
        var sut = CreateSut(validator: validator);

        var result = await sut.AssignGatewayCredentialsAsync("missing-group", "cred-1");

        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors, e => e.Code == GatewayCredentialsGroupErrorCodes.GroupNotFound);
    }

    [Fact]
    public async Task AssignGatewayCredentialsAsync_InvalidCredentialId_ReturnsGatewayCredentialInvalid()
    {
        var repo = new InMemoryGatewayCredentialsGroupRepository();
        var sut = CreateSut(repo);
        var created = await sut.CreateGroupAsync("Group A");
        Assert.True(created.IsSuccess);

        var result = await sut.AssignGatewayCredentialsAsync(created.Value!.Id, "  ");

        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors, e => e.Code == GatewayCredentialsGroupErrorCodes.GatewayCredentialInvalid);
    }

    [Fact]
    public async Task AssignGatewayCredentialsAsync_CredentialNotFound_ReturnsGatewayCredentialNotFound()
    {
        var repo = new InMemoryGatewayCredentialsGroupRepository();
        var sut = CreateSut(repo);
        var created = await sut.CreateGroupAsync("Group A");
        Assert.True(created.IsSuccess);

        var result = await sut.AssignGatewayCredentialsAsync(created.Value!.Id, "missing-cred");

        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors, e => e.Code == GatewayCredentialsGroupErrorCodes.GatewayCredentialNotFound);
    }

    [Fact]
    public async Task AssignGatewayCredentialsAsync_Duplicate_ReturnsGatewayCredentialAlreadyAssigned()
    {
        var repo = new InMemoryGatewayCredentialsGroupRepository();
        var validator = new FakeGatewayCredentialsIdValidator(["cred-1"]);
        var sut = CreateSut(repo, validator);
        var created = await sut.CreateGroupAsync("Group A");
        Assert.True(created.IsSuccess);

        var first = await sut.AssignGatewayCredentialsAsync(created.Value!.Id, "cred-1");
        Assert.True(first.IsSuccess);

        var duplicate = await sut.AssignGatewayCredentialsAsync(created.Value.Id, "cred-1");

        Assert.True(duplicate.IsFailure);
        Assert.Contains(duplicate.Errors, e => e.Code == GatewayCredentialsGroupErrorCodes.GatewayCredentialAlreadyAssigned);
    }

    [Fact]
    public async Task UnassignGatewayCredentialsAsync_AssignedCredential_ReturnsSuccess()
    {
        var repo = new InMemoryGatewayCredentialsGroupRepository();
        var validator = new FakeGatewayCredentialsIdValidator(["cred-1"]);
        var sut = CreateSut(repo, validator);
        var created = await sut.CreateGroupAsync("Group A");
        Assert.True(created.IsSuccess);
        await sut.AssignGatewayCredentialsAsync(created.Value!.Id, "cred-1");

        var result = await sut.UnassignGatewayCredentialsAsync(created.Value.Id, "cred-1");

        Assert.True(result.IsSuccess);
        var group = repo.AsQueryable().First();
        Assert.DoesNotContain("cred-1", group.GatewayCredentialsIds);
    }

    [Fact]
    public async Task UnassignGatewayCredentialsAsync_InvalidGroupId_ReturnsGroupIdInvalid()
    {
        var sut = CreateSut();

        var result = await sut.UnassignGatewayCredentialsAsync("", "cred-1");

        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors, e => e.Code == GatewayCredentialsGroupErrorCodes.GroupIdInvalid);
    }

    [Fact]
    public async Task UnassignGatewayCredentialsAsync_GroupNotFound_ReturnsGroupNotFound()
    {
        var sut = CreateSut();

        var result = await sut.UnassignGatewayCredentialsAsync("missing-group", "cred-1");

        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors, e => e.Code == GatewayCredentialsGroupErrorCodes.GroupNotFound);
    }

    [Fact]
    public async Task UnassignGatewayCredentialsAsync_InvalidCredentialId_ReturnsGatewayCredentialInvalid()
    {
        var repo = new InMemoryGatewayCredentialsGroupRepository();
        var sut = CreateSut(repo);
        var created = await sut.CreateGroupAsync("Group A");
        Assert.True(created.IsSuccess);

        var result = await sut.UnassignGatewayCredentialsAsync(created.Value!.Id, "");

        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors, e => e.Code == GatewayCredentialsGroupErrorCodes.GatewayCredentialInvalid);
    }

    [Fact]
    public async Task UnassignGatewayCredentialsAsync_CredentialNotAssigned_ReturnsGatewayCredentialNotAssigned()
    {
        var repo = new InMemoryGatewayCredentialsGroupRepository();
        var sut = CreateSut(repo);
        var created = await sut.CreateGroupAsync("Group A");
        Assert.True(created.IsSuccess);

        var result = await sut.UnassignGatewayCredentialsAsync(created.Value!.Id, "cred-1");

        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors, e => e.Code == GatewayCredentialsGroupErrorCodes.GatewayCredentialNotAssigned);
    }

    [Fact]
    public async Task DeleteGroupAsync_ExistingGroup_ReturnsSuccess()
    {
        var repo = new InMemoryGatewayCredentialsGroupRepository();
        var sut = CreateSut(repo);
        var created = await sut.CreateGroupAsync("Group A");
        Assert.True(created.IsSuccess);

        var result = await sut.DeleteGroupAsync(created.Value!.Id);

        Assert.True(result.IsSuccess);
        Assert.Empty(repo.AsQueryable().ToList());
    }

    [Fact]
    public async Task DeleteGroupAsync_InvalidGroupId_ReturnsGroupIdInvalid()
    {
        var sut = CreateSut();

        var result = await sut.DeleteGroupAsync("  ");

        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors, e => e.Code == GatewayCredentialsGroupErrorCodes.GroupIdInvalid);
    }

    [Fact]
    public async Task DeleteGroupAsync_GroupNotFound_ReturnsGroupNotFound()
    {
        var sut = CreateSut();

        var result = await sut.DeleteGroupAsync("missing-group");

        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors, e => e.Code == GatewayCredentialsGroupErrorCodes.GroupNotFound);
    }
}

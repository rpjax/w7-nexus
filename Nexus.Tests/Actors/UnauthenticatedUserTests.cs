using Nexus.Accounts.ErrorCodes;
using Nexus.Actors.Requests;
using Nexus.Authorization;
using Nexus.Tests.Accounts;
using Nexus.Tests.Support;
using Xunit;

namespace Nexus.Tests.Actors;

public sealed class UnauthenticatedUserTests
{
    [Fact]
    public async Task CreateAdministratorAccountAsync_ValidRequest_CreatesAccountWithAdministratorRole()
    {
        var accounts = new InMemoryAccountRepository();
        var sut = new ActorTestContext().CreateUnauthenticatedUser(accounts);

        var result = await sut.CreateAdministratorAccountAsync(new CreateAdministratorAccountRequest
        {
            Username = "newadmin",
            Password = "password123"
        });

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);

        var persisted = accounts.AsQueryable().Single();
        Assert.Equal("newadmin", persisted.Username);
        Assert.Contains(Roles.Administrator, persisted.Roles);
        Assert.Empty(persisted.Permissions);
    }

    [Fact]
    public async Task CreateAdministratorAccountAsync_InvalidUsername_PropagatesError()
    {
        var accounts = new InMemoryAccountRepository();
        var sut = new ActorTestContext().CreateUnauthenticatedUser(accounts);

        var result = await sut.CreateAdministratorAccountAsync(new CreateAdministratorAccountRequest
        {
            Username = "ab",
            Password = "password123"
        });

        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors, e =>
            e.Code == AccountErrorCodes.UsernameInvalidFormat ||
            e.Code == AccountErrorCodes.UsernameEmpty);
        Assert.Empty(accounts.AsQueryable().ToArray());
    }

    [Fact]
    public async Task CreateOperatorAccountAsync_ValidRequest_CreatesAccountWithOperatorRole()
    {
        var accounts = new InMemoryAccountRepository();
        var sut = new ActorTestContext().CreateUnauthenticatedUser(accounts);

        var result = await sut.CreateOperatorAccountAsync(new CreateOperatorAccountRequest
        {
            Username = "newoperator",
            Password = "password123"
        });

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);

        var persisted = accounts.AsQueryable().Single();
        Assert.Equal("newoperator", persisted.Username);
        Assert.Contains(Roles.Operator, persisted.Roles);
        Assert.Empty(persisted.Permissions);
    }

    [Fact]
    public async Task CreateOperatorAccountAsync_ShortPassword_PropagatesError()
    {
        var accounts = new InMemoryAccountRepository();
        var sut = new ActorTestContext().CreateUnauthenticatedUser(accounts);

        var result = await sut.CreateOperatorAccountAsync(new CreateOperatorAccountRequest
        {
            Username = "validoperator",
            Password = "short"
        });

        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors, e => e.Code == AccountErrorCodes.PasswordTooShort);
        Assert.Empty(accounts.AsQueryable().ToArray());
    }
}

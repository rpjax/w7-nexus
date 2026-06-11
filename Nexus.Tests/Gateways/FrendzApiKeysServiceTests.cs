using Nexus.Database.Models;
using Nexus.Gateways.Frendz.Application;
using Nexus.Gateways.Frendz.Application.Models;
using Nexus.Gateways.Frendz.ErrorCodes;
using Xunit;
using static Nexus.Tests.Gateways.ApiKeysServiceTestSupport;

namespace Nexus.Tests.Gateways;

public sealed class FrendzApiKeysServiceTests
{
    private static FrendzApiKeysService CreateSut(AsyncInMemoryAccountRepository? accounts = null) =>
        new(
            CreateThrowIfCalledMongoCollection<FrendzApiCredentialsRecord>(),
            accounts ?? new AsyncInMemoryAccountRepository());

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task AddCredentialsAsync_EmptyToken_ReturnsTokenRequired(string? token)
    {
        var sut = CreateSut();

        var result = await sut.AddCredentialsAsync(new AddCredentialsRequest
        {
            Name = "Key",
            Token = token!,
        });

        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors, e => e.Code == FrendzErrorCodes.TokenRequired);
    }

    [Fact]
    public async Task AddCredentialsAsync_NameTooLong_ReturnsNameTooLong()
    {
        var sut = CreateSut();

        var result = await sut.AddCredentialsAsync(new AddCredentialsRequest
        {
            Name = Repeat('n', 201),
            Token = "valid-token",
        });

        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors, e => e.Code == FrendzErrorCodes.NameTooLong);
    }

    [Fact]
    public async Task AddCredentialsAsync_TokenTooLong_ReturnsTokenTooLong()
    {
        var sut = CreateSut();

        var result = await sut.AddCredentialsAsync(new AddCredentialsRequest
        {
            Name = "Key",
            Token = Repeat('t', 8193),
        });

        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors, e => e.Code == FrendzErrorCodes.TokenTooLong);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task AddCredentialsAsync_EmptyStrawManId_ReturnsStrawManIdInvalid(string strawManId)
    {
        var sut = CreateSut();

        var result = await sut.AddCredentialsAsync(new AddCredentialsRequest
        {
            Name = "Key",
            Token = "valid-token",
            StrawManId = strawManId,
        });

        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors, e => e.Code == FrendzErrorCodes.StrawManIdInvalid);
    }

    [Fact]
    public async Task AddCredentialsAsync_StrawManIdTooLong_ReturnsStrawManIdTooLong()
    {
        var sut = CreateSut();

        var result = await sut.AddCredentialsAsync(new AddCredentialsRequest
        {
            Name = "Key",
            Token = "valid-token",
            StrawManId = Repeat('s', 257),
        });

        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors, e => e.Code == FrendzErrorCodes.StrawManIdTooLong);
    }

    [Fact]
    public async Task AddCredentialsAsync_StrawManAccountNotFound_ReturnsStrawManAccountNotFound()
    {
        var sut = CreateSut();

        var result = await sut.AddCredentialsAsync(new AddCredentialsRequest
        {
            Name = "Key",
            Token = "valid-token",
            StrawManId = "missing-sm",
        });

        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors, e => e.Code == FrendzErrorCodes.StrawManAccountNotFound);
    }

    [Fact]
    public async Task UpdateCredentialsAsync_InvalidCredentialId_ReturnsCredentialIdInvalid()
    {
        var sut = CreateSut();

        var result = await sut.UpdateCredentialsAsync(new UpdateCredentialsRequest
        {
            Id = "not-an-object-id",
            Name = "Key",
            Token = "valid-token",
        });

        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors, e => e.Code == FrendzErrorCodes.CredentialIdInvalid);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task UpdateCredentialsAsync_EmptyToken_ReturnsTokenRequired(string? token)
    {
        var sut = CreateSut();

        var result = await sut.UpdateCredentialsAsync(new UpdateCredentialsRequest
        {
            Id = MongoDB.Bson.ObjectId.GenerateNewId().ToString(),
            Name = "Key",
            Token = token!,
        });

        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors, e => e.Code == FrendzErrorCodes.TokenRequired);
    }

    [Fact]
    public async Task UpdateCredentialsAsync_StrawManAccountNotFound_ReturnsStrawManAccountNotFound()
    {
        var sut = CreateSut();

        var result = await sut.UpdateCredentialsAsync(new UpdateCredentialsRequest
        {
            Id = MongoDB.Bson.ObjectId.GenerateNewId().ToString(),
            Name = "Key",
            Token = "valid-token",
            StrawManId = "missing-sm",
        });

        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors, e => e.Code == FrendzErrorCodes.StrawManAccountNotFound);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("invalid")]
    public async Task SetCredentialEnabledAsync_InvalidId_ReturnsCredentialIdInvalid(string? id)
    {
        var sut = CreateSut();

        var result = await sut.SetCredentialEnabledAsync(new SetFrendzCredentialEnabledRequest
        {
            Id = id!,
            Enabled = true,
        });

        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors, e => e.Code == FrendzErrorCodes.CredentialIdInvalid);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("invalid")]
    public async Task DeleteCredentialsAsync_InvalidId_ReturnsCredentialIdInvalid(string? id)
    {
        var sut = CreateSut();

        var result = await sut.DeleteCredentialsAsync(id!);

        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors, e => e.Code == FrendzErrorCodes.CredentialIdInvalid);
    }

}

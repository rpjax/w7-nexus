using Nexus.Database.Models;
using Nexus.Gateways.SigiloPay.Application;
using Nexus.Gateways.SigiloPay.Application.Models;
using Nexus.Gateways.SigiloPay.ErrorCodes;
using Xunit;
using static Nexus.Tests.Gateways.ApiKeysServiceTestSupport;

namespace Nexus.Tests.Gateways;

public sealed class SigiloPayApiKeysServiceTests
{
    private static SigiloPayApiKeysService CreateSut(AsyncInMemoryAccountRepository? accounts = null) =>
        new(
            CreateThrowIfCalledMongoCollection<SigiloPayApiCredentialsRecord>(),
            accounts ?? new AsyncInMemoryAccountRepository());

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task AddCredentialsAsync_EmptyPublicKey_ReturnsPublicKeyRequired(string? publicKey)
    {
        var sut = CreateSut();

        var result = await sut.AddCredentialsAsync(new AddSigiloPayCredentialsRequest
        {
            Name = "Key",
            PublicKey = publicKey!,
            SecretKey = "secret",
        });

        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors, e => e.Code == SigiloPayErrorCodes.PublicKeyRequired);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task AddCredentialsAsync_EmptySecretKey_ReturnsSecretKeyRequired(string? secretKey)
    {
        var sut = CreateSut();

        var result = await sut.AddCredentialsAsync(new AddSigiloPayCredentialsRequest
        {
            Name = "Key",
            PublicKey = "public",
            SecretKey = secretKey!,
        });

        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors, e => e.Code == SigiloPayErrorCodes.SecretKeyRequired);
    }

    [Fact]
    public async Task AddCredentialsAsync_NameTooLong_ReturnsNameTooLong()
    {
        var sut = CreateSut();

        var result = await sut.AddCredentialsAsync(new AddSigiloPayCredentialsRequest
        {
            Name = Repeat('n', 201),
            PublicKey = "public",
            SecretKey = "secret",
        });

        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors, e => e.Code == SigiloPayErrorCodes.NameTooLong);
    }

    [Fact]
    public async Task AddCredentialsAsync_PublicKeyTooLong_ReturnsPublicKeyTooLong()
    {
        var sut = CreateSut();

        var result = await sut.AddCredentialsAsync(new AddSigiloPayCredentialsRequest
        {
            Name = "Key",
            PublicKey = Repeat('p', 8193),
            SecretKey = "secret",
        });

        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors, e => e.Code == SigiloPayErrorCodes.PublicKeyTooLong);
    }

    [Fact]
    public async Task AddCredentialsAsync_SecretKeyTooLong_ReturnsSecretKeyTooLong()
    {
        var sut = CreateSut();

        var result = await sut.AddCredentialsAsync(new AddSigiloPayCredentialsRequest
        {
            Name = "Key",
            PublicKey = "public",
            SecretKey = Repeat('s', 8193),
        });

        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors, e => e.Code == SigiloPayErrorCodes.SecretKeyTooLong);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task AddCredentialsAsync_EmptyStrawManId_ReturnsStrawManIdInvalid(string strawManId)
    {
        var sut = CreateSut();

        var result = await sut.AddCredentialsAsync(new AddSigiloPayCredentialsRequest
        {
            Name = "Key",
            PublicKey = "public",
            SecretKey = "secret",
            StrawManId = strawManId,
        });

        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors, e => e.Code == SigiloPayErrorCodes.StrawManIdInvalid);
    }

    [Fact]
    public async Task AddCredentialsAsync_StrawManIdTooLong_ReturnsStrawManIdTooLong()
    {
        var sut = CreateSut();

        var result = await sut.AddCredentialsAsync(new AddSigiloPayCredentialsRequest
        {
            Name = "Key",
            PublicKey = "public",
            SecretKey = "secret",
            StrawManId = Repeat('s', 257),
        });

        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors, e => e.Code == SigiloPayErrorCodes.StrawManIdTooLong);
    }

    [Fact]
    public async Task AddCredentialsAsync_StrawManAccountNotFound_ReturnsStrawManAccountNotFound()
    {
        var sut = CreateSut();

        var result = await sut.AddCredentialsAsync(new AddSigiloPayCredentialsRequest
        {
            Name = "Key",
            PublicKey = "public",
            SecretKey = "secret",
            StrawManId = "missing-sm",
        });

        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors, e => e.Code == SigiloPayErrorCodes.StrawManAccountNotFound);
    }

    [Fact]
    public async Task UpdateCredentialsAsync_InvalidCredentialId_ReturnsCredentialIdInvalid()
    {
        var sut = CreateSut();

        var result = await sut.UpdateCredentialsAsync(new UpdateSigiloPayCredentialsRequest
        {
            Id = "not-an-object-id",
            Name = "Key",
            PublicKey = "public",
            SecretKey = "secret",
        });

        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors, e => e.Code == SigiloPayErrorCodes.CredentialIdInvalid);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task UpdateCredentialsAsync_EmptyPublicKey_ReturnsPublicKeyRequired(string? publicKey)
    {
        var sut = CreateSut();

        var result = await sut.UpdateCredentialsAsync(new UpdateSigiloPayCredentialsRequest
        {
            Id = MongoDB.Bson.ObjectId.GenerateNewId().ToString(),
            Name = "Key",
            PublicKey = publicKey!,
            SecretKey = "secret",
        });

        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors, e => e.Code == SigiloPayErrorCodes.PublicKeyRequired);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task UpdateCredentialsAsync_EmptySecretKey_ReturnsSecretKeyRequired(string? secretKey)
    {
        var sut = CreateSut();

        var result = await sut.UpdateCredentialsAsync(new UpdateSigiloPayCredentialsRequest
        {
            Id = MongoDB.Bson.ObjectId.GenerateNewId().ToString(),
            Name = "Key",
            PublicKey = "public",
            SecretKey = secretKey!,
        });

        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors, e => e.Code == SigiloPayErrorCodes.SecretKeyRequired);
    }

    [Fact]
    public async Task UpdateCredentialsAsync_StrawManAccountNotFound_ReturnsStrawManAccountNotFound()
    {
        var sut = CreateSut();

        var result = await sut.UpdateCredentialsAsync(new UpdateSigiloPayCredentialsRequest
        {
            Id = MongoDB.Bson.ObjectId.GenerateNewId().ToString(),
            Name = "Key",
            PublicKey = "public",
            SecretKey = "secret",
            StrawManId = "missing-sm",
        });

        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors, e => e.Code == SigiloPayErrorCodes.StrawManAccountNotFound);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("invalid")]
    public async Task SetCredentialEnabledAsync_InvalidId_ReturnsCredentialIdInvalid(string? id)
    {
        var sut = CreateSut();

        var result = await sut.SetCredentialEnabledAsync(new SetSigiloPayCredentialEnabledRequest
        {
            Id = id!,
            Enabled = true,
        });

        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors, e => e.Code == SigiloPayErrorCodes.CredentialIdInvalid);
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
        Assert.Contains(result.Errors, e => e.Code == SigiloPayErrorCodes.CredentialIdInvalid);
    }
}

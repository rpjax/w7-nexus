using System.Linq.Expressions;
using Nexus.Gateways.Wintech.Application.Contracts;
using Nexus.Gateways.SigiloPay.Application.Contracts;
using Nexus.Gateways.Frendz.Application.Contracts;
using Aidan.Core.Linq;
using Aidan.Core.Patterns;
using Aidan.Mongo.Linq;
using Nexus.Gateways.Application.Services;
using Nexus.Gateways.Frendz.Application.Services;
using Nexus.Gateways.Frendz.Application.Models;
using Nexus.Gateways.SigiloPay.Application.Services;
using Nexus.Gateways.SigiloPay.Application.Models;
using Nexus.Gateways.Wintech.Application.Services;
using Nexus.Gateways.Wintech.Application.Models;
using Xunit;

namespace Nexus.Tests.Gateways;

public sealed class GatewayCredentialsIdValidatorTests
{
    private sealed class InMemoryFrendzCredentialsRepository : IFrendzApiCredentialsRepository
    {
        private readonly FrendzApiCredentials[] _credentials;

        public InMemoryFrendzCredentialsRepository(params FrendzApiCredentials[] credentials) =>
            _credentials = credentials;

        public IAsyncQueryable<FrendzApiCredentials> AsQueryable()
            => new MongoAsyncQueryable<FrendzApiCredentials>(_credentials.AsQueryable());

        public Task<FrendzApiCredentials> CreateAsync(FrendzApiCredentials entity) => throw new NotSupportedException();
        async Task IRepository<FrendzApiCredentials>.CreateAsync(FrendzApiCredentials entity) { await CreateAsync(entity); }
        public Task CreateAsync(IEnumerable<FrendzApiCredentials> entities) => throw new NotSupportedException();
        public Task DeleteAsync(FrendzApiCredentials entity) => throw new NotSupportedException();
        public Task<long> DeleteAsync(Expression<Func<FrendzApiCredentials, bool>> predicate) => throw new NotSupportedException();
        public Task UpdateAsync(FrendzApiCredentials entity) => throw new NotSupportedException();
        public Task<long> UpdateAsync(Expression expression) => throw new NotSupportedException();
    }

    private sealed class InMemorySigiloPayCredentialsRepository : ISigiloPayApiCredentialsRepository
    {
        private readonly SigiloPayApiCredentials[] _credentials;

        public InMemorySigiloPayCredentialsRepository(params SigiloPayApiCredentials[] credentials) =>
            _credentials = credentials;

        public IAsyncQueryable<SigiloPayApiCredentials> AsQueryable()
            => new MongoAsyncQueryable<SigiloPayApiCredentials>(_credentials.AsQueryable());

        public Task<SigiloPayApiCredentials> CreateAsync(SigiloPayApiCredentials entity) => throw new NotSupportedException();
        async Task IRepository<SigiloPayApiCredentials>.CreateAsync(SigiloPayApiCredentials entity) { await CreateAsync(entity); }
        public Task CreateAsync(IEnumerable<SigiloPayApiCredentials> entities) => throw new NotSupportedException();
        public Task DeleteAsync(SigiloPayApiCredentials entity) => throw new NotSupportedException();
        public Task<long> DeleteAsync(Expression<Func<SigiloPayApiCredentials, bool>> predicate) => throw new NotSupportedException();
        public Task UpdateAsync(SigiloPayApiCredentials entity) => throw new NotSupportedException();
        public Task<long> UpdateAsync(Expression expression) => throw new NotSupportedException();
    }

    private sealed class InMemoryWintechCredentialsRepository : IWintechApiCredentialsRepository
    {
        private readonly WintechApiCredentials[] _credentials;

        public InMemoryWintechCredentialsRepository(params WintechApiCredentials[] credentials) =>
            _credentials = credentials;

        public IAsyncQueryable<WintechApiCredentials> AsQueryable()
            => new MongoAsyncQueryable<WintechApiCredentials>(_credentials.AsQueryable());

        public Task<WintechApiCredentials> CreateAsync(WintechApiCredentials entity) => throw new NotSupportedException();
        async Task IRepository<WintechApiCredentials>.CreateAsync(WintechApiCredentials entity) { await CreateAsync(entity); }
        public Task CreateAsync(IEnumerable<WintechApiCredentials> entities) => throw new NotSupportedException();
        public Task DeleteAsync(WintechApiCredentials entity) => throw new NotSupportedException();
        public Task<long> DeleteAsync(Expression<Func<WintechApiCredentials, bool>> predicate) => throw new NotSupportedException();
        public Task UpdateAsync(WintechApiCredentials entity) => throw new NotSupportedException();
        public Task<long> UpdateAsync(Expression expression) => throw new NotSupportedException();
    }

    private static GatewayCredentialsIdValidator CreateSut(
        InMemoryFrendzCredentialsRepository? frendz = null,
        InMemorySigiloPayCredentialsRepository? sigiloPay = null,
        InMemoryWintechCredentialsRepository? wintech = null) =>
        new(
            frendz ?? new InMemoryFrendzCredentialsRepository(),
            sigiloPay ?? new InMemorySigiloPayCredentialsRepository(),
            wintech ?? new InMemoryWintechCredentialsRepository());

    [Fact]
    public async Task ExistsAsync_UnknownId_ReturnsFalse()
    {
        var sut = CreateSut();

        var exists = await sut.ExistsAsync("missing-id");

        Assert.False(exists);
    }

    [Fact]
    public async Task ExistsAsync_FrendzCredential_ReturnsTrue()
    {
        var frendzId = "frendz-cred-1";
        var sut = CreateSut(new InMemoryFrendzCredentialsRepository(new FrendzApiCredentials
        {
            Id = frendzId,
            Name = "Frendz Key",
            Token = "token",
        }));

        var exists = await sut.ExistsAsync(frendzId);

        Assert.True(exists);
    }

    [Fact]
    public async Task ExistsAsync_SigiloPayCredential_ReturnsTrue()
    {
        var sigiloId = "sigilo-cred-1";
        var sut = CreateSut(
            sigiloPay: new InMemorySigiloPayCredentialsRepository(new SigiloPayApiCredentials
            {
                Id = sigiloId,
                Name = "Sigilo Key",
                PublicKey = "pub",
                SecretKey = "sec",
            }));

        var exists = await sut.ExistsAsync(sigiloId);

        Assert.True(exists);
    }

    [Fact]
    public async Task ExistsAsync_WintechCredential_ReturnsTrue()
    {
        var wintechId = "wintech-cred-1";
        var sut = CreateSut(
            wintech: new InMemoryWintechCredentialsRepository(new WintechApiCredentials
            {
                Id = wintechId,
                Name = "Wintech Key",
                PublicKey = "pub",
                SecretKey = "sec",
            }));

        var exists = await sut.ExistsAsync(wintechId);

        Assert.True(exists);
    }

    [Fact]
    public async Task ExistsAsync_TrimsCredentialId()
    {
        var id = "trimmed-id";
        var sut = CreateSut(new InMemoryFrendzCredentialsRepository(new FrendzApiCredentials
        {
            Id = id,
            Name = "Key",
            Token = "token",
        }));

        var exists = await sut.ExistsAsync($"  {id}  ");

        Assert.True(exists);
    }
}

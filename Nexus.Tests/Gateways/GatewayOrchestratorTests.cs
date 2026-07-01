using Aidan.Core.Linq;
using Aidan.Core.Patterns;
using Aidan.Mongo.Linq;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Nexus.Gateways.Application.Contracts;
using Nexus.Gateways.Application.Models;
using Nexus.Gateways.Application.Requests;
using Nexus.Gateways.Application.Responses;
using Nexus.Gateways.Application.Options;
using Nexus.Gateways.Application.Requests;
using Nexus.Gateways.Application.Responses;
using Nexus.Gateways.Application.Services;
using Nexus.Gateways.Frendz.Application.Contracts;
using Nexus.Gateways.Frendz.Application.Models;
using Nexus.Gateways.SigiloPay.Application.Contracts;
using Nexus.Gateways.SigiloPay.Application.Models;
using Nexus.Gateways.Wintech.Application.Contracts;
using Nexus.Gateways.Wintech.Application.Models;
using Nexus.Payments.Errors;
using Xunit;

namespace Nexus.Tests.Gateways;

public sealed class GatewayOrchestratorTests
{
    [Fact]
    public async Task TryCreatePixAsync_WhenPaymentIdMissing_ReturnsFailure()
    {
        var sut = CreateSut();

        var result = await sut.TryCreatePixAsync(new TryCreatePixRequest
        {
            PaymentId = "",
            Amount = 10m,
            Credentials = [new GatewayCredentialReference { Gateway = PaymentGateway.Frendz, CredentialId = "1" }],
        });

        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors, e => e.Code == PixPaymentErrorCodes.PaymentIdInvalid);
    }

    [Fact]
    public async Task TryCreatePixAsync_WhenCredentialsEmpty_ReturnsFailure()
    {
        var sut = CreateSut();

        var result = await sut.TryCreatePixAsync(new TryCreatePixRequest
        {
            PaymentId = "pay-1",
            Amount = 10m,
            Credentials = [],
        });

        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors, e => e.Code == PixPaymentErrorCodes.NoGatewayServicesAvailable);
    }

    [Fact]
    public async Task TryCreatePixAsync_WhenCredentialSucceeds_ReturnsPixResponse()
    {
        var cred = new FrendzApiCredentials { Id = "cred-1", Name = "c", Token = "tok", StrawManId = "straw-1" };
        var sut = CreateSut(
            frendz: new SingleFrendzCredentialsRepository(cred),
            gatewayService: new StubGatewayService("trx-1", "pix-123"));

        var result = await sut.TryCreatePixAsync(new TryCreatePixRequest
        {
            PaymentId = "pay-1",
            Amount = 10m,
            Credentials = [new GatewayCredentialReference { Gateway = PaymentGateway.Frendz, CredentialId = "cred-1" }],
        });

        Assert.True(result.IsSuccess);
        Assert.Equal("trx-1", result.Value!.TransactionId);
        Assert.Equal("pix-123", result.Value.PixCode);
        Assert.Equal(PaymentGateway.Frendz, result.Value.Gateway);
        Assert.Equal("cred-1", result.Value.CredentialId);
    }

    [Fact]
    public async Task TryCreatePixAsync_WhenCredentialDisabled_SkipsAndFails()
    {
        var cred = new FrendzApiCredentials { Id = "cred-1", Name = "c", Token = "tok", Enabled = false, StrawManId = "straw-1" };
        var sut = CreateSut(frendz: new SingleFrendzCredentialsRepository(cred));

        var result = await sut.TryCreatePixAsync(new TryCreatePixRequest
        {
            PaymentId = "pay-1",
            Amount = 10m,
            Credentials = [new GatewayCredentialReference { Gateway = PaymentGateway.Frendz, CredentialId = "cred-1" }],
        });

        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors, e => e.Code == PixPaymentErrorCodes.GatewayPixFailed);
    }

    [Fact]
    public async Task TryCreatePixAsync_WhenMockEnabled_ReturnsMockPixWithoutCallingService()
    {
        var sut = CreateSut(useMock: true);

        var result = await sut.TryCreatePixAsync(new TryCreatePixRequest
        {
            PaymentId = "pay-mock",
            Amount = 25.50m,
            Credentials = [new GatewayCredentialReference { Gateway = PaymentGateway.Wintech, CredentialId = "any" }],
        });

        Assert.True(result.IsSuccess);
        Assert.StartsWith("mock-pay-mock", result.Value!.TransactionId);
        Assert.Contains("pay-mock", result.Value.PixCode);
        Assert.Equal(PaymentGateway.Wintech, result.Value.Gateway);
    }

    private static GatewayOrchestrator CreateSut(
        IFrendzApiCredentialsRepository? frendz = null,
        ISigiloPayApiCredentialsRepository? sigiloPay = null,
        IWintechApiCredentialsRepository? wintech = null,
        IGatewayService? gatewayService = null,
        bool useMock = false)
    {
        gatewayService ??= new StubGatewayService("trx-default", "pix-default");

        return new GatewayOrchestrator(
            frendz ?? new EmptyFrendzCredentialsRepository(),
            sigiloPay ?? new EmptySigiloPayCredentialsRepository(),
            wintech ?? new EmptyWintechCredentialsRepository(),
            new StubFrendzServiceFactory(gatewayService),
            new StubSigiloPayServiceFactory(gatewayService),
            new StubWintechServiceFactory(gatewayService),
            Options.Create(new GatewaysOptions { UseMockOrchestrator = useMock }),
            NullLogger<GatewayOrchestrator>.Instance);
    }

    private sealed class StubGatewayService(string transactionId, string pixCode) : IGatewayService
    {
        public Task<CreatePixResponse> CreatePixAsync(CreatePixRequest request) =>
            Task.FromResult(new CreatePixResponse
            {
                TransactionId = transactionId,
                PixCode = pixCode,
            });
    }

    private sealed class StubFrendzServiceFactory(IGatewayService service) : IFrendzServiceFactory
    {
        public Task<IGatewayService> CreateAsync(FrendzApiCredentials credentials) =>
            Task.FromResult(service);
    }

    private sealed class StubSigiloPayServiceFactory(IGatewayService service) : ISigiloPayServiceFactory
    {
        public Task<IGatewayService> CreateAsync(SigiloPayApiCredentials credentials) =>
            Task.FromResult(service);
    }

    private sealed class StubWintechServiceFactory(IGatewayService service) : IWintechServiceFactory
    {
        public Task<IGatewayService> CreateAsync(WintechApiCredentials credentials) =>
            Task.FromResult(service);
    }

    private sealed class EmptyFrendzCredentialsRepository : IFrendzApiCredentialsRepository
    {
        public IAsyncQueryable<FrendzApiCredentials> AsQueryable() =>
            new QueryableToAsyncQueryableAdapter<FrendzApiCredentials>(Array.Empty<FrendzApiCredentials>().AsQueryable());

        public Task<FrendzApiCredentials> CreateAsync(FrendzApiCredentials entity) => throw new NotSupportedException();
        async Task IRepository<FrendzApiCredentials>.CreateAsync(FrendzApiCredentials entity) { await CreateAsync(entity); }
        public Task CreateAsync(IEnumerable<FrendzApiCredentials> entities) => throw new NotSupportedException();
        public Task DeleteAsync(FrendzApiCredentials entity) => throw new NotSupportedException();
        public Task<long> DeleteAsync(System.Linq.Expressions.Expression<Func<FrendzApiCredentials, bool>> predicate) => throw new NotSupportedException();
        public Task UpdateAsync(FrendzApiCredentials entity) => throw new NotSupportedException();
        public Task<long> UpdateAsync(System.Linq.Expressions.Expression expression) => throw new NotSupportedException();
    }

    private sealed class SingleFrendzCredentialsRepository(FrendzApiCredentials credential) : IFrendzApiCredentialsRepository
    {
        public IAsyncQueryable<FrendzApiCredentials> AsQueryable() =>
            new QueryableToAsyncQueryableAdapter<FrendzApiCredentials>(new[] { credential }.AsQueryable());

        public Task<FrendzApiCredentials> CreateAsync(FrendzApiCredentials entity) => throw new NotSupportedException();
        async Task IRepository<FrendzApiCredentials>.CreateAsync(FrendzApiCredentials entity) { await CreateAsync(entity); }
        public Task CreateAsync(IEnumerable<FrendzApiCredentials> entities) => throw new NotSupportedException();
        public Task DeleteAsync(FrendzApiCredentials entity) => throw new NotSupportedException();
        public Task<long> DeleteAsync(System.Linq.Expressions.Expression<Func<FrendzApiCredentials, bool>> predicate) => throw new NotSupportedException();
        public Task UpdateAsync(FrendzApiCredentials entity) => throw new NotSupportedException();
        public Task<long> UpdateAsync(System.Linq.Expressions.Expression expression) => throw new NotSupportedException();
    }

    private sealed class EmptySigiloPayCredentialsRepository : ISigiloPayApiCredentialsRepository
    {
        public IAsyncQueryable<SigiloPayApiCredentials> AsQueryable() =>
            new QueryableToAsyncQueryableAdapter<SigiloPayApiCredentials>(Array.Empty<SigiloPayApiCredentials>().AsQueryable());

        public Task<SigiloPayApiCredentials> CreateAsync(SigiloPayApiCredentials entity) => throw new NotSupportedException();
        async Task IRepository<SigiloPayApiCredentials>.CreateAsync(SigiloPayApiCredentials entity) { await CreateAsync(entity); }
        public Task CreateAsync(IEnumerable<SigiloPayApiCredentials> entities) => throw new NotSupportedException();
        public Task DeleteAsync(SigiloPayApiCredentials entity) => throw new NotSupportedException();
        public Task<long> DeleteAsync(System.Linq.Expressions.Expression<Func<SigiloPayApiCredentials, bool>> predicate) => throw new NotSupportedException();
        public Task UpdateAsync(SigiloPayApiCredentials entity) => throw new NotSupportedException();
        public Task<long> UpdateAsync(System.Linq.Expressions.Expression expression) => throw new NotSupportedException();
    }

    private sealed class EmptyWintechCredentialsRepository : IWintechApiCredentialsRepository
    {
        public IAsyncQueryable<WintechApiCredentials> AsQueryable() =>
            new QueryableToAsyncQueryableAdapter<WintechApiCredentials>(Array.Empty<WintechApiCredentials>().AsQueryable());

        public Task<WintechApiCredentials> CreateAsync(WintechApiCredentials entity) => throw new NotSupportedException();
        async Task IRepository<WintechApiCredentials>.CreateAsync(WintechApiCredentials entity) { await CreateAsync(entity); }
        public Task CreateAsync(IEnumerable<WintechApiCredentials> entities) => throw new NotSupportedException();
        public Task DeleteAsync(WintechApiCredentials entity) => throw new NotSupportedException();
        public Task<long> DeleteAsync(System.Linq.Expressions.Expression<Func<WintechApiCredentials, bool>> predicate) => throw new NotSupportedException();
        public Task UpdateAsync(WintechApiCredentials entity) => throw new NotSupportedException();
        public Task<long> UpdateAsync(System.Linq.Expressions.Expression expression) => throw new NotSupportedException();
    }
}

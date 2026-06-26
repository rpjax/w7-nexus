using System.Linq.Expressions;
using Aidan.Core.Linq;
using Aidan.Core.Patterns;
using Aidan.Mongo.Linq;
using Microsoft.Extensions.Logging.Abstractions;
using Nexus.Gateways.Aggregates;
using Nexus.Gateways.Application.Contracts;
using Nexus.Gateways.Application.Models;
using Nexus.Gateways.Application.Services;
using Nexus.Gateways.Frendz.Application.Contracts;
using Nexus.Gateways.Frendz.Application.Models;
using Nexus.Gateways.Frendz.Application.Services;
using Nexus.Gateways.SigiloPay.Application.Models;
using Nexus.Gateways.Wintech.Application.Models;
using Nexus.Gateways.SigiloPay.Application.Services;
using Nexus.Gateways.SigiloPay.Application.Contracts;
using Nexus.Gateways.Wintech.Application.Contracts;
using Nexus.Gateways.Wintech.Application.Services;
using Nexus.Database.Models;
using Nexus.Operations.Aggregates;
using Nexus.Operations.Application.Contracts;
using Nexus.Payments.Aggregates;
using Nexus.Payments.Application.Contracts;
using Nexus.Payments.Application.Models;
using Nexus.Payments.Errors;
using Nexus.Tests.Payments;
using Xunit;

namespace Nexus.Tests.Gateways;

public sealed class MockGatewayOrchestratorTests
{
    [Fact]
    public async Task CreateGatewayPixAsync_WhenOperationMissing_ReturnsFailure()
    {
        var sut = CreateSut(new EmptyOperationRepository());

        var result = await sut.CreateGatewayPixAsync(new CreateGatewayPixRequest
        {
            OperationId = "missing",
            Amount = 10m,
        });

        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors, e => e.Code == PixPaymentErrorCodes.OperationNotFound);
    }

    [Fact]
    public async Task CreateGatewayPixAsync_WithoutOperator_ReturnsMockPix()
    {
        var operation = CreateOperation("op-1");
        var cred = new FrendzApiCredentials { Id = "cred-1", Name = "c", Token = "tok", StrawManId = "straw-1" };
        var paymentRepo = new TrackingPaymentRepository();
        var sut = CreateSut(
            new SingleOperationRepository(operation),
            paymentRepo,
            frendz: new SingleFrendzCredentialsRepository(cred));

        var result = await sut.CreateGatewayPixAsync(new CreateGatewayPixRequest
        {
            OperationId = "op-1",
            Amount = 25.50m,
        });

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.StartsWith("000201", result.Value!.Code);
        Assert.Contains("MOCK", result.Value.Code);
        Assert.True(paymentRepo.WasUpdated);
    }

    [Fact]
    public async Task CreateGatewayPixAsync_WithOperator_UsesTeamCredentialOwner()
    {
        var operation = CreateOperation("op-1");
        var team = new Team(
            "team-1",
            "op-1",
            "Team A",
            null,
            new[] { "operator-1" },
            new[] { "straw-1" },
            GatewaySelectionStrategy.PerStrawman,
            Array.Empty<string>(),
            Array.Empty<string>(),
            Array.Empty<OperatorProfitShareRuleRecord>(),
            DateTime.UtcNow,
            DateTime.UtcNow);

        var cred = new FrendzApiCredentials { Id = "cred-1", Name = "c", Token = "tok", StrawManId = "straw-1" };
        var paymentRepo = new TrackingPaymentRepository();
        var sut = CreateSut(
            new SingleOperationRepository(operation),
            paymentRepo,
            new SingleTeamRepository(team),
            frendz: new SingleFrendzCredentialsRepository(cred));

        var result = await sut.CreateGatewayPixAsync(new CreateGatewayPixRequest
        {
            OperationId = "op-1",
            OperatorId = "operator-1",
            Amount = 10m,
        });

        Assert.True(result.IsSuccess);
        Assert.StartsWith("mock-", result.Value!.Id);
        Assert.Equal("straw-1", paymentRepo.LastBoundStrawManId);
    }

    [Fact]
    public async Task CreateGatewayPixAsync_WhenNoCredentialsAvailable_ReturnsNoGatewayServicesAvailable()
    {
        var operation = CreateOperation("op-1", credentialIds: Array.Empty<string>());
        var sut = CreateSut(new SingleOperationRepository(operation));

        var result = await sut.CreateGatewayPixAsync(new CreateGatewayPixRequest
        {
            OperationId = "op-1",
            Amount = 10m,
        });

        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors, e => e.Code == PixPaymentErrorCodes.NoGatewayServicesAvailable);
    }

    private static MockGatewayOrchestrator CreateSut(
        IOperationRepository operations,
        TrackingPaymentRepository? paymentRepo = null,
        ITeamRepository? teams = null,
        IFrendzApiCredentialsRepository? frendz = null)
    {
        paymentRepo ??= new TrackingPaymentRepository();
        teams ??= new EmptyTeamRepository();
        frendz ??= new EmptyFrendzCredentialsRepository();

        var gatewayPixService = new StubGatewayPixService();
        var resolver = new GatewayCredentialProviderResolver(
            frendz,
            new StubGatewayPixServiceFactory(gatewayPixService),
            new EmptySigiloPayCredentialsRepository(),
            new StubSigiloPayGatewayPixServiceFactory(gatewayPixService),
            new EmptyWintechCredentialsRepository(),
            new StubWintechGatewayPixServiceFactory(gatewayPixService),
            new EmptyGatewayCredentialsGroupRepository());

        return new MockGatewayOrchestrator(
            operations,
            teams,
            new StubPaymentService(),
            paymentRepo,
            PaymentTestDoubles.SplitCalculation(),
            resolver,
            NullLogger<MockGatewayOrchestrator>.Instance);
    }

    private static Operation CreateOperation(string id, string[]? credentialIds = null)
        => new(
            id,
            "Operation",
            "Description",
            Array.Empty<string>(),
            new[] { "straw-1" },
            GatewaySelectionStrategy.Manual,
            credentialIds ?? new[] { "cred-1" },
            Array.Empty<string>(),
            DateTime.UtcNow,
            DateTime.UtcNow);

    private sealed class StubPaymentService : IPaymentService
    {
        public Task<IResult<Payment>> CreatePaymentAsync(CreatePaymentRequest request)
        {
            var id = string.IsNullOrWhiteSpace(request.ExplicitPaymentId)
                ? "pay-1"
                : request.ExplicitPaymentId!.Trim();

            var payment = PaymentTestFactory.Create(
                id,
                request.OperationId!,
                request.Gateway,
                request.GatewayPaymentId!,
                request.Amount,
                strawManId: request.StrawManId ?? string.Empty);

            IResult<Payment> ok = Result.Create<Payment>().WithValue(payment).Build();
            return Task.FromResult(ok);
        }

        public Task<IResult> DeletePaymentAsync(string paymentId) =>
            Task.FromResult<IResult>(Result.Success());

        public Task<IResult> PayAsync(string paymentId) =>
            Task.FromResult<IResult>(Result.Success());

        public Task<IResult> RefundAsync(string paymentId) =>
            Task.FromResult<IResult>(Result.Success());

        public Task<IResult> KillAsync(string paymentId, string reason) =>
            Task.FromResult<IResult>(Result.Success());

        public Task<IResult> MarkAsDistributedAsync(string paymentId) =>
            Task.FromResult<IResult>(Result.Success());

        public Task<IResult<Payment>> GetByIdAsync(string paymentId)
        {
            IResult<Payment> ok = Result.Create<Payment>()
                .WithValue(PaymentTestFactory.Create(id: paymentId))
                .Build();
            return Task.FromResult(ok);
        }

        public Task<IResult<Payment>> BindOperatorAsync(string paymentId, string OperatorId)
        {
            IResult<Payment> ok = Result.Create<Payment>()
                .WithValue(PaymentTestFactory.Create(id: paymentId, operatorId: OperatorId))
                .Build();
            return Task.FromResult(ok);
        }

        public Task<IResult<Payment>> BindStrawManAsync(string paymentId, string StrawManId)
        {
            IResult<Payment> ok = Result.Create<Payment>()
                .WithValue(PaymentTestFactory.Create(id: paymentId, strawManId: StrawManId))
                .Build();
            return Task.FromResult(ok);
        }
    }

    private sealed class TrackingPaymentRepository : IPaymentRepository
    {
        public bool WasUpdated { get; private set; }
        public string? LastBoundStrawManId { get; private set; }

        public IAsyncQueryable<Payment> AsQueryable() =>
            new MongoAsyncQueryable<Payment>(Array.Empty<Payment>().AsQueryable());

        public Task<Payment> CreateAsync(Payment entity) => Task.FromResult(entity);
        async Task IRepository<Payment>.CreateAsync(Payment entity) { await CreateAsync(entity); }
        public Task CreateAsync(IEnumerable<Payment> entities) => Task.CompletedTask;
        public Task DeleteAsync(Payment entity) => Task.CompletedTask;
        public Task<long> DeleteAsync(Expression<Func<Payment, bool>> predicate) => Task.FromResult(0L);

        public Task UpdateAsync(Payment entity)
        {
            WasUpdated = true;
            LastBoundStrawManId = entity.StrawManId;
            return Task.CompletedTask;
        }

        public Task<long> UpdateAsync(Expression expression) => Task.FromResult(0L);
    }

    private sealed class EmptyOperationRepository : IOperationRepository
    {
        public IAsyncQueryable<Operation> AsQueryable() =>
            new MongoAsyncQueryable<Operation>(Array.Empty<Operation>().AsQueryable());

        public Task<Operation> CreateAsync(Operation entity) => Task.FromResult(entity);
        async Task IRepository<Operation>.CreateAsync(Operation entity) { await CreateAsync(entity); }
        public Task CreateAsync(IEnumerable<Operation> entities) => Task.CompletedTask;
        public Task DeleteAsync(Operation entity) => Task.CompletedTask;
        public Task<long> DeleteAsync(Expression<Func<Operation, bool>> predicate) => Task.FromResult(0L);
        public Task UpdateAsync(Operation entity) => Task.CompletedTask;
        public Task<long> UpdateAsync(Expression expression) => Task.FromResult(0L);
    }

    private sealed class SingleOperationRepository : IOperationRepository
    {
        private readonly Operation _operation;

        public SingleOperationRepository(Operation operation) => _operation = operation;

        public IAsyncQueryable<Operation> AsQueryable() =>
            new MongoAsyncQueryable<Operation>(new[] { _operation }.AsQueryable());

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
            new MongoAsyncQueryable<Team>(Array.Empty<Team>().AsQueryable());

        public Task<Team> CreateAsync(Team entity) => Task.FromResult(entity);
        async Task IRepository<Team>.CreateAsync(Team entity) { await CreateAsync(entity); }
        public Task CreateAsync(IEnumerable<Team> entities) => Task.CompletedTask;
        public Task DeleteAsync(Team entity) => Task.CompletedTask;
        public Task<long> DeleteAsync(Expression<Func<Team, bool>> predicate) => Task.FromResult(0L);
        public Task UpdateAsync(Team entity) => Task.CompletedTask;
        public Task<long> UpdateAsync(Expression expression) => Task.FromResult(0L);
    }

    private sealed class SingleTeamRepository : ITeamRepository
    {
        private readonly Team _team;

        public SingleTeamRepository(Team team) => _team = team;

        public IAsyncQueryable<Team> AsQueryable() =>
            new MongoAsyncQueryable<Team>(new[] { _team }.AsQueryable());

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
            new MongoAsyncQueryable<FrendzApiCredentials>(Array.Empty<FrendzApiCredentials>().AsQueryable());

        public Task<FrendzApiCredentials> CreateAsync(FrendzApiCredentials entity) => throw new NotSupportedException();
        async Task IRepository<FrendzApiCredentials>.CreateAsync(FrendzApiCredentials entity) { await CreateAsync(entity); }
        public Task CreateAsync(IEnumerable<FrendzApiCredentials> entities) => throw new NotSupportedException();
        public Task DeleteAsync(FrendzApiCredentials entity) => throw new NotSupportedException();
        public Task<long> DeleteAsync(Expression<Func<FrendzApiCredentials, bool>> predicate) => throw new NotSupportedException();
        public Task UpdateAsync(FrendzApiCredentials entity) => throw new NotSupportedException();
        public Task<long> UpdateAsync(Expression expression) => throw new NotSupportedException();
    }

    private sealed class SingleFrendzCredentialsRepository : IFrendzApiCredentialsRepository
    {
        private readonly FrendzApiCredentials _credential;

        public SingleFrendzCredentialsRepository(FrendzApiCredentials credential) => _credential = credential;

        public IAsyncQueryable<FrendzApiCredentials> AsQueryable() =>
            new MongoAsyncQueryable<FrendzApiCredentials>(new[] { _credential }.AsQueryable());

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
            new MongoAsyncQueryable<SigiloPayApiCredentials>(Array.Empty<SigiloPayApiCredentials>().AsQueryable());

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
            new MongoAsyncQueryable<WintechApiCredentials>(Array.Empty<WintechApiCredentials>().AsQueryable());

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
            new MongoAsyncQueryable<GatewayCredentialsGroup>(Array.Empty<GatewayCredentialsGroup>().AsQueryable());

        public Task<GatewayCredentialsGroup> CreateAsync(GatewayCredentialsGroup entity) => throw new NotSupportedException();
        async Task IRepository<GatewayCredentialsGroup>.CreateAsync(GatewayCredentialsGroup entity) { await CreateAsync(entity); }
        public Task CreateAsync(IEnumerable<GatewayCredentialsGroup> entities) => throw new NotSupportedException();
        public Task DeleteAsync(GatewayCredentialsGroup entity) => throw new NotSupportedException();
        public Task<long> DeleteAsync(Expression<Func<GatewayCredentialsGroup, bool>> predicate) => throw new NotSupportedException();
        public Task UpdateAsync(GatewayCredentialsGroup entity) => throw new NotSupportedException();
        public Task<long> UpdateAsync(Expression expression) => throw new NotSupportedException();
    }

    private sealed class StubGatewayPixService : IGatewayPixService
    {
        public Task<GatewayPix> CreateGatewayPixAsync(CreateGatewayPixRequest request) =>
            Task.FromResult(new GatewayPix { Id = request.PaymentId, Code = "pix" });
    }

    private sealed class StubGatewayPixServiceFactory : IFrendzGatewayPixServiceFactory
    {
        private readonly IGatewayPixService _service;

        public StubGatewayPixServiceFactory(IGatewayPixService service) => _service = service;

        public IGatewayPixService Create(FrendzApiCredentials credentials) => _service;
    }

    private sealed class StubSigiloPayGatewayPixServiceFactory : ISigiloPayGatewayPixServiceFactory
    {
        private readonly IGatewayPixService _service;

        public StubSigiloPayGatewayPixServiceFactory(IGatewayPixService service) => _service = service;

        public IGatewayPixService Create(SigiloPayApiCredentials credentials) => _service;
    }

    private sealed class StubWintechGatewayPixServiceFactory : IWintechGatewayPixServiceFactory
    {
        private readonly IGatewayPixService _service;

        public StubWintechGatewayPixServiceFactory(IGatewayPixService service) => _service = service;

        public IGatewayPixService Create(WintechApiCredentials credentials) => _service;
    }
}

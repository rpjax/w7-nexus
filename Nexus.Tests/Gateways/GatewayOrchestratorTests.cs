using System.Linq.Expressions;
using Nexus.Gateways.Wintech.Application.Contracts;
using Nexus.Gateways.SigiloPay.Application.Contracts;
using Nexus.Gateways.Frendz.Application.Contracts;
using Nexus.Gateways.Application.Contracts;
using Nexus.Payments.Application.Contracts;
using Nexus.Operations.Application.Contracts;
using Aidan.Core.Linq;
using Aidan.Core.Patterns;
using Aidan.Mongo.Linq;
using Xunit;
using Nexus.Operations.Aggregates;
using Nexus.Operations.Application;
using Nexus.Database.Models;
using Nexus.Gateways.Wintech.Application.Models;
using Nexus.Gateways.Wintech.Application;
using Nexus.Gateways.Entities;
using Nexus.Gateways.Application;
using Nexus.Gateways.Frendz.Application;
using Nexus.Gateways.Application.Models;
using Nexus.Gateways.Frendz.Application.Models;
using Nexus.Gateways.SigiloPay.Application.Models;
using Nexus.Gateways.SigiloPay.Application;
using Nexus.Payments.Aggregates;
using Nexus.Payments.Application;
using Nexus.Payments.Application.Models;
using Nexus.Payments.Errors;

namespace Nexus.Tests.Gateways;

public sealed class GatewayOrchestratorTests
{
    [Fact]
    public async Task CreateGatewayPixAsync_WhenOperationMissing_ReturnsFailure()
    {
        var sut = new GatewayOrchestrator(
            new EmptyOperationRepository(),
            new EmptyTeamRepository(),
            new StubPaymentService(),
            new StubPaymentRepository(),
            new EmptyFrendzCredentialsRepository(),
            new StubGatewayPixServiceFactory(new StubGatewayPixService()),
            new EmptySigiloPayCredentialsRepository(),
            new StubSigiloPayGatewayPixServiceFactory(new StubGatewayPixService()),
            new EmptyWintechCredentialsRepository(),
            new StubWintechGatewayPixServiceFactory(new StubGatewayPixService()),
            new EmptyGatewayCredentialsGroupRepository());

        var result = await sut.CreateGatewayPixAsync(new CreateGatewayPixRequest
        {
            OperationId = "missing",
            OperatorAccountId = "operator-1",
            Amount = 10m
        });

        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors, e => e.Code == PixPaymentErrorCodes.OperationNotFound);
    }

    [Fact]
    public async Task CreateGatewayPixAsync_WhenGatewayPixSucceeds_ReturnsGatewayPixAndUpdatesPayment()
    {
        var operation = new Operation(
            "op-1",
            "N",
            "D",
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
            (int)GatewaySelectionStrategy.PerStrawman,
            Array.Empty<string>(),
            Array.Empty<string>(),
            Array.Empty<OperatorProfitShareRuleRecord>(),
            DateTime.UtcNow,
            DateTime.UtcNow);

        var cred = new FrendzApiCredentials { Id = "1", Name = "c", Token = "tok" };
        var paymentRepo = new StubPaymentRepository();
        var gatewayPixService = new StubGatewayPixService
        {
            OnCreate = r => Task.FromResult(new GatewayPix
            {
                Id = r.PaymentId,
                Code = "pix-code"
            })
        };

        var sut = new GatewayOrchestrator(
            new SingleOperationRepository(operation),
            new SingleTeamRepository(team),
            new StubPaymentService(),
            paymentRepo,
            new SingleFrendzCredentialsRepository(cred),
            new StubGatewayPixServiceFactory(gatewayPixService),
            new EmptySigiloPayCredentialsRepository(),
            new StubSigiloPayGatewayPixServiceFactory(gatewayPixService),
            new EmptyWintechCredentialsRepository(),
            new StubWintechGatewayPixServiceFactory(gatewayPixService),
            new EmptyGatewayCredentialsGroupRepository());

        var result = await sut.CreateGatewayPixAsync(new CreateGatewayPixRequest
        {
            OperationId = "op-1",
            OperatorAccountId = "operator-1",
            Amount = 10m
        });

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal("pix-code", result.Value!.Code);
        Assert.True(paymentRepo.WasUpdated);
    }

    [Fact]
    public async Task CreateGatewayPixAsync_WhenPerGroupStrategy_UsesCredentialsFromAssignedGroups()
    {
        var operation = new Operation(
            "op-1",
            "N",
            "D",
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
            (int)GatewaySelectionStrategy.PerGroup,
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

        var cred = new FrendzApiCredentials { Id = "cred-1", Name = "c", Token = "tok" };
        var paymentRepo = new StubPaymentRepository();
        var gatewayPixService = new StubGatewayPixService
        {
            OnCreate = r => Task.FromResult(new GatewayPix
            {
                Id = r.PaymentId,
                Code = "pix-group"
            })
        };

        var sut = new GatewayOrchestrator(
            new SingleOperationRepository(operation),
            new SingleTeamRepository(team),
            new StubPaymentService(),
            paymentRepo,
            new SingleFrendzCredentialsRepository(cred),
            new StubGatewayPixServiceFactory(gatewayPixService),
            new EmptySigiloPayCredentialsRepository(),
            new StubSigiloPayGatewayPixServiceFactory(gatewayPixService),
            new EmptyWintechCredentialsRepository(),
            new StubWintechGatewayPixServiceFactory(gatewayPixService),
            new SingleGatewayCredentialsGroupRepository(group));

        var result = await sut.CreateGatewayPixAsync(new CreateGatewayPixRequest
        {
            OperationId = "op-1",
            OperatorAccountId = "operator-1",
            Amount = 10m
        });

        Assert.True(result.IsSuccess);
        Assert.Equal("pix-group", result.Value!.Code);
    }

    private sealed class StubPaymentService : IPaymentService
    {
        public Task<IResult<Payment>> CreatePaymentAsync(CreatePaymentRequest request)
        {
            var id = string.IsNullOrWhiteSpace(request.ExplicitPaymentId)
                ? "pay-1"
                : request.ExplicitPaymentId!.Trim();
            var payment = new Payment(
                id,
                request.OperationId!,
                request.Gateway,
                request.GatewayPaymentId!,
                request.Amount,
                PaymentStatus.Pending,
                operatorAccountId: null,
                strawManAccountId: null,
                DateTime.UtcNow,
                paidAt: null,
                refundedAt: null,
                diedAt: null,
                deathReason: null);
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
    }

    private sealed class StubPaymentRepository : IPaymentRepository
    {
        public bool WasUpdated { get; private set; }

        public IAsyncQueryable<Payment> AsQueryable() =>
            new MongoAsyncQueryable<Payment>(Array.Empty<Payment>().AsQueryable());

        public Task CreateAsync(Payment entity) => Task.CompletedTask;
        public Task CreateAsync(IEnumerable<Payment> entities) => Task.CompletedTask;
        public Task DeleteAsync(Payment entity) => Task.CompletedTask;
        public Task<long> DeleteAsync(Expression<Func<Payment, bool>> predicate) => Task.FromResult(0L);

        public Task UpdateAsync(Payment entity)
        {
            WasUpdated = true;
            return Task.CompletedTask;
        }

        public Task<long> UpdateAsync(Expression expression) => Task.FromResult(0L);
    }

    private sealed class EmptyOperationRepository : IOperationRepository
    {
        public IAsyncQueryable<Operation> AsQueryable() =>
            new MongoAsyncQueryable<Operation>(Array.Empty<Operation>().AsQueryable());

        public Task CreateAsync(Operation entity) => Task.CompletedTask;
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

        public Task CreateAsync(Operation entity) => Task.CompletedTask;
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

        public Task CreateAsync(Team entity) => Task.CompletedTask;
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

        public Task CreateAsync(Team entity) => Task.CompletedTask;
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

        public Task CreateAsync(FrendzApiCredentials entity) => throw new NotSupportedException();
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

        public Task CreateAsync(FrendzApiCredentials entity) => throw new NotSupportedException();
        public Task CreateAsync(IEnumerable<FrendzApiCredentials> entities) => throw new NotSupportedException();
        public Task DeleteAsync(FrendzApiCredentials entity) => throw new NotSupportedException();
        public Task<long> DeleteAsync(Expression<Func<FrendzApiCredentials, bool>> predicate) => throw new NotSupportedException();
        public Task UpdateAsync(FrendzApiCredentials entity) => throw new NotSupportedException();
        public Task<long> UpdateAsync(Expression expression) => throw new NotSupportedException();
    }

    private sealed class StubGatewayPixService : IGatewayPixService
    {
        public Func<CreateGatewayPixRequest, Task<GatewayPix>>? OnCreate { get; init; }

        public Task<GatewayPix> CreateGatewayPixAsync(CreateGatewayPixRequest request)
        {
            if (OnCreate is null)
                throw new InvalidOperationException();
            return OnCreate(request);
        }
    }

    private sealed class StubGatewayPixServiceFactory : IFrendzGatewayPixServiceFactory
    {
        private readonly IGatewayPixService _service;

        public StubGatewayPixServiceFactory(IGatewayPixService service) => _service = service;

        public IGatewayPixService Create(FrendzApiCredentials credentials) => _service;
    }

    private sealed class EmptySigiloPayCredentialsRepository : ISigiloPayApiCredentialsRepository
    {
        public IAsyncQueryable<SigiloPayApiCredentials> AsQueryable() =>
            new MongoAsyncQueryable<SigiloPayApiCredentials>(Array.Empty<SigiloPayApiCredentials>().AsQueryable());

        public Task CreateAsync(SigiloPayApiCredentials entity) => throw new NotSupportedException();
        public Task CreateAsync(IEnumerable<SigiloPayApiCredentials> entities) => throw new NotSupportedException();
        public Task DeleteAsync(SigiloPayApiCredentials entity) => throw new NotSupportedException();
        public Task<long> DeleteAsync(Expression<Func<SigiloPayApiCredentials, bool>> predicate) => throw new NotSupportedException();
        public Task UpdateAsync(SigiloPayApiCredentials entity) => throw new NotSupportedException();
        public Task<long> UpdateAsync(Expression expression) => throw new NotSupportedException();
    }

    private sealed class StubSigiloPayGatewayPixServiceFactory : ISigiloPayGatewayPixServiceFactory
    {
        private readonly IGatewayPixService _service;

        public StubSigiloPayGatewayPixServiceFactory(IGatewayPixService service) => _service = service;

        public IGatewayPixService Create(SigiloPayApiCredentials credentials) => _service;
    }

    private sealed class EmptyWintechCredentialsRepository : IWintechApiCredentialsRepository
    {
        public IAsyncQueryable<WintechApiCredentials> AsQueryable() =>
            new MongoAsyncQueryable<WintechApiCredentials>(Array.Empty<WintechApiCredentials>().AsQueryable());

        public Task CreateAsync(WintechApiCredentials entity) => throw new NotSupportedException();
        public Task CreateAsync(IEnumerable<WintechApiCredentials> entities) => throw new NotSupportedException();
        public Task DeleteAsync(WintechApiCredentials entity) => throw new NotSupportedException();
        public Task<long> DeleteAsync(Expression<Func<WintechApiCredentials, bool>> predicate) => throw new NotSupportedException();
        public Task UpdateAsync(WintechApiCredentials entity) => throw new NotSupportedException();
        public Task<long> UpdateAsync(Expression expression) => throw new NotSupportedException();
    }

    private sealed class StubWintechGatewayPixServiceFactory : IWintechGatewayPixServiceFactory
    {
        private readonly IGatewayPixService _service;

        public StubWintechGatewayPixServiceFactory(IGatewayPixService service) => _service = service;

        public IGatewayPixService Create(WintechApiCredentials credentials) => _service;
    }

    private sealed class EmptyGatewayCredentialsGroupRepository : IGatewayCredentialsGroupRepository
    {
        public IAsyncQueryable<GatewayCredentialsGroup> AsQueryable() =>
            new MongoAsyncQueryable<GatewayCredentialsGroup>(Array.Empty<GatewayCredentialsGroup>().AsQueryable());

        public Task CreateAsync(GatewayCredentialsGroup entity) => throw new NotSupportedException();
        public Task CreateAsync(IEnumerable<GatewayCredentialsGroup> entities) => throw new NotSupportedException();
        public Task DeleteAsync(GatewayCredentialsGroup entity) => throw new NotSupportedException();
        public Task<long> DeleteAsync(Expression<Func<GatewayCredentialsGroup, bool>> predicate) => throw new NotSupportedException();
        public Task UpdateAsync(GatewayCredentialsGroup entity) => throw new NotSupportedException();
        public Task<long> UpdateAsync(Expression expression) => throw new NotSupportedException();
    }

    private sealed class SingleGatewayCredentialsGroupRepository : IGatewayCredentialsGroupRepository
    {
        private readonly GatewayCredentialsGroup _group;

        public SingleGatewayCredentialsGroupRepository(GatewayCredentialsGroup group) => _group = group;

        public IAsyncQueryable<GatewayCredentialsGroup> AsQueryable() =>
            new MongoAsyncQueryable<GatewayCredentialsGroup>(new[] { _group }.AsQueryable());

        public Task CreateAsync(GatewayCredentialsGroup entity) => throw new NotSupportedException();
        public Task CreateAsync(IEnumerable<GatewayCredentialsGroup> entities) => throw new NotSupportedException();
        public Task DeleteAsync(GatewayCredentialsGroup entity) => throw new NotSupportedException();
        public Task<long> DeleteAsync(Expression<Func<GatewayCredentialsGroup, bool>> predicate) => throw new NotSupportedException();
        public Task UpdateAsync(GatewayCredentialsGroup entity) => throw new NotSupportedException();
        public Task<long> UpdateAsync(Expression expression) => throw new NotSupportedException();
    }
}

using System.Linq.Expressions;
using Aidan.Core.Linq;
using Aidan.Core.Patterns;
using Aidan.Mongo.Linq;
using Microsoft.Extensions.Logging.Abstractions;
using Nexus.Gateways.Application.Models;
using Nexus.Gateways.Application.Services;
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
        var paymentRepo = new TrackingPaymentRepository();
        var sut = CreateSut(new SingleOperationRepository(operation), paymentRepo);

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
    public async Task CreateGatewayPixAsync_WithOperator_UsesTeamContext()
    {
        var operation = CreateOperation("op-1");
        var team = new Team(
            "team-1",
            "op-1",
            "Team A",
            null,
            new[] { "operator-1" },
            Array.Empty<string>(),
            GatewaySelectionStrategy.PerStrawman,
            Array.Empty<string>(),
            Array.Empty<string>(),
            Array.Empty<OperatorProfitShareRuleRecord>(),
            DateTime.UtcNow,
            DateTime.UtcNow);

        var paymentRepo = new TrackingPaymentRepository();
        var sut = CreateSut(
            new SingleOperationRepository(operation),
            paymentRepo,
            new SingleTeamRepository(team));

        var result = await sut.CreateGatewayPixAsync(new CreateGatewayPixRequest
        {
            OperationId = "op-1",
            OperatorAccountId = "operator-1",
            Amount = 10m,
        });

        Assert.True(result.IsSuccess);
        Assert.StartsWith("mock-", result.Value!.Id);
    }

    private static MockGatewayOrchestrator CreateSut(
        IOperationRepository operations,
        TrackingPaymentRepository? paymentRepo = null,
        ITeamRepository? teams = null)
    {
        paymentRepo ??= new TrackingPaymentRepository();
        teams ??= new EmptyTeamRepository();

        return new MockGatewayOrchestrator(
            operations,
            teams,
            new StubPaymentService(),
            paymentRepo,
            NullLogger<MockGatewayOrchestrator>.Instance);
    }

    private static Operation CreateOperation(string id)
        => new(
            id,
            "Operation",
            "Description",
            Array.Empty<string>(),
            Array.Empty<string>(),
            GatewaySelectionStrategy.Manual,
            Array.Empty<string>(),
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
                request.TeamId ?? string.Empty,
                request.Gateway,
                request.GatewayPaymentId!,
                request.Amount);

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

    private sealed class TrackingPaymentRepository : IPaymentRepository
    {
        public bool WasUpdated { get; private set; }

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
}

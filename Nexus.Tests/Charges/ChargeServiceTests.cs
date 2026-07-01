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
using Nexus.Database.Models;
using Nexus.Gateways.Aggregates;
using Nexus.Gateways.Application.Models;
using Nexus.Gateways.Application.Requests;
using Nexus.Gateways.Application.Responses;
using Nexus.Gateways.Frendz.Application.Models;
using Nexus.Gateways.SigiloPay.Application.Models;
using Nexus.Gateways.Wintech.Application.Models;
using Nexus.Payments.Aggregates;
using Nexus.Payments.Application.Models;
using Nexus.Payments.Errors;
using Nexus.Tests.Payments;
using Nexus.Charges.Application;
using Nexus.Charges.Application.Models;
using Nexus.Charges.Application.Services;

namespace Nexus.Tests.Charges;

public sealed class ChargeServiceTests
{
    [Fact]
    public async Task CreatePixChargeAsync_WhenGatewayPixSucceeds_ReturnsGatewayPixAndUpdatesPayment()
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
            new[] { "straw-1" },
            GatewaySelectionStrategy.PerStrawman,
            Array.Empty<string>(),
            Array.Empty<string>(),
            Array.Empty<OperatorProfitShareRuleRecord>(),
            DateTime.UtcNow,
            DateTime.UtcNow);

        var cred = new FrendzApiCredentials { Id = "1", Name = "c", Token = "tok", StrawManId = "straw-1" };
        var paymentRepo = new StubPaymentRepository();

        var sut = CreateSut(
            new SingleOperationRepository(operation),
            new SingleTeamRepository(team),
            new StubPaymentService(),
            paymentRepo,
            frendz: new SingleFrendzCredentialsRepository(cred));

        var result = await sut.CreatePixChargeAsync(new CreatePixChargeRequest
        {
            OperationId = "op-1",
            OperatorId = "operator-1",
            Amount = 10m
        });

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal("pix-code", result.Value!.PixCode);
        Assert.Equal(ChargeDefaults.PaymentRecipient, result.Value.PaymentRecipient);
        Assert.True(paymentRepo.WasUpdated);
    }

    [Fact]
    public async Task CreatePixChargeAsync_WhenPerGroupStrategy_UsesCredentialsFromAssignedGroups()
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

        var cred = new FrendzApiCredentials { Id = "cred-1", Name = "c", Token = "tok", StrawManId = "straw-1" };
        var paymentRepo = new StubPaymentRepository();
        var orchestrator = new StubGatewayOrchestrator
        {
            OnTry = _ => Result.Create<TryCreatePixResponse>()
                .WithValue(new TryCreatePixResponse
                {
                    TransactionId = "trx-group",
                    PixCode = "pix-group",
                    Gateway = PaymentGateway.Frendz,
                    CredentialId = "cred-1",
                })
                .Build(),
        };

        var sut = CreateSut(
            new SingleOperationRepository(operation),
            new SingleTeamRepository(team),
            new StubPaymentService(),
            paymentRepo,
            orchestrator,
            frendz: new SingleFrendzCredentialsRepository(cred),
            groups: new SingleGatewayCredentialsGroupRepository(group));

        var result = await sut.CreatePixChargeAsync(new CreatePixChargeRequest
        {
            OperationId = "op-1",
            OperatorId = "operator-1",
            Amount = 10m
        });

        Assert.True(result.IsSuccess);
        Assert.Equal("pix-group", result.Value!.PixCode);
    }

    [Fact]
    public async Task CreatePixChargeAsync_WithoutOperator_UsesOperationDefaultCredentials()
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

        var cred = new FrendzApiCredentials { Id = "cred-op-1", Name = "c", Token = "tok", StrawManId = "straw-1" };
        var paymentRepo = new StubPaymentRepository();

        var sut = CreateSut(
            new SingleOperationRepository(operation),
            new EmptyTeamRepository(),
            new StubPaymentService(),
            paymentRepo,
            frendz: new SingleFrendzCredentialsRepository(cred));

        var result = await sut.CreatePixChargeAsync(new CreatePixChargeRequest
        {
            OperationId = "op-1",
            Amount = 10m
        });

        Assert.True(result.IsSuccess);
        Assert.Equal("pix-code", result.Value!.PixCode);
        Assert.True(paymentRepo.WasUpdated);
    }

    [Fact]
    public async Task CreatePixChargeAsync_WhenAmountInvalid_ReturnsFailure()
    {
        var sut = CreateSut(
            new EmptyOperationRepository(),
            new EmptyTeamRepository(),
            new StubPaymentService(),
            new StubPaymentRepository());

        var result = await sut.CreatePixChargeAsync(new CreatePixChargeRequest
        {
            OperationId = "op-1",
            Amount = 0m,
        });

        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors, e => e.Code == PixPaymentErrorCodes.AmountInvalid);
    }

    private static ChargeService CreateSut(
        IOperationRepository operations,
        ITeamRepository teams,
        IPaymentService paymentService,
        IPaymentRepository paymentRepo,
        IGatewayOrchestrator? gatewayOrchestrator = null,
        IFrendzApiCredentialsRepository? frendz = null,
        ISigiloPayApiCredentialsRepository? sigiloPay = null,
        IWintechApiCredentialsRepository? wintech = null,
        IGatewayCredentialsGroupRepository? groups = null)
    {
        gatewayOrchestrator ??= new StubGatewayOrchestrator();
        frendz ??= new EmptyFrendzCredentialsRepository();
        sigiloPay ??= new EmptySigiloPayCredentialsRepository();
        wintech ??= new EmptyWintechCredentialsRepository();
        groups ??= new EmptyGatewayCredentialsGroupRepository();

        var credentialsResolver = new GatewayCredentialsResolver(
            operations,
            teams,
            frendz,
            sigiloPay,
            wintech,
            groups);

        return new ChargeService(
            credentialsResolver,
            paymentService,
            paymentRepo,
            PaymentTestDoubles.SplitCalculation(),
            gatewayOrchestrator);
    }

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

    private sealed class StubPaymentRepository : IPaymentRepository
    {
        public bool WasUpdated { get; private set; }

        public IAsyncQueryable<Payment> AsQueryable() =>
            new QueryableToAsyncQueryableAdapter<Payment>(Array.Empty<Payment>().AsQueryable());

        public Task<Payment> CreateAsync(Payment entity)
        {
            var persisted = string.IsNullOrWhiteSpace(entity.Id)
                ? PaymentTestFactory.Create(
                    operationId: entity.OperationId,
                    gateway: entity.Gateway,
                    gatewayPaymentId: entity.GatewayTransactionId,
                    amount: entity.Amount,
                    splits: entity.Splits,
                    status: entity.Status,
                    settlementStatus: entity.SettlementStatus,
                    distributionStatus: entity.DistributionStatus,
                    operatorId: entity.OperatorId,
                    strawManId: entity.StrawManId,
                    createdAt: entity.CreatedAt,
                    paidAt: entity.PaidAt,
                    refundedAt: entity.RefundedAt,
                    killedAt: entity.KilledAt,
                    killReason: entity.KillReason,
                    withdrawnAt: entity.WithdrawnAt,
                    distributedAt: entity.DistributedAt)
                : entity;

            return Task.FromResult(persisted);
        }
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

    private sealed class StubGatewayOrchestrator : IGatewayOrchestrator
    {
        public Func<TryCreatePixRequest, IResult<TryCreatePixResponse>>? OnTry { get; init; }

        public Task<IResult<TryCreatePixResponse>> TryCreatePixAsync(TryCreatePixRequest request)
        {
            if (OnTry is not null)
                return Task.FromResult(OnTry(request));

            var credentialId = request.Credentials.FirstOrDefault()?.CredentialId ?? string.Empty;
            IResult<TryCreatePixResponse> ok = Result.Create<TryCreatePixResponse>()
                .WithValue(new TryCreatePixResponse
                {
                    TransactionId = $"trx-{request.PaymentId}",
                    PixCode = "pix-code",
                    Gateway = request.Credentials.FirstOrDefault()?.Gateway ?? PaymentGateway.Frendz,
                    CredentialId = credentialId,
                })
                .Build();
            return Task.FromResult(ok);
        }
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

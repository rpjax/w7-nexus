using System.Linq.Expressions;
using Aidan.Core.Linq;
using Aidan.Core.Patterns;
using Aidan.Mongo.Linq;
using Nexus.Charges.Infrastructure;
using Xunit;
using Nexus.Legacy.Wintech.Application;
using Nexus.Legacy.SigiloPay.Application;
using Nexus.Legacy.Payments.Aggregates;
using Nexus.Legacy.Payments.Application;
using Nexus.Legacy.Payments.ErrorCodes;
using Nexus.Legacy.Frendz.Application;
using Nexus.Legacy.Charges.Application;
using Nexus.Legacy.Wintech.Application.Models;
using Nexus.Legacy.SigiloPay.Application.Models;
using Nexus.Legacy.Payments.Application.Models;
using Nexus.Legacy.Frendz.Application.Models;
using Nexus.Legacy.Charges.Application.Models;
using Nexus.Legacy.Database.Models;
using Nexus.Operations.Aggregates;
using Nexus.Operations.Application;

namespace Nexus.Tests.Charges;

public sealed class ChargeOrchestratorTests
{
    [Fact]
    public async Task CreatePixChargeAsync_WhenOperationMissing_ReturnsFailure()
    {
        var sut = new ChargeOrchestrator(
            new EmptyOperationRepository(),
            new EmptyTeamRepository(),
            new StubPaymentService(),
            new StubPaymentRepository(),
            new EmptyFrendzCredentialsRepository(),
            new StubChargeServiceFactory(new StubChargeService()),
            new EmptySigiloPayCredentialsRepository(),
            new StubSigiloPayChargeServiceFactory(new StubChargeService()),
            new EmptyWintechCredentialsRepository(),
            new StubWintechChargeServiceFactory(new StubChargeService()));

        var result = await sut.CreatePixChargeAsync(new CreatePixChargeRequest
        {
            OperationId = "missing",
            OperatorAccountId = "operator-1",
            Amount = 10m
        });

        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors, e => e.Code == PixPaymentErrorCodes.OperationNotFound);
    }

    [Fact]
    public async Task CreatePixChargeAsync_WhenChargeSucceeds_ReturnsPixChargeAndUpdatesPayment()
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
        var chargeService = new StubChargeService
        {
            OnCreate = r => Task.FromResult(new PixCharge
            {
                Id = r.PaymentId,
                Code = "pix-code"
            })
        };

        var sut = new ChargeOrchestrator(
            new SingleOperationRepository(operation),
            new SingleTeamRepository(team),
            new StubPaymentService(),
            paymentRepo,
            new SingleFrendzCredentialsRepository(cred),
            new StubChargeServiceFactory(chargeService),
            new EmptySigiloPayCredentialsRepository(),
            new StubSigiloPayChargeServiceFactory(chargeService),
            new EmptyWintechCredentialsRepository(),
            new StubWintechChargeServiceFactory(chargeService));

        var result = await sut.CreatePixChargeAsync(new CreatePixChargeRequest
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

    private sealed class StubChargeService : IChargeService
    {
        public Func<CreatePixChargeRequest, Task<PixCharge>>? OnCreate { get; init; }

        public Task<PixCharge> CreatePixChargeAsync(CreatePixChargeRequest request)
        {
            if (OnCreate is null)
                throw new InvalidOperationException();
            return OnCreate(request);
        }
    }

    private sealed class StubChargeServiceFactory : IFrendzChargeServiceFactory
    {
        private readonly IChargeService _service;

        public StubChargeServiceFactory(IChargeService service) => _service = service;

        public IChargeService Create(FrendzApiCredentials credentials) => _service;
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

    private sealed class StubSigiloPayChargeServiceFactory : ISigiloPayChargeServiceFactory
    {
        private readonly IChargeService _service;

        public StubSigiloPayChargeServiceFactory(IChargeService service) => _service = service;

        public IChargeService Create(SigiloPayApiCredentials credentials) => _service;
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

    private sealed class StubWintechChargeServiceFactory : IWintechChargeServiceFactory
    {
        private readonly IChargeService _service;

        public StubWintechChargeServiceFactory(IChargeService service) => _service = service;

        public IChargeService Create(WintechApiCredentials credentials) => _service;
    }
}

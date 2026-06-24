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
using Nexus.Operations.Application.Services;
using Nexus.Database.Models;
using Nexus.Gateways.Wintech.Application.Models;
using Nexus.Gateways.Wintech.Application.Services;
using Nexus.Gateways.Aggregates;
using Nexus.Gateways.Application.Services;
using Nexus.Gateways.Frendz.Application.Services;
using Nexus.Gateways.Application.Models;
using Nexus.Gateways.Frendz.Application.Models;
using Nexus.Gateways.SigiloPay.Application.Models;
using Nexus.Gateways.SigiloPay.Application.Services;
using Nexus.Payments.Aggregates;
using Nexus.Payments.Application.Services;
using Nexus.Payments.Application.Models;
using Nexus.Payments.Errors;
using Nexus.Tests.Payments;

namespace Nexus.Tests.Gateways;

public sealed class GatewayOrchestratorTests
{
    [Fact]
    public async Task CreateGatewayPixAsync_WhenOperationMissing_ReturnsFailure()
    {
        var sut = CreateSut(
            new EmptyOperationRepository(),
            new EmptyTeamRepository(),
            new StubPaymentService(),
            new StubPaymentRepository());

        var result = await sut.CreateGatewayPixAsync(new CreateGatewayPixRequest
        {
            OperationId = "missing",
            OperatorId = "operator-1",
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
        var gatewayPixService = new StubGatewayPixService
        {
            OnCreate = r => Task.FromResult(new GatewayPix
            {
                Id = r.PaymentId,
                Code = "pix-code"
            })
        };

        var sut = CreateSut(
            new SingleOperationRepository(operation),
            new SingleTeamRepository(team),
            new StubPaymentService(),
            paymentRepo,
            frendz: new SingleFrendzCredentialsRepository(cred),
            gatewayPixService: gatewayPixService);

        var result = await sut.CreateGatewayPixAsync(new CreateGatewayPixRequest
        {
            OperationId = "op-1",
            OperatorId = "operator-1",
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
        var gatewayPixService = new StubGatewayPixService
        {
            OnCreate = r => Task.FromResult(new GatewayPix
            {
                Id = r.PaymentId,
                Code = "pix-group"
            })
        };

        var sut = CreateSut(
            new SingleOperationRepository(operation),
            new SingleTeamRepository(team),
            new StubPaymentService(),
            paymentRepo,
            frendz: new SingleFrendzCredentialsRepository(cred),
            groups: new SingleGatewayCredentialsGroupRepository(group),
            gatewayPixService: gatewayPixService);

        var result = await sut.CreateGatewayPixAsync(new CreateGatewayPixRequest
        {
            OperationId = "op-1",
            OperatorId = "operator-1",
            Amount = 10m
        });

        Assert.True(result.IsSuccess);
        Assert.Equal("pix-group", result.Value!.Code);
    }

    [Fact]
    public async Task CreateGatewayPixAsync_WhenOperatorHasNoTeam_ReturnsTeamNotFound()
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

        var sut = CreateSut(
            new SingleOperationRepository(operation),
            new EmptyTeamRepository(),
            new StubPaymentService(),
            new StubPaymentRepository(),
            frendz: new SingleFrendzCredentialsRepository(cred));

        var result = await sut.CreateGatewayPixAsync(new CreateGatewayPixRequest
        {
            OperationId = "op-1",
            OperatorId = "operator-without-team",
            Amount = 10m
        });

        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors, e => e.Code == PixPaymentErrorCodes.TeamNotFound);
    }

    [Fact]
    public async Task CreateGatewayPixAsync_WithoutOperator_UsesOperationDefaultCredentials()
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
        var gatewayPixService = new StubGatewayPixService
        {
            OnCreate = r => Task.FromResult(new GatewayPix
            {
                Id = r.PaymentId,
                Code = "pix-operation-default"
            })
        };

        var sut = CreateSut(
            new SingleOperationRepository(operation),
            new EmptyTeamRepository(),
            new StubPaymentService(),
            paymentRepo,
            frendz: new SingleFrendzCredentialsRepository(cred),
            gatewayPixService: gatewayPixService);

        var result = await sut.CreateGatewayPixAsync(new CreateGatewayPixRequest
        {
            OperationId = "op-1",
            Amount = 10m
        });

        Assert.True(result.IsSuccess);
        Assert.Equal("pix-operation-default", result.Value!.Code);
        Assert.True(paymentRepo.WasUpdated);
    }

    [Fact]
    public async Task CreateGatewayPixAsync_WhenPerStrawmanWithEmptyStrawManIds_ReturnsNoGatewayServicesAvailable()
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

        var cred = new FrendzApiCredentials { Id = "cred-1", Name = "c", Token = "tok", StrawManId = "straw-1" };

        var sut = CreateSut(
            new SingleOperationRepository(operation),
            new EmptyTeamRepository(),
            new StubPaymentService(),
            new StubPaymentRepository(),
            frendz: new SingleFrendzCredentialsRepository(cred));

        var result = await sut.CreateGatewayPixAsync(new CreateGatewayPixRequest
        {
            OperationId = "op-1",
            Amount = 10m
        });

        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors, e => e.Code == PixPaymentErrorCodes.NoGatewayServicesAvailable);
    }

    [Fact]
    public async Task CreateGatewayPixAsync_WhenManualCredentialHasNoStrawManOwner_ReturnsNoGatewayServicesAvailable()
    {
        var operation = new Operation(
            "op-1",
            "N",
            "D",
            Array.Empty<string>(),
            Array.Empty<string>(),
            GatewaySelectionStrategy.Manual,
            new[] { "cred-1" },
            Array.Empty<string>(),
            DateTime.UtcNow,
            DateTime.UtcNow);

        var cred = new FrendzApiCredentials { Id = "cred-1", Name = "c", Token = "tok", StrawManId = null };

        var sut = CreateSut(
            new SingleOperationRepository(operation),
            new EmptyTeamRepository(),
            new StubPaymentService(),
            new StubPaymentRepository(),
            frendz: new SingleFrendzCredentialsRepository(cred));

        var result = await sut.CreateGatewayPixAsync(new CreateGatewayPixRequest
        {
            OperationId = "op-1",
            Amount = 10m
        });

        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors, e => e.Code == PixPaymentErrorCodes.NoGatewayServicesAvailable);
    }

    [Fact]
    public async Task CreateGatewayPixAsync_WhenPerStrawman_OnlyUsesCredentialsFromLinkedStrawMen()
    {
        var operation = new Operation(
            "op-1",
            "N",
            "D",
            Array.Empty<string>(),
            new[] { "straw-1" },
            GatewaySelectionStrategy.PerStrawman,
            Array.Empty<string>(),
            Array.Empty<string>(),
            DateTime.UtcNow,
            DateTime.UtcNow);

        var linkedCred = new FrendzApiCredentials { Id = "cred-linked", Name = "linked", Token = "tok", StrawManId = "straw-1" };
        var unlinkedCred = new FrendzApiCredentials { Id = "cred-unlinked", Name = "unlinked", Token = "tok2", StrawManId = "straw-2" };
        var genericCred = new FrendzApiCredentials { Id = "cred-generic", Name = "generic", Token = "tok3", StrawManId = null };

        var gatewayPixService = new StubGatewayPixService
        {
            OnCreate = r => Task.FromResult(new GatewayPix
            {
                Id = r.PaymentId,
                Code = "pix-linked"
            })
        };

        var sut = CreateSut(
            new SingleOperationRepository(operation),
            new EmptyTeamRepository(),
            new StubPaymentService(),
            new StubPaymentRepository(),
            frendz: new MultiFrendzCredentialsRepository(linkedCred, unlinkedCred, genericCred),
            gatewayPixService: gatewayPixService);

        var result = await sut.CreateGatewayPixAsync(new CreateGatewayPixRequest
        {
            OperationId = "op-1",
            Amount = 10m
        });

        Assert.True(result.IsSuccess);
        Assert.Equal("pix-linked", result.Value!.Code);
    }

    [Fact]
    public async Task CreateGatewayPixAsync_WithOperator_UsesTeamCredentialScopeOverOperation()
    {
        var operation = new Operation(
            "op-1",
            "N",
            "D",
            Array.Empty<string>(),
            Array.Empty<string>(),
            GatewaySelectionStrategy.Manual,
            new[] { "cred-op" },
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
            GatewaySelectionStrategy.Manual,
            new[] { "cred-team" },
            Array.Empty<string>(),
            Array.Empty<OperatorProfitShareRuleRecord>(),
            DateTime.UtcNow,
            DateTime.UtcNow);

        var operationCred = new FrendzApiCredentials { Id = "cred-op", Name = "op", Token = "tok-op", StrawManId = "straw-op" };
        var teamCred = new FrendzApiCredentials { Id = "cred-team", Name = "team", Token = "tok-team", StrawManId = "straw-team" };

        var gatewayPixService = new TrackingGatewayPixService();
        var paymentRepo = new StubPaymentRepository();

        var sut = CreateSut(
            new SingleOperationRepository(operation),
            new SingleTeamRepository(team),
            new StubPaymentService(),
            paymentRepo,
            frendz: new MultiFrendzCredentialsRepository(operationCred, teamCred),
            gatewayPixService: gatewayPixService);

        var result = await sut.CreateGatewayPixAsync(new CreateGatewayPixRequest
        {
            OperationId = "op-1",
            OperatorId = "operator-1",
            Amount = 10m
        });

        Assert.True(result.IsSuccess);
        Assert.Equal("straw-team", gatewayPixService.LastStrawManId);
    }

    private static GatewayOrchestrator CreateSut(
        IOperationRepository operations,
        ITeamRepository teams,
        IPaymentService paymentService,
        IPaymentRepository paymentRepo,
        IFrendzApiCredentialsRepository? frendz = null,
        ISigiloPayApiCredentialsRepository? sigiloPay = null,
        IWintechApiCredentialsRepository? wintech = null,
        IGatewayCredentialsGroupRepository? groups = null,
        IGatewayPixService? gatewayPixService = null)
    {
        gatewayPixService ??= new StubGatewayPixService();
        frendz ??= new EmptyFrendzCredentialsRepository();
        sigiloPay ??= new EmptySigiloPayCredentialsRepository();
        wintech ??= new EmptyWintechCredentialsRepository();
        groups ??= new EmptyGatewayCredentialsGroupRepository();

        var resolver = new GatewayCredentialProviderResolver(
            frendz,
            new StubGatewayPixServiceFactory(gatewayPixService),
            sigiloPay,
            new StubSigiloPayGatewayPixServiceFactory(gatewayPixService),
            wintech,
            new StubWintechGatewayPixServiceFactory(gatewayPixService),
            groups);

        return new GatewayOrchestrator(operations, teams, paymentService, paymentRepo, resolver);
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
            new MongoAsyncQueryable<Payment>(Array.Empty<Payment>().AsQueryable());

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
                    operatorId: entity.OperatorId,
                    strawManId: entity.StrawManId,
                    createdAt: entity.CreatedAt,
                    paidAt: entity.PaidAt,
                    refundedAt: entity.RefundedAt,
                    killedAt: entity.KilledAt,
                    killReason: entity.KillReason,
                    withdrawnAt: entity.WithdrawnAt)
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

    private sealed class MultiFrendzCredentialsRepository : IFrendzApiCredentialsRepository
    {
        private readonly FrendzApiCredentials[] _credentials;

        public MultiFrendzCredentialsRepository(params FrendzApiCredentials[] credentials) =>
            _credentials = credentials;

        public IAsyncQueryable<FrendzApiCredentials> AsQueryable() =>
            new MongoAsyncQueryable<FrendzApiCredentials>(_credentials.AsQueryable());

        public Task<FrendzApiCredentials> CreateAsync(FrendzApiCredentials entity) => throw new NotSupportedException();
        async Task IRepository<FrendzApiCredentials>.CreateAsync(FrendzApiCredentials entity) { await CreateAsync(entity); }
        public Task CreateAsync(IEnumerable<FrendzApiCredentials> entities) => throw new NotSupportedException();
        public Task DeleteAsync(FrendzApiCredentials entity) => throw new NotSupportedException();
        public Task<long> DeleteAsync(Expression<Func<FrendzApiCredentials, bool>> predicate) => throw new NotSupportedException();
        public Task UpdateAsync(FrendzApiCredentials entity) => throw new NotSupportedException();
        public Task<long> UpdateAsync(Expression expression) => throw new NotSupportedException();
    }

    private sealed class TrackingGatewayPixService : IGatewayPixService
    {
        public string? LastStrawManId { get; private set; }

        public Task<GatewayPix> CreateGatewayPixAsync(CreateGatewayPixRequest request)
        {
            LastStrawManId = request.StrawManId;
            return Task.FromResult(new GatewayPix
            {
                Id = request.PaymentId,
                Code = "pix-tracked"
            });
        }
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

        public Task<SigiloPayApiCredentials> CreateAsync(SigiloPayApiCredentials entity) => throw new NotSupportedException();
        async Task IRepository<SigiloPayApiCredentials>.CreateAsync(SigiloPayApiCredentials entity) { await CreateAsync(entity); }
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

        public Task<WintechApiCredentials> CreateAsync(WintechApiCredentials entity) => throw new NotSupportedException();
        async Task IRepository<WintechApiCredentials>.CreateAsync(WintechApiCredentials entity) { await CreateAsync(entity); }
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

        public Task<GatewayCredentialsGroup> CreateAsync(GatewayCredentialsGroup entity) => throw new NotSupportedException();
        async Task IRepository<GatewayCredentialsGroup>.CreateAsync(GatewayCredentialsGroup entity) { await CreateAsync(entity); }
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

        public Task<GatewayCredentialsGroup> CreateAsync(GatewayCredentialsGroup entity) => throw new NotSupportedException();
        async Task IRepository<GatewayCredentialsGroup>.CreateAsync(GatewayCredentialsGroup entity) { await CreateAsync(entity); }
        public Task CreateAsync(IEnumerable<GatewayCredentialsGroup> entities) => throw new NotSupportedException();
        public Task DeleteAsync(GatewayCredentialsGroup entity) => throw new NotSupportedException();
        public Task<long> DeleteAsync(Expression<Func<GatewayCredentialsGroup, bool>> predicate) => throw new NotSupportedException();
        public Task UpdateAsync(GatewayCredentialsGroup entity) => throw new NotSupportedException();
        public Task<long> UpdateAsync(Expression expression) => throw new NotSupportedException();
    }
}


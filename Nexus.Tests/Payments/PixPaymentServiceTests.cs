using System.Linq.Expressions;
using Aidan.Core.Linq;
using Aidan.Core.Patterns;
using Nexus.Payments.Application.Contracts;
using Nexus.Operations.Application.Contracts;
using Aidan.Mongo.Linq;
using Nexus.Payments.Application.Models;
using Xunit;
using Nexus.Accounts.Aggregates;
using Nexus.Authorization;
using Nexus.Operations.Aggregates;
using Nexus.Operations.Application.Services;
using Nexus.Payments.Aggregates;
using Nexus.Payments.Application.Services;
using Nexus.Accounts.Application.Contracts;
using Nexus.Payments.Errors;

namespace Nexus.Tests.Payments;

public sealed class PixPaymentServiceTests
{
    private sealed class StubPixPaymentRepository : IPaymentRepository
    {
        public IAsyncQueryable<Payment> AsQueryable()
            => new MongoAsyncQueryable<Payment>(Array.Empty<Payment>().AsQueryable());

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

        async Task IRepository<Payment>.CreateAsync(Payment entity)
        {
            await CreateAsync(entity);
        }

        public Task CreateAsync(IEnumerable<Payment> entities) => Task.CompletedTask;

        public Task DeleteAsync(Payment entity) => Task.CompletedTask;

        public Task<long> DeleteAsync(System.Linq.Expressions.Expression<Func<Payment, bool>> predicate) =>
            Task.FromResult(0L);

        public Task UpdateAsync(Payment entity) => Task.CompletedTask;

        public Task<long> UpdateAsync(System.Linq.Expressions.Expression expression) =>
            Task.FromResult(0L);
    }

    private sealed class StubOperationRepository : IOperationRepository
    {
        private readonly Operation[] _operations;

        public StubOperationRepository(params Operation[] operations)
        {
            _operations = operations;
        }

        public StubOperationRepository(params string[] operationIds)
        {
            _operations = operationIds
                .Select(id => new Operation(
                    id,
                    $"Operation {id}",
                    "desc",
                    Array.Empty<string>(),
                    Array.Empty<string>(),
                    GatewaySelectionStrategy.PerStrawman,
                    Array.Empty<string>(),
                    Array.Empty<string>(),
                    DateTime.UtcNow,
                    DateTime.UtcNow))
                .ToArray();
        }

        public IAsyncQueryable<Operation> AsQueryable()
            => new MongoAsyncQueryable<Operation>(_operations.AsQueryable());

        public Task<Operation> CreateAsync(Operation entity) => Task.FromResult(entity);

        async Task IRepository<Operation>.CreateAsync(Operation entity)
        {
            await CreateAsync(entity);
        }

        public Task CreateAsync(IEnumerable<Operation> entities) => Task.CompletedTask;
        public Task DeleteAsync(Operation entity) => Task.CompletedTask;
        public Task<long> DeleteAsync(System.Linq.Expressions.Expression<Func<Operation, bool>> predicate) => Task.FromResult(0L);
        public Task UpdateAsync(Operation entity) => Task.CompletedTask;
        public Task<long> UpdateAsync(System.Linq.Expressions.Expression expression) => Task.FromResult(0L);
    }

    private sealed class StubTeamRepository : ITeamRepository
    {
        private readonly Team[] _teams;

        public StubTeamRepository(params Team[] teams) => _teams = teams;

        public IAsyncQueryable<Team> AsQueryable()
            => new MongoAsyncQueryable<Team>(_teams.AsQueryable());

        public Task<Team> CreateAsync(Team entity) => Task.FromResult(entity);

        async Task IRepository<Team>.CreateAsync(Team entity)
        {
            await CreateAsync(entity);
        }

        public Task CreateAsync(IEnumerable<Team> entities) => Task.CompletedTask;
        public Task DeleteAsync(Team entity) => Task.CompletedTask;
        public Task<long> DeleteAsync(System.Linq.Expressions.Expression<Func<Team, bool>> predicate) => Task.FromResult(0L);
        public Task UpdateAsync(Team entity) => Task.CompletedTask;
        public Task<long> UpdateAsync(System.Linq.Expressions.Expression expression) => Task.FromResult(0L);
    }

    private sealed class StubAccountRepository : IAccountRepository
    {
        private readonly Account[] _accounts;

        public StubAccountRepository(params string[] accountIds)
        {
            _accounts = accountIds
                .Select(id => new Account(
                    id,
                    $"user-{id}",
                    "hash",
                    id.StartsWith("admin", StringComparison.Ordinal)
                        ? new[] { Roles.Administrator }
                        : id.StartsWith("sm", StringComparison.Ordinal) || id.StartsWith("straw", StringComparison.Ordinal)
                            ? new[] { Roles.StrawMan }
                            : Array.Empty<string>(),
                    Array.Empty<string>(),
                    DateTime.UtcNow,
                    DateTime.UtcNow))
                .ToArray();
        }

        public IAsyncQueryable<Account> AsQueryable()
            => new MongoAsyncQueryable<Account>(_accounts.AsQueryable());

        public Task<Account> CreateAsync(Account entity) => Task.FromResult(entity);

        async Task IRepository<Account>.CreateAsync(Account entity)
        {
            await CreateAsync(entity);
        }

        public Task CreateAsync(IEnumerable<Account> entities) => Task.CompletedTask;
        public Task DeleteAsync(Account entity) => Task.CompletedTask;
        public Task<long> DeleteAsync(System.Linq.Expressions.Expression<Func<Account, bool>> predicate) => Task.FromResult(0L);
        public Task UpdateAsync(Account entity) => Task.CompletedTask;
        public Task<long> UpdateAsync(System.Linq.Expressions.Expression expression) => Task.FromResult(0L);
    }

    private static PaymentService CreateSut(
        StubAccountRepository accounts,
        StubPixPaymentRepository payments,
        StubOperationRepository operations,
        StubTeamRepository? teams = null,
        PaymentSplitCalculationService? splitCalculation = null) =>
        new(
            accounts,
            payments,
            operations,
            teams ?? new StubTeamRepository(),
            splitCalculation ?? PaymentTestDoubles.SplitCalculation());

    [Fact]
    public async Task CreatePaymentAsync_GatewayNone_ReturnsGatewayInvalid()
    {
        var sut = CreateSut(new StubAccountRepository(), new StubPixPaymentRepository(), new StubOperationRepository("op-1"));

        var result = await sut.CreatePaymentAsync(new CreatePaymentRequest
        {
            OperationId = "op-1",
            OperatorId = null,
            StrawManId = string.Empty,
            Gateway = PaymentGateway.None,
            Amount = 10m,
            GatewayPaymentId = "gw-1"
        });

        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors, e => e.Code == PixPaymentErrorCodes.GatewayInvalid);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-0.01)]
    public async Task CreatePaymentAsync_InvalidAmount_ReturnsAmountInvalid(decimal amount)
    {
        var sut = CreateSut(new StubAccountRepository(), new StubPixPaymentRepository(), new StubOperationRepository("op-1"));

        var result = await sut.CreatePaymentAsync(new CreatePaymentRequest
        {
            OperationId = "op-1",
            OperatorId = null,
            StrawManId = string.Empty,
            Gateway = PaymentGateway.FusionPay,
            Amount = amount,
            GatewayPaymentId = "gw-1"
        });

        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors, e => e.Code == PixPaymentErrorCodes.AmountInvalid);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task CreatePaymentAsync_InvalidGatewayPaymentId_ReturnsGatewayPaymentIdInvalid(string? gatewayPaymentId)
    {
        var sut = CreateSut(new StubAccountRepository(), new StubPixPaymentRepository(), new StubOperationRepository("op-1"));

        var result = await sut.CreatePaymentAsync(new CreatePaymentRequest
        {
            OperationId = "op-1",
            OperatorId = null,
            StrawManId = string.Empty,
            Gateway = PaymentGateway.Frendz,
            Amount = 10m,
            GatewayPaymentId = gatewayPaymentId
        });

        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors, e => e.Code == PixPaymentErrorCodes.GatewayPaymentIdInvalid);
    }

    [Fact]
    public async Task CreatePaymentAsync_OperatorProvidedAsEmpty_ReturnsOperatorInvalid()
    {
        var sut = CreateSut(new StubAccountRepository(), new StubPixPaymentRepository(), new StubOperationRepository("op-1"));

        var result = await sut.CreatePaymentAsync(new CreatePaymentRequest
        {
            OperationId = "op-1",
            OperatorId = "  ",
            StrawManId = string.Empty,
            Gateway = PaymentGateway.FusionPay,
            Amount = 10m,
            GatewayPaymentId = "gw-1"
        });

        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors, e => e.Code == PixPaymentErrorCodes.OperatorInvalid);
    }

    [Fact]
    public async Task CreatePaymentAsync_OperatorWithoutMatchingTeam_ReturnsTeamNotFound()
    {
        var sut = CreateSut(
            new StubAccountRepository("op-42", "straw-1"),
            new StubPixPaymentRepository(),
            new StubOperationRepository("op-1"));

        var result = await sut.CreatePaymentAsync(new CreatePaymentRequest
        {
            OperationId = "op-1",
            OperatorId = "op-42",
            Gateway = PaymentGateway.FusionPay,
            Amount = 10m,
            GatewayPaymentId = "gw-1"
        });

        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors, e => e.Code == PixPaymentErrorCodes.TeamNotFound);
    }

    [Fact]
    public async Task CreatePaymentAsync_WithoutOperatorAndNoRecipients_ReturnsProfitShareRecipientsNotFound()
    {
        var sut = CreateSut(new StubAccountRepository(), new StubPixPaymentRepository(), new StubOperationRepository("op-1"));

        var result = await sut.CreatePaymentAsync(new CreatePaymentRequest
        {
            OperationId = "op-1",
            Gateway = PaymentGateway.FusionPay,
            Amount = 10m,
            GatewayPaymentId = "gw-1"
        });

        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors, e => e.Code == PixPaymentErrorCodes.ProfitShareRecipientsNotFound);
    }

    [Fact]
    public async Task CreatePaymentAsync_WithoutOperator_SplitsAmongOperationAdministrators()
    {
        var operation = new Operation(
            "op-1",
            "Operation",
            "desc",
            new[] { "admin-1", "admin-2" },
            Array.Empty<string>(),
            GatewaySelectionStrategy.PerStrawman,
            Array.Empty<string>(),
            Array.Empty<string>(),
            DateTime.UtcNow,
            DateTime.UtcNow);

        var sut = CreateSut(
            new StubAccountRepository(),
            new StubPixPaymentRepository(),
            new StubOperationRepository(operation));

        var result = await sut.CreatePaymentAsync(new CreatePaymentRequest
        {
            OperationId = "op-1",
            Gateway = PaymentGateway.FusionPay,
            Amount = 100m,
            GatewayPaymentId = "gw-1"
        });

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value!.Splits.Count);
        Assert.Equal(100m, result.Value.Splits.Sum(split => split.Amount));
    }

    [Fact]
    public async Task CreatePaymentAsync_WithoutOperator_FallsBackToSystemAdministrators()
    {
        var operation = new Operation(
            "op-1",
            "Operation",
            "desc",
            Array.Empty<string>(),
            Array.Empty<string>(),
            GatewaySelectionStrategy.PerStrawman,
            Array.Empty<string>(),
            Array.Empty<string>(),
            DateTime.UtcNow,
            DateTime.UtcNow);

        var sut = CreateSut(
            new StubAccountRepository("admin-1", "admin-2"),
            new StubPixPaymentRepository(),
            new StubOperationRepository(operation));

        var result = await sut.CreatePaymentAsync(new CreatePaymentRequest
        {
            OperationId = "op-1",
            Gateway = PaymentGateway.FusionPay,
            Amount = 100m,
            GatewayPaymentId = "gw-1"
        });

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value!.Splits.Count);
        Assert.Equal(100m, result.Value.Splits.Sum(split => split.Amount));
    }

    [Fact]
    public async Task CreatePaymentAsync_ValidMinimalWithoutStrawMan_CreatesPendingPayment()
    {
        var operation = new Operation(
            "op-1",
            "Operation",
            "desc",
            new[] { "admin-1" },
            Array.Empty<string>(),
            GatewaySelectionStrategy.PerStrawman,
            Array.Empty<string>(),
            Array.Empty<string>(),
            DateTime.UtcNow,
            DateTime.UtcNow);

        var sut = CreateSut(
            new StubAccountRepository("admin-1"),
            new StubPixPaymentRepository(),
            new StubOperationRepository(operation));

        var result = await sut.CreatePaymentAsync(new CreatePaymentRequest
        {
            OperationId = "op-1",
            OperatorId = null,
            Gateway = PaymentGateway.FusionPay,
            Amount = 19.90m,
            GatewayPaymentId = "ext-pay-99"
        });

        Assert.True(result.IsSuccess);
        var payment = result.Value!;
        Assert.Equal(PaymentGateway.FusionPay, payment.Gateway);
        Assert.Equal("ext-pay-99", payment.GatewayTransactionId);
        Assert.Equal("op-1", payment.OperationId);
        Assert.Equal(19.90m, payment.Amount);
        Assert.Equal(PaymentStatus.Pending, payment.Status);
        Assert.Single(payment.Splits);
        Assert.Null(payment.OperatorId);
        Assert.Equal(string.Empty, payment.StrawManId);
        Assert.False(string.IsNullOrEmpty(payment.Id));
    }

    [Fact]
    public async Task CreatePaymentAsync_OperatorNotFound_ReturnsOperatorAccountNotFound()
    {
        var sut = CreateSut(new StubAccountRepository("straw-1"), new StubPixPaymentRepository(), new StubOperationRepository("op-1"));

        var result = await sut.CreatePaymentAsync(new CreatePaymentRequest
        {
            OperationId = "op-1",
            OperatorId = "missing-operator",
            StrawManId = "straw-1",
            Gateway = PaymentGateway.Frendz,
            Amount = 5m,
            GatewayPaymentId = "gw-1"
        });

        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors, e => e.Code == PixPaymentErrorCodes.OperatorNotFound);
    }

    [Fact]
    public async Task CreatePaymentAsync_StrawManNotFound_ReturnsStrawManAccountNotFound()
    {
        var sut = CreateSut(new StubAccountRepository("op-1"), new StubPixPaymentRepository(), new StubOperationRepository("op-1"));

        var result = await sut.CreatePaymentAsync(new CreatePaymentRequest
        {
            OperationId = "op-1",
            OperatorId = null,
            StrawManId = "missing-straw",
            Gateway = PaymentGateway.FusionPay,
            Amount = 5m,
            GatewayPaymentId = "gw-1"
        });

        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors, e => e.Code == PixPaymentErrorCodes.StrawManNotFound);
    }

    [Fact]
    public async Task CreatePaymentAsync_OperatorExists_SnapshotsProfitShareAndBindsOperator()
    {
        var team = TeamTestFactory.WithOperatorProfitShare("team-1", "op-1", "op-42", "straw-1", ("op-42", 100m));
        var sut = CreateSut(
            new StubAccountRepository("op-42", "straw-1"),
            new StubPixPaymentRepository(),
            new StubOperationRepository("op-1"),
            new StubTeamRepository(team));

        var result = await sut.CreatePaymentAsync(new CreatePaymentRequest
        {
            OperationId = "op-1",
            OperatorId = "op-42",
            StrawManId = "straw-1",
            Gateway = PaymentGateway.SigiloPay,
            Amount = 100m,
            GatewayPaymentId = "gw-abc"
        });

        Assert.True(result.IsSuccess);
        Assert.Equal("op-42", result.Value!.OperatorId);
        Assert.Equal("straw-1", result.Value.StrawManId);
        Assert.Single(result.Value.Splits);
        Assert.Equal(100m, result.Value.Splits.Sum(split => split.Amount));
    }

    [Fact]
    public async Task CreatePaymentAsync_StrawManExists_BindsStrawManWhenProvided()
    {
        var operation = new Operation(
            "op-1",
            "Operation",
            "desc",
            new[] { "admin-1" },
            Array.Empty<string>(),
            GatewaySelectionStrategy.PerStrawman,
            Array.Empty<string>(),
            Array.Empty<string>(),
            DateTime.UtcNow,
            DateTime.UtcNow);

        var sut = CreateSut(
            new StubAccountRepository("sm-7", "admin-1"),
            new StubPixPaymentRepository(),
            new StubOperationRepository(operation));

        var result = await sut.CreatePaymentAsync(new CreatePaymentRequest
        {
            OperationId = "op-1",
            OperatorId = null,
            StrawManId = "sm-7",
            Gateway = PaymentGateway.FusionPay,
            Amount = 2m,
            GatewayPaymentId = "gw-x"
        });

        Assert.True(result.IsSuccess);
        Assert.Equal("sm-7", result.Value!.StrawManId);
        Assert.Null(result.Value.OperatorId);
    }

    [Fact]
    public async Task CreatePaymentAsync_OperatorAndStrawMan_BindsBothAndSnapshotsSplit()
    {
        var team = TeamTestFactory.WithOperatorProfitShare("team-1", "op-1", "op", "sm", ("op", 60m), ("partner", 40m));
        var sut = CreateSut(
            new StubAccountRepository("op", "sm"),
            new StubPixPaymentRepository(),
            new StubOperationRepository("op-1"),
            new StubTeamRepository(team));

        var result = await sut.CreatePaymentAsync(new CreatePaymentRequest
        {
            OperationId = "op-1",
            OperatorId = "op",
            StrawManId = "sm",
            Gateway = PaymentGateway.Frendz,
            Amount = 3m,
            GatewayPaymentId = "gw-dual"
        });

        Assert.True(result.IsSuccess);
        Assert.Equal("op", result.Value!.OperatorId);
        Assert.Equal("sm", result.Value.StrawManId);
        Assert.Equal(2, result.Value.Splits.Count);
        Assert.Equal(3m, result.Value.Splits.Sum(split => split.Amount));
    }

    [Fact]
    public async Task CreatePaymentAsync_WithStrawManFee_IncludesStrawManSplit()
    {
        var team = TeamTestFactory.WithOperatorProfitShare(
            "team-1",
            "op-1",
            "op-42",
            "straw-1",
            ("op-42", 50m),
            ("partner-1", 50m));
        var sut = CreateSut(
            new StubAccountRepository("op-42", "partner-1", "straw-1"),
            new StubPixPaymentRepository(),
            new StubOperationRepository("op-1"),
            new StubTeamRepository(team),
            PaymentTestDoubles.SplitCalculation(new Dictionary<string, decimal> { ["straw-1"] = 10m }));

        var result = await sut.CreatePaymentAsync(new CreatePaymentRequest
        {
            OperationId = "op-1",
            OperatorId = "op-42",
            StrawManId = "straw-1",
            Gateway = PaymentGateway.SigiloPay,
            Amount = 100m,
            GatewayPaymentId = "gw-fee",
        });

        Assert.True(result.IsSuccess);
        Assert.Equal(3, result.Value!.Splits.Count);
        Assert.Equal(100m, result.Value.Splits.Sum(split => split.Amount));

        var strawSplit = result.Value.Splits.Single(s => s.AccountId == "straw-1");
        Assert.Equal(PaymentSplitKind.StrawManFee, strawSplit.SplitKind);
        Assert.Equal(10m, strawSplit.Percentage);
        Assert.Equal(10m, strawSplit.Amount);

        var operatorSplit = result.Value.Splits.Single(s => s.AccountId == "op-42");
        Assert.Equal(45m, operatorSplit.Percentage);
        Assert.Equal(45m, operatorSplit.Amount);
    }

    [Fact]
    public async Task CreatePaymentAsync_MultipleValidationErrors_AccumulatesErrors()
    {
        var sut = CreateSut(new StubAccountRepository(), new StubPixPaymentRepository(), new StubOperationRepository("op-1"));

        var result = await sut.CreatePaymentAsync(new CreatePaymentRequest
        {
            OperationId = "op-1",
            OperatorId = " ",
            StrawManId = " ",
            Gateway = PaymentGateway.FusionPay,
            Amount = 0m,
            GatewayPaymentId = ""
        });

        Assert.True(result.IsFailure);
        Assert.True(result.Errors.Count() >= 2);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task CreatePaymentAsync_InvalidOperationId_ReturnsOperationIdInvalid(string? operationId)
    {
        var sut = CreateSut(new StubAccountRepository(), new StubPixPaymentRepository(), new StubOperationRepository("op-1"));

        var result = await sut.CreatePaymentAsync(new CreatePaymentRequest
        {
            OperationId = operationId,
            Gateway = PaymentGateway.FusionPay,
            Amount = 10m,
            GatewayPaymentId = "gw-1"
        });

        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors, e => e.Code == PixPaymentErrorCodes.OperationIdInvalid);
    }

    [Fact]
    public async Task CreatePaymentAsync_OperationNotFound_ReturnsOperationNotFound()
    {
        var sut = CreateSut(new StubAccountRepository("straw-1"), new StubPixPaymentRepository(), new StubOperationRepository("op-1"));

        var result = await sut.CreatePaymentAsync(new CreatePaymentRequest
        {
            OperationId = "missing-operation",
            StrawManId = "straw-1",
            Gateway = PaymentGateway.FusionPay,
            Amount = 10m,
            GatewayPaymentId = "gw-1"
        });

        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors, e => e.Code == PixPaymentErrorCodes.OperationNotFound);
    }
}

using Nexus.Payments.Aggregates;
using Nexus.Withdrawals.Aggregates;
using Nexus.Withdrawals.Application.Contracts;
using Nexus.Withdrawals.Application.Services;
using Nexus.Withdrawals.Errors;
using Xunit;

namespace Nexus.Tests.Withdrawals;

public sealed class WithdrawalServiceTests
{
    [Fact]
    public async Task CreateWithdrawal_RejectsBankAccountFromAnotherStrawMan()
    {
        var context = WithdrawalServiceTestContext.Create();
        context.SeedOperation(strawManIds: ["straw-1"]);
        context.SeedStrawMan("straw-1");
        context.SeedStrawMan("straw-2");

        var bankAccount = WithdrawalTestFactory.CreateBankAccount(strawManAccountId: "straw-2");
        context.BankAccounts.Seed(bankAccount);

        var payment = context.SeedPaidPayment(amount: 100m);
        var sut = context.CreateService();

        var result = await sut.CreateWithdrawalAsync(new CreateWithdrawalRequest
        {
            OperationId = WithdrawalServiceTestContext.OperationId,
            Type = WithdrawalType.Pix,
            StrawManAccountId = "straw-1",
            BankAccountId = bankAccount.Id,
            PaymentIds = new[] { payment.Id },
        });

        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors, e => e.Code == WithdrawalErrorCodes.BankAccountMismatch);
    }

    [Fact]
    public async Task CreateWithdrawal_RejectsPaymentFromAnotherOperation()
    {
        var context = WithdrawalServiceTestContext.Create();
        context.SeedOperation(strawManIds: ["straw-1"]);
        context.SeedStrawMan("straw-1");

        var bankAccount = WithdrawalTestFactory.CreateBankAccount(strawManAccountId: "straw-1");
        context.BankAccounts.Seed(bankAccount);

        var payment = context.SeedPaidPayment(operationId: "other-operation", amount: 100m);
        var sut = context.CreateService();

        var result = await sut.CreateWithdrawalAsync(new CreateWithdrawalRequest
        {
            OperationId = WithdrawalServiceTestContext.OperationId,
            Type = WithdrawalType.Pix,
            StrawManAccountId = "straw-1",
            BankAccountId = bankAccount.Id,
            PaymentIds = new[] { payment.Id },
        });

        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors, e => e.Code == WithdrawalErrorCodes.PaymentOperationMismatch);
    }

    [Fact]
    public async Task CreateWithdrawal_RejectsStrawManNotOnOperation()
    {
        var context = WithdrawalServiceTestContext.Create();
        context.SeedOperation(strawManIds: ["straw-1"]);
        context.SeedStrawMan("straw-1");
        context.SeedStrawMan("straw-2");

        var bankAccount = WithdrawalTestFactory.CreateBankAccount(strawManAccountId: "straw-2");
        context.BankAccounts.Seed(bankAccount);

        var payment = context.SeedPaidPayment(amount: 100m);
        var sut = context.CreateService();

        var result = await sut.CreateWithdrawalAsync(new CreateWithdrawalRequest
        {
            OperationId = WithdrawalServiceTestContext.OperationId,
            Type = WithdrawalType.Pix,
            StrawManAccountId = "straw-2",
            BankAccountId = bankAccount.Id,
            PaymentIds = new[] { payment.Id },
        });

        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors, e => e.Code == WithdrawalErrorCodes.StrawManNotOnOperation);
    }

    [Fact]
    public async Task CreateWithdrawal_RejectsAlreadyWithdrawnPayment()
    {
        var context = WithdrawalServiceTestContext.Create();
        context.SeedOperation(strawManIds: ["straw-1"]);
        context.SeedStrawMan("straw-1");

        var bankAccount = WithdrawalTestFactory.CreateBankAccount(strawManAccountId: "straw-1");
        context.BankAccounts.Seed(bankAccount);

        var payment = context.SeedPaidPayment(
            amount: 100m,
            settlementStatus: PaymentSettlementStatus.Withdrawn,
            withdrawnAt: DateTime.UtcNow);
        var sut = context.CreateService();

        var result = await sut.CreateWithdrawalAsync(new CreateWithdrawalRequest
        {
            OperationId = WithdrawalServiceTestContext.OperationId,
            Type = WithdrawalType.Pix,
            StrawManAccountId = "straw-1",
            BankAccountId = bankAccount.Id,
            PaymentIds = new[] { payment.Id },
        });

        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors, e => e.Code == WithdrawalErrorCodes.PaymentAlreadyWithdrawn);
    }
}

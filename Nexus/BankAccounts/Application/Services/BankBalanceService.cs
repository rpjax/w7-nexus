using Aidan.Core.Errors;
using Aidan.Core.Linq.Extensions;
using Aidan.Core.Patterns;
using Nexus.BankAccounts.Aggregates;
using Nexus.BankAccounts.Application.Contracts;
using Nexus.BankAccounts.Errors;

namespace Nexus.BankAccounts.Application.Services;

public sealed class BankBalanceService : IBankBalanceService
{
    private readonly IBankBalanceRepository _balances;

    public BankBalanceService(IBankBalanceRepository balances)
    {
        _balances = balances;
    }

    public async Task<IResult<BankBalance>> GetByIdAsync(string balanceId)
    {
        if (string.IsNullOrWhiteSpace(balanceId))
        {
            return Result<BankBalance>.Failure(Error.Create()
                .WithCode(BankAccountErrorCodes.BalanceIdInvalid)
                .WithMessage("O ID do saldo é obrigatório.")
                .Build());
        }

        var balance = await _balances.AsQueryable()
            .Where(b => b.Id == balanceId.Trim())
            .FirstOrDefaultAsync();

        if (balance is null)
        {
            return Result<BankBalance>.Failure(Error.Create()
                .WithCode(BankAccountErrorCodes.BalanceNotFound)
                .WithMessage($"O saldo '{balanceId}' não foi encontrado.")
                .Build());
        }

        return Result<BankBalance>.Success(balance);
    }

    public async Task<IResult<BankBalance>> CreditAsync(string bankAccountId, BankBalance balance)
    {
        ArgumentNullException.ThrowIfNull(balance);

        if (!string.Equals(balance.BankAccountId, bankAccountId.Trim(), StringComparison.Ordinal))
        {
            return Result<BankBalance>.Failure(Error.Create()
                .WithCode(BankAccountErrorCodes.BankAccountMismatch)
                .WithMessage("O saldo não pertence à conta bancária informada.")
                .Build());
        }

        var persisted = await _balances.CreateAsync(balance);
        return Result<BankBalance>.Success(persisted);
    }

    public async Task<IResult<BankDebitPartialResult>> DebitPartialAsync(string balanceId, decimal amountBrl)
    {
        var balanceResult = await GetByIdAsync(balanceId);
        if (balanceResult.IsFailure)
            return Result<BankDebitPartialResult>.Failure(balanceResult.Errors);

        var balance = balanceResult.Value!;
        var debitResult = balance.DebitPartial(amountBrl);
        if (debitResult.IsFailure)
            return debitResult;

        var outcome = debitResult.Value!;
        if (outcome.RemainderBalance is null)
        {
            await _balances.DeleteAsync(balance);
        }
        else
        {
            await _balances.UpdateAsync(outcome.RemainderBalance);
            await _balances.CreateAsync(outcome.DebitedBalance);
        }

        return Result<BankDebitPartialResult>.Success(outcome);
    }

    public async Task<IResult> DeleteAsync(string balanceId)
    {
        var balanceResult = await GetByIdAsync(balanceId);
        if (balanceResult.IsFailure)
            return Result.Failure(balanceResult.Errors);

        await _balances.DeleteAsync(balanceResult.Value!);
        return Result.Success();
    }
}

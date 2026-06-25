using Aidan.Core.Errors;
using Aidan.Core.Linq.Extensions;
using Aidan.Core.Patterns;
using Nexus.CryptoWallets.Aggregates;
using Nexus.CryptoWallets.Application.Contracts;
using Nexus.CryptoWallets.Errors;

namespace Nexus.CryptoWallets.Application.Services;

public sealed class CryptoBalanceService : ICryptoBalanceService
{
    private readonly ICryptoBalanceRepository _balances;

    public CryptoBalanceService(ICryptoBalanceRepository balances)
    {
        _balances = balances;
    }

    public async Task<IResult<CryptoBalance>> GetByIdAsync(string balanceId)
    {
        if (string.IsNullOrWhiteSpace(balanceId))
        {
            return Result<CryptoBalance>.Failure(Error.Create()
                .WithCode(CryptoWalletErrorCodes.BalanceIdInvalid)
                .WithMessage("O ID do saldo é obrigatório.")
                .Build());
        }

        var balance = await _balances.AsQueryable()
            .Where(b => b.Id == balanceId.Trim())
            .FirstOrDefaultAsync();

        if (balance is null)
        {
            return Result<CryptoBalance>.Failure(Error.Create()
                .WithCode(CryptoWalletErrorCodes.BalanceNotFound)
                .WithMessage($"O saldo '{balanceId}' não foi encontrado.")
                .Build());
        }

        return Result<CryptoBalance>.Success(balance);
    }

    public async Task<IResult<CryptoBalance>> CreditAsync(string cryptoWalletId, CryptoBalance balance)
    {
        ArgumentNullException.ThrowIfNull(balance);

        if (!string.Equals(balance.CryptoWalletId, cryptoWalletId.Trim(), StringComparison.Ordinal))
        {
            return Result<CryptoBalance>.Failure(Error.Create()
                .WithCode(CryptoWalletErrorCodes.CryptoWalletMismatch)
                .WithMessage("O saldo não pertence à wallet crypto informada.")
                .Build());
        }

        var persisted = await _balances.CreateAsync(balance);
        return Result<CryptoBalance>.Success(persisted);
    }

    public async Task<IResult<CryptoDebitPartialResult>> DebitPartialAsync(string balanceId, decimal amount)
    {
        var balanceResult = await GetByIdAsync(balanceId);
        if (balanceResult.IsFailure)
            return Result<CryptoDebitPartialResult>.Failure(balanceResult.Errors);

        var balance = balanceResult.Value!;
        var debitResult = balance.DebitPartial(amount);
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

        return Result<CryptoDebitPartialResult>.Success(outcome);
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

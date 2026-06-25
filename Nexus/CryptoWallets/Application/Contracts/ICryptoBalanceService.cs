using Aidan.Core.Patterns;
using Nexus.CryptoWallets.Aggregates;

namespace Nexus.CryptoWallets.Application.Contracts;

public interface ICryptoBalanceService
{
    Task<IResult<CryptoBalance>> GetByIdAsync(string balanceId);
    Task<IResult<CryptoBalance>> CreditAsync(string cryptoWalletId, CryptoBalance balance);
    Task<IResult<CryptoDebitPartialResult>> DebitPartialAsync(string balanceId, decimal amount);
    Task<IResult> DeleteAsync(string balanceId);
}

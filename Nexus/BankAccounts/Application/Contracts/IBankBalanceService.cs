using Aidan.Core.Patterns;
using Nexus.BankAccounts.Aggregates;

namespace Nexus.BankAccounts.Application.Contracts;

public interface IBankBalanceService
{
    Task<IResult<BankBalance>> GetByIdAsync(string balanceId);
    Task<IResult<BankBalance>> CreditAsync(string bankAccountId, BankBalance balance);
    Task<IResult<BankDebitPartialResult>> DebitPartialAsync(string balanceId, decimal amountBrl);
    Task<IResult> DeleteAsync(string balanceId);
}

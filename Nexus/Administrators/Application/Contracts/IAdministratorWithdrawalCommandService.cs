using Aidan.Core.Patterns;
using Nexus.Authorization.Application.Models;
using Nexus.Withdrawals.Application.Contracts;

namespace Nexus.Administrators.Application.Contracts;

public interface IAdministratorWithdrawalCommandService
{
    Task<IResult<Withdrawals.Aggregates.BankAccount>> CreateBankAccountAsync(CreateBankAccountRequest request);
    Task<IResult<Withdrawals.Aggregates.CryptoWallet>> CreateCryptoWalletAsync(CreateCryptoWalletRequest request);
    Task<IResult<Withdrawals.Aggregates.Withdrawal>> CreateWithdrawalAsync(CreateWithdrawalRequest request);
    Task<IResult<Withdrawals.Aggregates.Withdrawal>> GetWithdrawalAsync(string withdrawalId);
}

using Aidan.Core.Linq;
using Aidan.Core.Patterns;
using Nexus.Withdrawals.Aggregates;

namespace Nexus.Withdrawals.Application.Contracts;

public interface IBankAccountRepository : IRepository<BankAccount>
{
    new Task<BankAccount> CreateAsync(BankAccount entity);
}

public interface ICryptoWalletRepository : IRepository<CryptoWallet>
{
    new Task<CryptoWallet> CreateAsync(CryptoWallet entity);
}

public interface IWithdrawalRepository : IRepository<Withdrawal>
{
    new Task<Withdrawal> CreateAsync(Withdrawal entity);
}

using Aidan.Core.Patterns;
using Nexus.BankAccounts.Aggregates;

namespace Nexus.BankAccounts.Application.Contracts;

public interface IBankAccountRepository : IRepository<BankAccount>
{
    new Task<BankAccount> CreateAsync(BankAccount entity);
}

using Aidan.Core.Patterns;
using Nexus.BankAccounts.Aggregates;

namespace Nexus.BankAccounts.Application.Contracts;

public interface IBankBalanceRepository : IRepository<BankBalance>
{
    new Task<BankBalance> CreateAsync(BankBalance entity);
}

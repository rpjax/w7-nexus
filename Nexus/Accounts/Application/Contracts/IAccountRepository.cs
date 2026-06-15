using Aidan.Core.Patterns;
using Nexus.Accounts.Aggregates;

namespace Nexus.Accounts.Application.Contracts;

public interface IAccountRepository : IRepository<Account>
{
    new Task<Account> CreateAsync(Account entity);
}

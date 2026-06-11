using Aidan.Core.Patterns;
using Nexus.Accounts.Aggregates;

namespace Nexus.Accounts.Application.Services.Contracts;

public interface IAccountRepository : IRepository<Account>
{
    new Task<Account> CreateAsync(Account entity);
}

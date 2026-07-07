using Aidan.Core.Patterns;
using Nexus.Accounts.Aggregates;

namespace Nexus.Accounts.Application.Contracts;

public interface IAccountCreator
{
    Task<IResult<Account>> CreateAccountAsync(
        string username,
        string password,
        string[]? roles = null,
        string[]? permissions = null);
}

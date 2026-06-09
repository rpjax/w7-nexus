using Aidan.Core.Patterns;
using Nexus.Legacy.Accounts.Aggregates;

namespace Nexus.Legacy.Accounts.Application;

public interface IAccountCreator
{
    Task<IResult<Account>> CreateAccountAsync(
        string username,
        string password,
        string[]? roles = null,
        string[]? permissions = null);
}

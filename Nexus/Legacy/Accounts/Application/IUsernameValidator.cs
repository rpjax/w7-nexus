using Aidan.Core.Patterns;

namespace Nexus.Legacy.Accounts.Application;

public interface IUsernameValidator
{
    Task<IResult> ValidateForCreationAsync(string username);
    Task<IResult> ValidateForChangeAsync(string newUsername, string accountId);
}

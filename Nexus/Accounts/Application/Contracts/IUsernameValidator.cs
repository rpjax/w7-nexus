using Aidan.Core.Patterns;
using Nexus.Accounts.Application.Contracts;

namespace Nexus.Accounts.Application.Contracts;

public interface IUsernameValidator
{
    Task<IResult> ValidateForCreationAsync(string username);
    Task<IResult> ValidateForChangeAsync(string newUsername, string accountId);
}

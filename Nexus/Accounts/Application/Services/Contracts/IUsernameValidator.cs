using Aidan.Core.Patterns;

namespace Nexus.Accounts.Application.Services.Contracts;

public interface IUsernameValidator
{
    Task<IResult> ValidateForCreationAsync(string username);
    Task<IResult> ValidateForChangeAsync(string newUsername, string accountId);
}

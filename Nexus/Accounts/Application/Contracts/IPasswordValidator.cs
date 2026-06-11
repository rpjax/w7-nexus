using Aidan.Core.Patterns;
using Nexus.Accounts.Application.Contracts;

namespace Nexus.Accounts.Application.Contracts;

public interface IPasswordValidator
{
    Task<IResult> ValidateForCreationAsync(string password);
    Task<IResult> ValidateForChangeAsync(string newPassword);
}

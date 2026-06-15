using Aidan.Core.Patterns;

namespace Nexus.Accounts.Application.Services.Contracts;

public interface IPasswordValidator
{
    Task<IResult> ValidateForCreationAsync(string password);
    Task<IResult> ValidateForChangeAsync(string newPassword);
}

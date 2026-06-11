using Nexus.Accounts.Application.Contracts;

namespace Nexus.Accounts.Application.Contracts;

public interface IPasswordVerifier
{
    Task<bool> VerifyAsync(string password, string passwordHash);
}

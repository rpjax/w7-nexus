using Nexus.Accounts.Application.Contracts;

namespace Nexus.Accounts.Application;

public sealed class PasswordVerifier : IPasswordVerifier
{
    public Task<bool> VerifyAsync(string password, string passwordHash)
    {
        var isValid = BCrypt.Net.BCrypt.Verify(password, passwordHash);
        return Task.FromResult(isValid);
    }
}

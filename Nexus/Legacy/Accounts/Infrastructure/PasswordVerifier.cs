using Nexus.Legacy.Accounts.Application;

namespace Nexus.Legacy.Accounts.Infrastructure;

public sealed class PasswordVerifier : IPasswordVerifier
{
    public Task<bool> VerifyAsync(string password, string passwordHash)
    {
        var isValid = BCrypt.Net.BCrypt.Verify(password, passwordHash);
        return Task.FromResult(isValid);
    }
}

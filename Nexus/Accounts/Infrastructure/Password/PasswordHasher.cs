using Nexus.Accounts.Application.Contracts;

namespace Nexus.Accounts.Infrastructure.Password;

public sealed class PasswordHasher : IPasswordHasher
{
    private const int WorkFactor = 12;

    public Task<string> HashAsync(string password)
    {
        var hash = BCrypt.Net.BCrypt.HashPassword(password, WorkFactor);
        return Task.FromResult(hash);
    }
}

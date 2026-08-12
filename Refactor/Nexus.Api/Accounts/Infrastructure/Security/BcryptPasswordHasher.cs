using Refactor.Nexus.Api.Accounts.Application.Ports.Out.Security;

namespace Refactor.Nexus.Api.Accounts.Infrastructure.Security;

public sealed class BcryptPasswordHasher : IPasswordHasher
{
    public Task<string> HashAsync(string password, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(BCrypt.Net.BCrypt.HashPassword(password));
    }
}

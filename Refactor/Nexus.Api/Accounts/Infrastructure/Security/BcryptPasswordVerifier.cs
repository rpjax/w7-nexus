using Refactor.Nexus.Api.Accounts.Application.Ports.Out.Security;

namespace Refactor.Nexus.Api.Accounts.Infrastructure.Security;

public sealed class BcryptPasswordVerifier : IPasswordVerifier
{
    public Task<bool> VerifyAsync(string password, string passwordHash, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(BCrypt.Net.BCrypt.Verify(password, passwordHash));
    }
}

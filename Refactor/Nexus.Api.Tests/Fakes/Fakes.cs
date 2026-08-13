using Refactor.Nexus.Api.Accounts.Application.Ports.Out.Security;
using Refactor.Nexus.Api.Authentication.Application.Models;
using Refactor.Nexus.Api.Authentication.Application.Ports.Out.Tokens;
using Refactor.Nexus.Api.Journal.Services.Contracts;

namespace Refactor.Nexus.Api.Tests.Fakes;

internal sealed class RecordingJournalWriter : IJournalWriter
{
    public List<object> Facts { get; } = [];

    public void Append<T>(T payload)
    {
        if (payload is not null)
            Facts.Add(payload);
    }
}

internal sealed class FakePasswordHasher : IPasswordHasher
{
    public Task<string> HashAsync(string password, CancellationToken cancellationToken = default) =>
        Task.FromResult($"hash:{password}");
}

internal sealed class FakePasswordVerifier : IPasswordVerifier
{
    public Task<bool> VerifyAsync(string password, string passwordHash, CancellationToken cancellationToken = default) =>
        Task.FromResult(passwordHash == $"hash:{password}" || passwordHash == password);
}

internal sealed class FakeJwtTokenService : IJwtTokenService
{
    public AuthenticationTokens GenerateTokens(JwtTokenSubject subject) =>
        new()
        {
            AccessToken = $"access:{subject.AccountId}",
            RefreshToken = "refresh",
            ExpiresAt = DateTime.UtcNow.AddHours(1)
        };
}

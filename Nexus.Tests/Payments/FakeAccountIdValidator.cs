using Nexus.Legacy.Payments.Application;

namespace Nexus.Tests.Payments;

internal sealed class FakeAccountIdValidator : IAccountIdValidator
{
    private readonly HashSet<string> _existingIds;

    public FakeAccountIdValidator(IEnumerable<string>? existingIds = null)
    {
        _existingIds = existingIds is null
            ? new HashSet<string>(StringComparer.Ordinal)
            : new HashSet<string>(existingIds, StringComparer.Ordinal);
    }

    public void AddExisting(string id) => _existingIds.Add(id);

    public Task<bool> ExistsAsync(string accountId) =>
        Task.FromResult(_existingIds.Contains(accountId));
}

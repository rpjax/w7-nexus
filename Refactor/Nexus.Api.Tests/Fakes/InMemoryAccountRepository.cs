using Refactor.Nexus.Api.Accounts.Application.Ports.Out.Persistence;
using Refactor.Nexus.Api.Accounts.Domain.Aggregates.Account;

namespace Refactor.Nexus.Api.Tests.Fakes;

internal sealed class InMemoryAccountRepository : IAccountRepository, IAccountReadRepository
{
    private readonly Dictionary<Guid, Account> _accounts = [];
    private readonly HashSet<string> _retiredUsernames = new(StringComparer.OrdinalIgnoreCase);

    public Task<Account?> GetByIdAsync(AccountId accountId, CancellationToken cancellationToken = default)
    {
        _accounts.TryGetValue(accountId.Value, out var account);
        return Task.FromResult(account);
    }

    public Task<Account> CreateAsync(Account account, CancellationToken cancellationToken = default)
    {
        _accounts[account.Id.Value] = account;
        account.ClearUncommitted();
        return Task.FromResult(account);
    }

    public Task UpdateAsync(Account account, CancellationToken cancellationToken = default)
    {
        _accounts[account.Id.Value] = account;
        account.ClearUncommitted();
        return Task.CompletedTask;
    }

    public async Task UpdateChangingUsernameAsync(
        Account account,
        string previousUsername,
        CancellationToken cancellationToken = default)
    {
        await UpdateAsync(account, cancellationToken);
        await RetireUsernameAsync(previousUsername, account.Id, cancellationToken);
    }

    public Task RetireUsernameAsync(string username, AccountId retiredFrom, CancellationToken cancellationToken = default)
    {
        _retiredUsernames.Add(username.Trim());
        return Task.CompletedTask;
    }

    public Task<Account?> FindByUsernameAsync(string username, CancellationToken cancellationToken = default)
    {
        var match = _accounts.Values.FirstOrDefault(account =>
            string.Equals(account.Username, username, StringComparison.OrdinalIgnoreCase));
        return Task.FromResult(match);
    }

    public Task<bool> IsUsernameRetiredAsync(string username, CancellationToken cancellationToken = default) =>
        Task.FromResult(_retiredUsernames.Contains(username.Trim()));

    public async Task<bool> IsUsernameTakenAsync(string username, CancellationToken cancellationToken = default)
    {
        if (await FindByUsernameAsync(username, cancellationToken) is not null)
            return true;

        return await IsUsernameRetiredAsync(username, cancellationToken);
    }

    public Task<(IReadOnlyList<Account> Items, int Total)> SearchAsync(
        string? keyword,
        string? status,
        string? role,
        int offset,
        int limit,
        CancellationToken cancellationToken = default)
    {
        var items = _accounts.Values.ToList();
        return Task.FromResult(((IReadOnlyList<Account>)items, items.Count));
    }

    public Task<int> CountByRoleAsync(string role, CancellationToken cancellationToken = default)
    {
        var count = _accounts.Values.Count(account =>
            account.Roles.Contains(role, StringComparer.OrdinalIgnoreCase));
        return Task.FromResult(count);
    }
}

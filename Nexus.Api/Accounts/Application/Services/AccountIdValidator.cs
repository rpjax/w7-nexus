using Nexus.Accounts.Application.Contracts;

namespace Nexus.Accounts.Application.Services;

public sealed class AccountIdValidator : IAccountIdValidator
{
    private readonly IAccountRepository _accounts;

    public AccountIdValidator(IAccountRepository accounts)
    {
        _accounts = accounts;
    }

    public Task<bool> ExistsAsync(string accountId)
    {
        var exists = _accounts.AsQueryable()
            .Any(a => a.Id == accountId);
        return Task.FromResult(exists);
    }
}

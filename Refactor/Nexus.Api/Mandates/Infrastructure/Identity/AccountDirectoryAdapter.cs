using Refactor.Nexus.Api.Accounts.Application.Ports.Out.Persistence;
using Refactor.Nexus.Api.Accounts.Domain.Aggregates.Account;
using Refactor.Nexus.Api.Mandates.Application.Ports.Out.Identity;
using Refactor.Nexus.Api.Mandates.Domain.Aggregates;

namespace Refactor.Nexus.Api.Mandates.Infrastructure.Identity;

public sealed class AccountDirectoryAdapter : IAccountDirectory
{
    private readonly IAccountRepository _accounts;

    public AccountDirectoryAdapter(IAccountRepository accounts)
    {
        _accounts = accounts;
    }

    public async Task<bool> ExistsAsync(MemberId accountId, CancellationToken cancellationToken = default)
    {
        var account = await _accounts.GetByIdAsync(new AccountId(accountId.Value), cancellationToken);
        return account is not null;
    }

    public async Task<bool> IsAdministratorAsync(MemberId accountId, CancellationToken cancellationToken = default)
    {
        var account = await _accounts.GetByIdAsync(new AccountId(accountId.Value), cancellationToken);
        return account?.IsAdministrator == true;
    }
}

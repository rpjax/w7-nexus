using Refactor.Nexus.Api.Accounts.Domain.Aggregates.Account;
using Refactor.Nexus.Api.Journal.Services.Contracts;

namespace Refactor.Nexus.Api.Accounts.Application.Journal;

internal static class AccountJournal
{
    public static void RecordCreated(this IJournalWriter journal, Account account)
    {
        journal.Append(new AccountCreated
        {
            AccountId = account.Id.Value,
            Username = account.Username,
            IsAdministrator = account.IsAdministrator
        });
    }

    public static void RecordDisabled(this IJournalWriter journal, Account account)
    {
        journal.Append(new AccountDisabled
        {
            AccountId = account.Id.Value,
            Username = account.Username
        });
    }

    public static void RecordEnabled(this IJournalWriter journal, Account account)
    {
        journal.Append(new AccountEnabled
        {
            AccountId = account.Id.Value,
            Username = account.Username
        });
    }

    public static void RecordRoleGranted(this IJournalWriter journal, Account account, string role)
    {
        journal.Append(new AccountRoleGranted
        {
            AccountId = account.Id.Value,
            Role = role
        });
    }

    public static void RecordRoleRevoked(this IJournalWriter journal, Account account, string role)
    {
        journal.Append(new AccountRoleRevoked
        {
            AccountId = account.Id.Value,
            Role = role
        });
    }

    public static void RecordPasswordReset(this IJournalWriter journal, Account account)
    {
        journal.Append(new AccountPasswordReset
        {
            AccountId = account.Id.Value
        });
    }

    public static void RecordUsernameChanged(this IJournalWriter journal, Account account, string previousUsername)
    {
        journal.Append(new AccountUsernameChanged
        {
            AccountId = account.Id.Value,
            PreviousUsername = previousUsername,
            NewUsername = account.Username
        });
    }
}

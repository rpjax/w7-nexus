using Refactor.Nexus.Api.Journal.Attributes;

namespace Refactor.Nexus.Api.Accounts.Application.Journal;

[CanonicalFact("Accounts.AccountCreated", schemaVersion: 1, Owner = "accounts", Name = "Account created")]
public sealed class AccountCreated
{
    [JournalIndex("account")]
    public required Guid AccountId { get; init; }

    public required string Username { get; init; }

    public required bool IsAdministrator { get; init; }
}

[CanonicalFact("Accounts.AccountDisabled", schemaVersion: 1, Owner = "accounts", Name = "Account disabled")]
public sealed class AccountDisabled
{
    [JournalIndex("account")]
    public required Guid AccountId { get; init; }

    public required string Username { get; init; }
}

[CanonicalFact("Accounts.AccountEnabled", schemaVersion: 1, Owner = "accounts", Name = "Account enabled")]
public sealed class AccountEnabled
{
    [JournalIndex("account")]
    public required Guid AccountId { get; init; }

    public required string Username { get; init; }
}

[CanonicalFact("Accounts.AccountRoleGranted", schemaVersion: 1, Owner = "accounts", Name = "Account role granted")]
public sealed class AccountRoleGranted
{
    [JournalIndex("account")]
    public required Guid AccountId { get; init; }

    public required string Role { get; init; }
}

[CanonicalFact("Accounts.AccountRoleRevoked", schemaVersion: 1, Owner = "accounts", Name = "Account role revoked")]
public sealed class AccountRoleRevoked
{
    [JournalIndex("account")]
    public required Guid AccountId { get; init; }

    public required string Role { get; init; }
}

[CanonicalFact("Accounts.AccountPasswordReset", schemaVersion: 1, Owner = "accounts", Name = "Account password reset")]
public sealed class AccountPasswordReset
{
    [JournalIndex("account")]
    public required Guid AccountId { get; init; }
}

[CanonicalFact("Accounts.AccountUsernameChanged", schemaVersion: 1, Owner = "accounts", Name = "Account username changed")]
public sealed class AccountUsernameChanged
{
    [JournalIndex("account")]
    public required Guid AccountId { get; init; }

    public required string PreviousUsername { get; init; }

    public required string NewUsername { get; init; }
}

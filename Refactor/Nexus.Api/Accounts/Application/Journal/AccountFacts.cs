using Refactor.Nexus.Api.Journal.Attributes;

namespace Refactor.Nexus.Api.Accounts.Application.Journal;

[CanonicalFact("Accounts.AccountCreated", schemaVersion: 1, Owner = "accounts", Name = "Account created")]
public sealed class AccountCreated
{
    [JournalIndex("account")]
    public required Guid AccountId { get; init; }

    public required string Handle { get; init; }

    public required bool IsAdministrator { get; init; }
}

[CanonicalFact("Accounts.AccountDisabled", schemaVersion: 1, Owner = "accounts", Name = "Account disabled")]
public sealed class AccountDisabled
{
    [JournalIndex("account")]
    public required Guid AccountId { get; init; }

    public required string Handle { get; init; }
}

[CanonicalFact("Accounts.AccountEnabled", schemaVersion: 1, Owner = "accounts", Name = "Account enabled")]
public sealed class AccountEnabled
{
    [JournalIndex("account")]
    public required Guid AccountId { get; init; }

    public required string Handle { get; init; }
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

[CanonicalFact("Accounts.AccountHandleChanged", schemaVersion: 1, Owner = "accounts", Name = "Account handle changed")]
public sealed class AccountHandleChanged
{
    [JournalIndex("account")]
    public required Guid AccountId { get; init; }

    public required string PreviousHandle { get; init; }

    public required string NewHandle { get; init; }
}

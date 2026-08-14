using Refactor.Nexus.Api.Accounts.Domain.Aggregates.Account;

namespace Refactor.Nexus.Api.Accounts.Domain.Events;

public sealed record AccountRegistered(
    Guid AccountId,
    string Username,
    string[] Roles,
    string[] Permissions,
    DateTime OccurredAt,
    Guid? ActedBy);

public sealed record AccountBackfilled(
    Guid AccountId,
    string Username,
    AccountStatus Status,
    string[] Roles,
    string[] Permissions,
    DateTime CreatedAt,
    DateTime LastUpdatedAt);

public sealed record AccountDisabled(Guid AccountId, DateTime OccurredAt, Guid? ActedBy);
public sealed record AccountEnabled(Guid AccountId, DateTime OccurredAt, Guid? ActedBy);
public sealed record AccountAdministratorGranted(Guid AccountId, DateTime OccurredAt, Guid? ActedBy);
public sealed record AccountAdministratorRevoked(Guid AccountId, DateTime OccurredAt, Guid? ActedBy);
public sealed record AccountUsernameChanged(Guid AccountId, string FromUsername, string ToUsername, DateTime OccurredAt, Guid? ActedBy);
public sealed record AccountPasswordChanged(Guid AccountId, DateTime OccurredAt, Guid? ActedBy);
public sealed record AccountPermissionGranted(Guid AccountId, string Permission, DateTime OccurredAt, Guid? ActedBy);
public sealed record AccountPermissionRevoked(Guid AccountId, string Permission, DateTime OccurredAt, Guid? ActedBy);

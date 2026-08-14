using Refactor.Nexus.Api.Journal.Services.Contracts;

namespace Refactor.Nexus.Api.WorldAccounts.Application.Journal;

internal static class WorldAccountJournal
{
    public static void RecordOpened(this IJournalWriter journal, Guid accountId, Guid actedBy) =>
        journal.Append(new WorldAccountOpened { AccountId = accountId, ActedBy = actedBy });

    public static void RecordLabeled(this IJournalWriter journal, Guid accountId, Guid actedBy) =>
        journal.Append(new WorldAccountLabeled { AccountId = accountId, ActedBy = actedBy });

    public static void RecordConfigured(this IJournalWriter journal, Guid accountId, Guid actedBy) =>
        journal.Append(new WorldAccountConfigured { AccountId = accountId, ActedBy = actedBy });

    public static void RecordObservation(this IJournalWriter journal, Guid accountId, Guid actedBy) =>
        journal.Append(new WorldAccountObservationRecorded { AccountId = accountId, ActedBy = actedBy });

    public static void RecordListed(this IJournalWriter journal, Guid actedBy) =>
        journal.Append(new WorldAccountsListed { ActedBy = actedBy });

    public static void RecordRead(this IJournalWriter journal, Guid accountId, Guid actedBy) =>
        journal.Append(new WorldAccountRead { AccountId = accountId, ActedBy = actedBy });
}

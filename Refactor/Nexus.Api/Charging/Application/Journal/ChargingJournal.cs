using Refactor.Nexus.Api.Journal.Services.Contracts;

namespace Refactor.Nexus.Api.Charging.Application.Journal;

internal static class ChargingJournal
{
    public static void RecordChargeCreated(this IJournalWriter journal, Guid chargeId, Guid actedBy) =>
        journal.Append(new ChargingChargeCreated { ChargeId = chargeId, ActedBy = actedBy });

    public static void RecordChargeTransitioned(this IJournalWriter journal, Guid chargeId, Guid actedBy) =>
        journal.Append(new ChargingChargeTransitioned
        {
            ChargeId = chargeId,
            ActedBy = actedBy == Guid.Empty ? null : actedBy,
        });

    public static void TryRecordChargeTransitioned(IJournalWriter journal, Guid chargeId, Guid actedBy)
    {
        try
        {
            journal.RecordChargeTransitioned(chargeId, actedBy);
        }
        catch
        {
            // Charge event already persisted; audit must not turn success into HTTP 500.
        }
    }

    public static void RecordRailBound(this IJournalWriter journal, Guid operationId, Guid actedBy) =>
        journal.Append(new ChargingRailBound { OperationId = operationId, ActedBy = actedBy });

    public static void RecordRailUnbound(this IJournalWriter journal, Guid operationId, Guid actedBy) =>
        journal.Append(new ChargingRailUnbound { OperationId = operationId, ActedBy = actedBy });

    public static void RecordRailsListed(this IJournalWriter journal, Guid actedBy) =>
        journal.Append(new ChargingRailsListed { ActedBy = actedBy });
}

using Refactor.Nexus.Api.Journal.Services.Contracts;
using OperationAggregate = Refactor.Nexus.Api.Operations.Domain.Aggregates.Operation.Operation;
using ScriptArtifact = Refactor.Nexus.Api.Operations.Domain.Aggregates.Script.ScriptArtifact;
using StoreObject = Refactor.Nexus.Api.Operations.Domain.Aggregates.Store.StoreObject;

namespace Refactor.Nexus.Api.Operations.Application.Journal;

internal static class OperationJournal
{
    public static void RecordCreated(this IJournalWriter journal, OperationAggregate operation)
    {
        journal.Append(new OperationCreated
        {
            OperationId = operation.Id.Value,
            OperationKey = operation.Key.Value,
            Name = operation.Name
        });
    }

    public static void RecordTransitioned(
        this IJournalWriter journal,
        Guid operationId,
        string from,
        string to)
    {
        journal.Append(new OperationTransitioned
        {
            OperationId = operationId,
            FromStatus = from,
            ToStatus = to
        });
    }

    public static void RecordAssigned(this IJournalWriter journal, Guid operationId, Guid memberId)
    {
        journal.Append(new OperatorAssigned { OperationId = operationId, MemberId = memberId });
    }

    public static void RecordUnassigned(this IJournalWriter journal, Guid operationId, Guid memberId)
    {
        journal.Append(new OperatorUnassigned { OperationId = operationId, MemberId = memberId });
    }

    public static void RecordScriptRegistered(this IJournalWriter journal, ScriptArtifact script)
    {
        journal.Append(new ScriptRegistered
        {
            ScriptId = script.Id,
            OperationKey = script.OperationKey.Value
        });
    }

    public static void RecordStoreUpserted(this IJournalWriter journal, StoreObject storeObject)
    {
        journal.Append(new StoreObjectUpserted
        {
            ObjectId = storeObject.Id,
            OperationKey = storeObject.OperationKey.Value
        });
    }

    public static void RecordStoreDeleted(this IJournalWriter journal, Guid objectId, string operationKey)
    {
        journal.Append(new StoreObjectDeleted { ObjectId = objectId, OperationKey = operationKey });
    }
}

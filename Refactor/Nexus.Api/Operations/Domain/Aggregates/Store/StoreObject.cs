using Aidan.Core.Errors;
using Aidan.Core.Patterns;
using IResult = Aidan.Core.Patterns.IResult;
using Refactor.Nexus.Api.Operations.Domain.Aggregates.Operation;
using Refactor.Nexus.Api.Operations.Domain.Errors;

namespace Refactor.Nexus.Api.Operations.Domain.Aggregates.Store;

public sealed class StoreObject
{
    private StoreObject(
        Guid id,
        OperationKey operationKey,
        string objectType,
        string payloadJson,
        DateTime createdAt,
        DateTime lastUpdatedAt)
    {
        Id = id;
        OperationKey = operationKey;
        ObjectType = objectType;
        PayloadJson = payloadJson;
        CreatedAt = createdAt;
        LastUpdatedAt = lastUpdatedAt;
    }

    public Guid Id { get; }
    public OperationKey OperationKey { get; }
    public string ObjectType { get; private set; }
    public string PayloadJson { get; private set; }
    public DateTime CreatedAt { get; }
    public DateTime LastUpdatedAt { get; private set; }

    public static IResult<StoreObject> Create(OperationKey operationKey, string objectType, string payloadJson)
    {
        if (string.IsNullOrWhiteSpace(operationKey.Value))
        {
            return Result<StoreObject>.Failure(Error.Create()
                .WithCode(OperationErrorCodes.KeyEmpty)
                .WithMessage("Operation key obrigatoria.")
                .Build());
        }

        if (string.IsNullOrWhiteSpace(objectType))
        {
            return Result<StoreObject>.Failure(Error.Create()
                .WithCode(OperationErrorCodes.NameEmpty)
                .WithMessage("ObjectType e obrigatorio.")
                .Build());
        }

        var now = DateTime.UtcNow;
        return Result<StoreObject>.Success(new StoreObject(
            Guid.NewGuid(),
            operationKey,
            objectType.Trim(),
            payloadJson ?? "{}",
            now,
            now));
    }

    public static StoreObject Rehydrate(
        Guid id,
        OperationKey operationKey,
        string objectType,
        string payloadJson,
        DateTime createdAt,
        DateTime lastUpdatedAt) =>
        new(id, operationKey, objectType, payloadJson, createdAt, lastUpdatedAt);

    public void Update(string objectType, string payloadJson)
    {
        ObjectType = objectType.Trim();
        PayloadJson = payloadJson ?? "{}";
        LastUpdatedAt = DateTime.UtcNow;
    }
}

namespace Refactor.Nexus.Api.Operations.Domain.Aggregates.Operation;

public readonly record struct OperationId(Guid Value)
{
    public static OperationId New() => new(Guid.NewGuid());

    public static bool TryParse(string? raw, out OperationId operationId)
    {
        if (Guid.TryParse(raw, out var value))
        {
            operationId = new OperationId(value);
            return true;
        }

        operationId = default;
        return false;
    }

    public override string ToString() => Value.ToString();
}

public readonly record struct OperationKey(string Value)
{
    public static OperationKey Mint() => new($"op_{Guid.NewGuid():N}");

    public static bool TryCreate(string? raw, out OperationKey key)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            key = default;
            return false;
        }

        key = new OperationKey(raw.Trim());
        return true;
    }

    public override string ToString() => Value;
}

public enum OperationStatus
{
    Draft = 0,
    Active = 1,
    Paused = 2,
    Closed = 3
}

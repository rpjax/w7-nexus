namespace Refactor.Nexus.Api.Mandates.Domain.Aggregates;

public readonly record struct MemberId(Guid Value)
{
    public static MemberId New() => new(Guid.NewGuid());

    public static bool TryParse(string? raw, out MemberId memberId)
    {
        if (Guid.TryParse(raw, out var value))
        {
            memberId = new MemberId(value);
            return true;
        }

        memberId = default;
        return false;
    }

    public override string ToString() => Value.ToString();
}

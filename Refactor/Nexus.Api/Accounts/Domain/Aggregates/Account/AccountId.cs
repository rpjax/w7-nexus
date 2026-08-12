namespace Refactor.Nexus.Api.Accounts.Domain.Aggregates.Account;

public readonly record struct AccountId(Guid Value)
{
    public static AccountId New() => new(Guid.NewGuid());

    public static bool TryParse(string? raw, out AccountId accountId)
    {
        if (Guid.TryParse(raw, out var value))
        {
            accountId = new AccountId(value);
            return true;
        }

        accountId = default;
        return false;
    }

    public override string ToString() => Value.ToString();
}

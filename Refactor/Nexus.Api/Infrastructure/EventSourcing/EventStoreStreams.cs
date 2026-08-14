namespace Refactor.Nexus.Api.Infrastructure.EventSourcing;

public static class EventStoreStreams
{
    public static string Account(Guid id) => $"account-{id:N}";
    public static string Mandate(Guid id) => $"mandate-{id:N}";
    public static string Deal(Guid id) => $"deal-{id:N}";
    public static string Stake(Guid id) => $"stake-{id:N}";
    public static string Operation(Guid id) => $"operation-{id:N}";
    public static string Charge(Guid id) => $"charge-{id:N}";
    public static string WorldAccount(Guid id) => $"world-account-{id:N}";
    public static string Claim(Guid id) => $"claim-{id:N}";
    public static string Hop(Guid id) => $"hop-{id:N}";
}

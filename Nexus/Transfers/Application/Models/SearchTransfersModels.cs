using Nexus.Transfers.Aggregates;

namespace Nexus.Transfers.Application.Models;

public sealed class SearchTransfersRequest
{
    public string? StrawManId { get; init; }
    public TransferType? Type { get; init; }
    public int Limit { get; init; } = 30;
    public int Offset { get; init; }
}

public sealed class SearchTransfersResponse
{
    public int Total { get; init; }
    public IReadOnlyList<Transfer> Items { get; init; } = Array.Empty<Transfer>();
}

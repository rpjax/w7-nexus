using Aidan.Core.Patterns;
using Nexus.Transfers.Application.Contracts;
using Nexus.Transfers.Application.Models;
using Nexus.Transfers.Aggregates;

namespace Nexus.Administrators.Application.Contracts;

public interface IAdministratorTransferCommandService
{
    Task<IResult<Transfer>> ExecuteWithdrawalAsync(WithdrawalTransferRequest request, CancellationToken cancellationToken = default);
    Task<IResult<Transfer>> ExecuteMovementAsync(MovementTransferRequest request, CancellationToken cancellationToken = default);
    Task<IResult<Transfer>> ExecutePayoutAsync(PayoutTransferRequest request, CancellationToken cancellationToken = default);
    Task<IResult<Transfer>> GetTransferAsync(string transferId);
    Task<IResult<SearchTransfersResponse>> SearchTransfersAsync(SearchTransfersRequest? request);
    Task<IResult<TransferTimelineDetails>> GetTransferTimelineAsync(string transferId);
}

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

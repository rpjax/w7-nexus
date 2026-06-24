using Aidan.Core.Linq.Extensions;
using Aidan.Core.Patterns;
using Nexus.Administrators.Application.Contracts;
using Nexus.Transfers.Application.Contracts;
using Nexus.Transfers.Application.Models;
using Nexus.Transfers.Aggregates;

namespace Nexus.Administrators.Application.Services;

public sealed class AdministratorTransferCommandService : IAdministratorTransferCommandService
{
    private readonly ITransferService _transfers;
    private readonly ITransferRepository _transferRepository;
    private readonly ITransferTimelineQueryService _timeline;

    public AdministratorTransferCommandService(
        ITransferService transfers,
        ITransferRepository transferRepository,
        ITransferTimelineQueryService timeline)
    {
        _transfers = transfers;
        _transferRepository = transferRepository;
        _timeline = timeline;
    }

    public Task<IResult<Transfer>> ExecuteWithdrawalAsync(WithdrawalTransferRequest request, CancellationToken cancellationToken = default) =>
        _transfers.ExecuteWithdrawalAsync(request, cancellationToken);

    public Task<IResult<Transfer>> ExecuteMovementAsync(MovementTransferRequest request, CancellationToken cancellationToken = default) =>
        _transfers.ExecuteMovementAsync(request, cancellationToken);

    public Task<IResult<Transfer>> ExecutePayoutAsync(PayoutTransferRequest request, CancellationToken cancellationToken = default) =>
        _transfers.ExecutePayoutAsync(request, cancellationToken);

    public Task<IResult<Transfer>> GetTransferAsync(string transferId) =>
        _transfers.GetByIdAsync(transferId);

    public Task<IResult<TransferTimelineDetails>> GetTransferTimelineAsync(string transferId) =>
        _timeline.GetTimelineAsync(transferId);

    public async Task<IResult<SearchTransfersResponse>> SearchTransfersAsync(SearchTransfersRequest? request)
    {
        request ??= new SearchTransfersRequest();
        var query = _transferRepository.AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.StrawManId))
            query = query.Where(t => t.StrawManId == request.StrawManId.Trim());

        if (request.Type.HasValue)
            query = query.Where(t => t.Type == request.Type.Value);

        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(t => t.CreatedAt)
            .Skip(Math.Max(0, request.Offset))
            .Take(NormalizeLimit(request.Limit))
            .ToArrayAsync();

        return Result<SearchTransfersResponse>.Success(new SearchTransfersResponse
        {
            Total = (int)total,
            Items = items,
        });
    }

    private static int NormalizeLimit(int limit) => limit <= 0 ? 30 : Math.Min(limit, 999);
}

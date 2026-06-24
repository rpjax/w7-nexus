using Aidan.Core.Errors;
using Aidan.Core.Patterns;
using Nexus.Transfers.Aggregates;
using Nexus.Transfers.Application.Contracts;
using Nexus.Transfers.Errors;

namespace Nexus.Transfers.Application.Services;

public sealed class TransferService : ITransferService
{
    private readonly IWithdrawalTransferUseCase _withdrawal;
    private readonly IMovementTransferUseCase _movement;
    private readonly IPayoutTransferUseCase _payout;
    private readonly ITransferRepository _transfers;

    public TransferService(
        IWithdrawalTransferUseCase withdrawal,
        IMovementTransferUseCase movement,
        IPayoutTransferUseCase payout,
        ITransferRepository transfers)
    {
        _withdrawal = withdrawal;
        _movement = movement;
        _payout = payout;
        _transfers = transfers;
    }

    public Task<IResult<Transfer>> ExecuteWithdrawalAsync(
        WithdrawalTransferRequest request,
        CancellationToken cancellationToken = default) =>
        _withdrawal.ExecuteAsync(request, cancellationToken);

    public Task<IResult<Transfer>> ExecuteMovementAsync(
        MovementTransferRequest request,
        CancellationToken cancellationToken = default) =>
        _movement.ExecuteAsync(request, cancellationToken);

    public Task<IResult<Transfer>> ExecutePayoutAsync(
        PayoutTransferRequest request,
        CancellationToken cancellationToken = default) =>
        _payout.ExecuteAsync(request, cancellationToken);

    public Task<IResult<Transfer>> GetByIdAsync(string transferId)
    {
        if (string.IsNullOrWhiteSpace(transferId))
        {
            return Task.FromResult<IResult<Transfer>>(Result<Transfer>.Failure(Error.Create()
                .WithCode(TransferErrorCodes.TransferIdInvalid)
                .WithMessage("O ID da transferência é obrigatório.")
                .Build()));
        }

        var transfer = _transfers.AsQueryable()
            .FirstOrDefault(t => t.Id == transferId.Trim());

        if (transfer is null)
        {
            return Task.FromResult<IResult<Transfer>>(Result<Transfer>.Failure(Error.Create()
                .WithCode(TransferErrorCodes.TransferNotFound)
                .WithMessage($"A transferência '{transferId}' não foi encontrada.")
                .Build()));
        }

        return Task.FromResult<IResult<Transfer>>(Result<Transfer>.Success(transfer));
    }
}

using Aidan.Core.Patterns;
using Nexus.Transfers.Aggregates;
using Nexus.Transfers.Application.Models;
using Nexus.Transfers.Application.Requests;

namespace Nexus.Transfers.Application.Contracts;

public interface ITransferService
{
    Task<IResult<Transfer>> ExecuteBankAccountMovementAsync(
        BankAccountMovementRequest request,
        CancellationToken cancellationToken = default);

    Task<IResult<Transfer>> ExecuteCryptoWalletMovementAsync(
        CryptoWalletMovementRequest request,
        CancellationToken cancellationToken = default);

    Task<IResult<Transfer>> ExecuteWithdrawalAsync(
        WithdrawalTransferRequest request,
        CancellationToken cancellationToken = default);

    Task<IResult<Transfer>> ExecutePayoutAsync(
        PayoutTransferRequest request,
        CancellationToken cancellationToken = default);

    Task<IResult<Transfer>> GetByIdAsync(string transferId);

    Task<IResult<SearchTransfersResponse>> SearchAsync(
        SearchTransfersRequest? request,
        CancellationToken cancellationToken = default);

    Task<IResult<TransferTimelineDetails>> GetTimelineAsync(
        string transferId,
        CancellationToken cancellationToken = default);
}

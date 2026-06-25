using Aidan.Core.Linq;
using Aidan.Core.Patterns;
using Nexus.CryptoWallets.Aggregates;
using Nexus.Transfers.Aggregates;

namespace Nexus.Transfers.Application.Contracts;

public interface ITransferRepository : IRepository<Transfer>
{
    new Task<Transfer> CreateAsync(Transfer entity);
}

public interface ITransferService
{
    Task<IResult<Transfer>> ExecuteWithdrawalAsync(WithdrawalTransferRequest request, CancellationToken cancellationToken = default);
    Task<IResult<Transfer>> ExecuteMovementAsync(MovementTransferRequest request, CancellationToken cancellationToken = default);
    Task<IResult<Transfer>> ExecutePayoutAsync(PayoutTransferRequest request, CancellationToken cancellationToken = default);
    Task<IResult<Transfer>> GetByIdAsync(string transferId);
}

public interface IWithdrawalTransferUseCase
{
    Task<IResult<Transfer>> ExecuteAsync(WithdrawalTransferRequest request, CancellationToken cancellationToken = default);
}

public interface IMovementTransferUseCase
{
    Task<IResult<Transfer>> ExecuteAsync(MovementTransferRequest request, CancellationToken cancellationToken = default);
}

public interface IPayoutTransferUseCase
{
    Task<IResult<Transfer>> ExecuteAsync(PayoutTransferRequest request, CancellationToken cancellationToken = default);
}

public sealed class WithdrawalTransferRequest
{
    public string StrawManId { get; init; } = string.Empty;
    public string? BankAccountId { get; init; }
    public string? CryptoWalletId { get; init; }
    public IReadOnlyList<string> PaymentIds { get; init; } = Array.Empty<string>();
    public OnrampingMethod? OnrampingMethod { get; init; }
    public decimal? ProducedAmount { get; init; }
    public CryptoAsset? ProducedAsset { get; init; }
    public Chain? ProducedChain { get; init; }
    public string? PixTransactionId { get; init; }
    public string? PixAuthenticationCode { get; init; }
    public string? CryptoTransactionId { get; init; }
}

public sealed class MovementTransferRequest
{
    public string StrawManId { get; init; } = string.Empty;
    public string? SourceBankAccountId { get; init; }
    public string? SourceCryptoWalletId { get; init; }
    public string SourceBalanceId { get; init; } = string.Empty;
    public decimal SourceAmount { get; init; }
    public string? DestinationBankAccountId { get; init; }
    public string? DestinationCryptoWalletId { get; init; }
    public OnrampingMethod? OnrampingMethod { get; init; }
    public decimal? ProducedAmount { get; init; }
    public CryptoAsset? ProducedAsset { get; init; }
    public Chain? ProducedChain { get; init; }
    public string? PixTransactionId { get; init; }
    public string? PixAuthenticationCode { get; init; }
    public string? CryptoTransactionId { get; init; }
}

public sealed class PayoutTransferRequest
{
    public string StrawManId { get; init; } = string.Empty;
    public string SourceBankAccountId { get; init; } = string.Empty;
    public string SourceBalanceId { get; init; } = string.Empty;
    public decimal SourceAmount { get; init; }
    public string? DestinationBankAccountId { get; init; }
    public string? DestinationCryptoWalletId { get; init; }
    public string? PixTransactionId { get; init; }
    public string? PixAuthenticationCode { get; init; }
    public string? CryptoTransactionId { get; init; }
}

using Aidan.Core.Patterns;
using Nexus.Withdrawals.Aggregates;

namespace Nexus.Withdrawals.Application.Contracts;

public interface IBankAccountService
{
    Task<IResult<BankAccount>> CreateAsync(CreateBankAccountRequest request);
    Task<IResult<BankAccount>> UpdateLabelAsync(string bankAccountId, string? label);
    Task<IResult<BankAccount>> GetByIdAsync(string bankAccountId);
}

public interface ICryptoWalletService
{
    Task<IResult<CryptoWallet>> CreateAsync(CreateCryptoWalletRequest request);
    Task<IResult<CryptoWallet>> UpdateLabelAsync(string cryptoWalletId, string? label);
    Task<IResult<CryptoWallet>> GetByIdAsync(string cryptoWalletId);
}

public interface IWithdrawalService
{
    Task<IResult<Withdrawal>> CreateWithdrawalAsync(CreateWithdrawalRequest request);
    Task<IResult<Withdrawal>> GetByIdAsync(string withdrawalId);
}

public sealed class CreateBankAccountRequest
{
    public string StrawManAccountId { get; init; } = string.Empty;
    public BrazilianBank Bank { get; init; }
    public string Agency { get; init; } = string.Empty;
    public string AccountNumber { get; init; } = string.Empty;
    public string? AccountDigit { get; init; }
    public BankAccountType AccountType { get; init; }
    public string? PixKey { get; init; }
    public string? Label { get; init; }
}

public sealed class CreateCryptoWalletRequest
{
    public string StrawManAccountId { get; init; } = string.Empty;
    public Chain Chain { get; init; }
    public CryptoAsset Asset { get; init; }
    public string Address { get; init; } = string.Empty;
    public string? Memo { get; init; }
    public string? Label { get; init; }
}

public sealed class CreateWithdrawalRequest
{
    public string OperationId { get; init; } = string.Empty;
    public WithdrawalType Type { get; init; }
    public string StrawManAccountId { get; init; } = string.Empty;
    public string? BankAccountId { get; init; }
    public string? CryptoWalletId { get; init; }
    public IReadOnlyList<string> PaymentIds { get; init; } = Array.Empty<string>();
    public string? CostDescription { get; init; }
    public decimal CostAmount { get; init; }
    public string? PixTransactionId { get; init; }
    public string? PixAuthenticationCode { get; init; }
    public string? CryptoTransactionId { get; init; }
}

public sealed class UpdateBankAccountLabelRequest
{
    public string? Label { get; init; }
}

public sealed class SearchBankAccountsRequest
{
    public string? StrawManAccountId { get; init; }
    public int Limit { get; init; } = 30;
    public int Offset { get; init; }
}

public sealed class SearchCryptoWalletsRequest
{
    public string? StrawManAccountId { get; init; }
    public int Limit { get; init; } = 30;
    public int Offset { get; init; }
}

public sealed class SearchWithdrawalsRequest
{
    public string? OperationId { get; init; }
    public string? StrawManAccountId { get; init; }
    public WithdrawalType? Type { get; init; }
    public int Limit { get; init; } = 30;
    public int Offset { get; init; }
}

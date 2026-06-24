using Aidan.Core.Linq;
using Aidan.Core.Patterns;
using Nexus.AccountNodes.Aggregates;
using Nexus.AccountNodes.Application.Contracts;

namespace Nexus.AccountNodes.Application.Contracts;

public interface IBankAccountRepository : IRepository<BankAccount>
{
    new Task<BankAccount> CreateAsync(BankAccount entity);
}

public interface ICryptoWalletRepository : IRepository<CryptoWallet>
{
    new Task<CryptoWallet> CreateAsync(CryptoWallet entity);
}

public interface IBankAccountService
{
    Task<IResult<BankAccount>> CreateAsync(CreateBankAccountRequest request);
    Task<IResult<BankAccount>> UpdateLabelAsync(string bankAccountId, string? label);
    Task<IResult<BankAccount>> GetByIdAsync(string bankAccountId);
}

public interface ICryptoWalletService
{
    Task<IResult<CryptoWallet>> CreateAsync(CreateCryptoWalletRequest request);
    Task<IResult<CryptoWallet>> UpsertAddressAsync(UpsertCryptoWalletAddressRequest request);
    Task<IResult<CryptoWallet>> UpdateLabelAsync(string cryptoWalletId, string? label);
    Task<IResult<CryptoWallet>> GetByIdAsync(string cryptoWalletId);
}

public interface IBalanceSplitCalculationService
{
    Task<IResult<BalanceSplitCalculationResult>> CalculateForCreditAsync(
        string destinationStrawManId,
        decimal amount,
        IReadOnlyList<BalanceSplitSnapshot> originalSplits,
        IReadOnlyList<string> appliedStrawManFeeIds,
        CancellationToken cancellationToken = default);
}

public sealed class BalanceSplitCalculationResult
{
    public IReadOnlyList<BalanceSplitSnapshot> SplitSnapshot { get; init; } = Array.Empty<BalanceSplitSnapshot>();
    public IReadOnlyList<string> AppliedStrawManFeeIds { get; init; } = Array.Empty<string>();
}

public sealed class CreateBankAccountRequest
{
    public string StrawManId { get; init; } = string.Empty;
    public BrazilianBank Bank { get; init; }
    public string Agency { get; init; } = string.Empty;
    public string AccountNumber { get; init; } = string.Empty;
    public string? AccountDigit { get; init; }
    public BankAccountType AccountType { get; init; }
    public string? Label { get; init; }
}

public sealed class CreateCryptoWalletAddressRequest
{
    public AddressNamespace Namespace { get; init; }
    public string Address { get; init; } = string.Empty;
    public string? Memo { get; init; }
}

public sealed class CreateCryptoWalletRequest
{
    public string StrawManId { get; init; } = string.Empty;
    public IReadOnlyList<CreateCryptoWalletAddressRequest> Addresses { get; init; } = Array.Empty<CreateCryptoWalletAddressRequest>();
    public string? Label { get; init; }
}

public sealed class UpsertCryptoWalletAddressRequest
{
    public string CryptoWalletId { get; init; } = string.Empty;
    public AddressNamespace Namespace { get; init; }
    public string Address { get; init; } = string.Empty;
    public string? Memo { get; init; }
}

public sealed class UpsertCryptoWalletAddressBody
{
    public AddressNamespace Namespace { get; init; }
    public string Address { get; init; } = string.Empty;
    public string? Memo { get; init; }
}

public sealed class UpdateBankAccountLabelRequest
{
    public string? Label { get; init; }
}

public sealed class UpdateCryptoWalletLabelRequest
{
    public string? Label { get; init; }
}

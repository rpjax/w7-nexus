using Aidan.Core.Patterns;
using Nexus.AccountNodes.Application.Contracts;
using Nexus.AccountNodes.Aggregates;

namespace Nexus.Administrators.Application.Contracts;

public interface IAdministratorAccountNodeCommandService
{
    Task<IResult<BankAccount>> CreateBankAccountAsync(CreateBankAccountRequest request);
    Task<IResult<CryptoWallet>> CreateCryptoWalletAsync(CreateCryptoWalletRequest request);
    Task<IResult<CryptoWallet>> UpsertCryptoWalletAddressAsync(UpsertCryptoWalletAddressRequest request);
    Task<IResult<BankAccount>> GetBankAccountAsync(string bankAccountId);
    Task<IResult<CryptoWallet>> GetCryptoWalletAsync(string cryptoWalletId);
    Task<IResult<BankAccount>> UpdateBankAccountLabelAsync(string bankAccountId, string? label);
    Task<IResult<SearchBankAccountsResponse>> SearchBankAccountsAsync(SearchBankAccountsRequest? request);
    Task<IResult<SearchCryptoWalletsResponse>> SearchCryptoWalletsAsync(SearchCryptoWalletsRequest? request);
}

public sealed class SearchBankAccountsRequest
{
    public string? StrawManId { get; init; }
    public int Limit { get; init; } = 30;
    public int Offset { get; init; }
}

public sealed class SearchBankAccountsResponse
{
    public int Total { get; init; }
    public IReadOnlyList<BankAccount> Items { get; init; } = Array.Empty<BankAccount>();
}

public sealed class SearchCryptoWalletsRequest
{
    public string? StrawManId { get; init; }
    public int Limit { get; init; } = 30;
    public int Offset { get; init; }
}

public sealed class SearchCryptoWalletsResponse
{
    public int Total { get; init; }
    public IReadOnlyList<CryptoWallet> Items { get; init; } = Array.Empty<CryptoWallet>();
}

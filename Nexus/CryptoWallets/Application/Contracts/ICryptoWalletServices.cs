using Aidan.Core.Patterns;
using Nexus.CryptoWallets.Aggregates;
using Nexus.CryptoWallets.Application.Requests;
using Nexus.CryptoWallets.Application.Responses;

namespace Nexus.CryptoWallets.Application.Contracts;

public interface ICryptoWalletService
{
    Task<IResult<CryptoWallet>> CreateAsync(CreateCryptoWalletRequest request);
    Task<IResult<CryptoWallet>> UpsertAddressAsync(UpsertCryptoWalletAddressRequest request);
    Task<IResult<CryptoWallet>> UpdateLabelAsync(string cryptoWalletId, string? label);
    Task<IResult<CryptoWallet>> GetByIdAsync(string cryptoWalletId);
    Task<IResult<SearchCryptoWalletsResponse>> SearchAsync(SearchCryptoWalletsRequest? request);
}

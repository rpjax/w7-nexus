using Aidan.Core.Errors;
using Aidan.Core.Patterns;
using Nexus.AccountNodes.Aggregates;
using Nexus.AccountNodes.Application.Contracts;
using Nexus.Accounts.Application.Contracts;
using Nexus.AccountNodes.Errors;

namespace Nexus.AccountNodes.Application.Services;

public sealed class CryptoWalletService : ICryptoWalletService
{
    private readonly IAccountRepository _accounts;
    private readonly ICryptoWalletRepository _cryptoWallets;

    public CryptoWalletService(
        IAccountRepository accounts,
        ICryptoWalletRepository cryptoWallets)
    {
        _accounts = accounts;
        _cryptoWallets = cryptoWallets;
    }

    public async Task<IResult<CryptoWallet>> CreateAsync(CreateCryptoWalletRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var strawManValidation = StrawManValidation.ValidateStrawManAccount(
            _accounts,
            request.StrawManId,
            CryptoWalletErrorCodes.StrawManInvalid,
            CryptoWalletErrorCodes.StrawManNotFound,
            CryptoWalletErrorCodes.StrawManRoleRequired);

        if (strawManValidation is not null)
            return Result<CryptoWallet>.Failure(strawManValidation.Errors);

        var addresses = new List<CryptoWalletAddress>();
        foreach (var input in request.Addresses ?? Array.Empty<CreateCryptoWalletAddressRequest>())
        {
            var addressResult = CryptoWalletAddress.Create(input.Namespace, input.Address, input.Memo);
            if (addressResult.IsFailure)
                return Result<CryptoWallet>.Failure(addressResult.Errors);
            addresses.Add(addressResult.Value!);
        }

        var createResult = CryptoWallet.Create(request.StrawManId, addresses, request.Label);
        if (createResult.IsFailure)
            return createResult;

        var persisted = await _cryptoWallets.CreateAsync(createResult.Value!);
        return Result<CryptoWallet>.Success(persisted);
    }

    public async Task<IResult<CryptoWallet>> UpsertAddressAsync(UpsertCryptoWalletAddressRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var wallet = FindCryptoWallet(request.CryptoWalletId);
        if (wallet is null)
            return NotFound(request.CryptoWalletId);

        var addressResult = CryptoWalletAddress.Create(request.Namespace, request.Address, request.Memo);
        if (addressResult.IsFailure)
            return Result<CryptoWallet>.Failure(addressResult.Errors);

        var upsertResult = wallet.UpsertAddress(addressResult.Value!);
        if (upsertResult.IsFailure)
            return Result<CryptoWallet>.Failure(upsertResult.Errors);

        await _cryptoWallets.UpdateAsync(wallet);
        return Result<CryptoWallet>.Success(wallet);
    }

    public async Task<IResult<CryptoWallet>> UpdateLabelAsync(string cryptoWalletId, string? label)
    {
        var wallet = FindCryptoWallet(cryptoWalletId);
        if (wallet is null)
            return NotFound(cryptoWalletId);

        var updateResult = wallet.UpdateLabel(label);
        if (updateResult.IsFailure)
            return Result<CryptoWallet>.Failure(updateResult.Errors);

        await _cryptoWallets.UpdateAsync(wallet);
        return Result<CryptoWallet>.Success(wallet);
    }

    public Task<IResult<CryptoWallet>> GetByIdAsync(string cryptoWalletId)
    {
        var wallet = FindCryptoWallet(cryptoWalletId);
        return Task.FromResult(wallet is null
            ? NotFound(cryptoWalletId)
            : Result<CryptoWallet>.Success(wallet));
    }

    private CryptoWallet? FindCryptoWallet(string cryptoWalletId)
    {
        if (string.IsNullOrWhiteSpace(cryptoWalletId))
            return null;

        return _cryptoWallets.AsQueryable()
            .FirstOrDefault(w => w.Id == cryptoWalletId.Trim());
    }

    private static IResult<CryptoWallet> NotFound(string cryptoWalletId) =>
        Result<CryptoWallet>.Failure(Error.Create()
            .WithCode(CryptoWalletErrorCodes.CryptoWalletNotFound)
            .WithMessage($"A wallet crypto '{cryptoWalletId}' não foi encontrada.")
            .Build());
}

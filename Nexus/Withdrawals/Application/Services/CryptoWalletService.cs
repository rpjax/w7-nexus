using Aidan.Core.Errors;
using Aidan.Core.Patterns;
using Nexus.Accounts.Application.Contracts;
using Nexus.Withdrawals.Aggregates;
using Nexus.Withdrawals.Application.Contracts;
using Nexus.Withdrawals.Errors;

namespace Nexus.Withdrawals.Application.Services;

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
            request.StrawManAccountId,
            CryptoWalletErrorCodes.StrawManInvalid,
            CryptoWalletErrorCodes.StrawManNotFound,
            CryptoWalletErrorCodes.StrawManRoleRequired);

        if (strawManValidation is not null)
            return Result<CryptoWallet>.Failure(strawManValidation.Errors);

        var createResult = CryptoWallet.Create(
            request.StrawManAccountId,
            request.Chain,
            request.Asset,
            request.Address,
            request.Memo,
            request.Label);

        if (createResult.IsFailure)
            return createResult;

        var persisted = await _cryptoWallets.CreateAsync(createResult.Value!);
        return Result<CryptoWallet>.Success(persisted);
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

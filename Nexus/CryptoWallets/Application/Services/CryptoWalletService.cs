using Aidan.Core.Errors;
using Aidan.Core.Linq.Extensions;
using Aidan.Core.Patterns;
using Nexus.Accounts.Application.Contracts;
using Nexus.CryptoWallets.Aggregates;
using Nexus.CryptoWallets.Application.Contracts;
using Nexus.CryptoWallets.Errors;

namespace Nexus.CryptoWallets.Application.Services;

public sealed class CryptoWalletService : ICryptoWalletService
{
    private readonly ICryptoWalletRepository _cryptoWallets;
    private readonly IAccountIdValidator _accountIdValidator;

    public CryptoWalletService(
        ICryptoWalletRepository cryptoWallets,
        IAccountIdValidator accountIdValidator)
    {
        _cryptoWallets = cryptoWallets;
        _accountIdValidator = accountIdValidator;
    }

    public async Task<IResult<CryptoWallet>> CreateAsync(CreateCryptoWalletRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var ownerValidation = await ValidateOwnerExistsAsync(request.OwnerId);
        if (ownerValidation is not null)
            return Result<CryptoWallet>.Failure(ownerValidation.Errors);

        var addresses = new List<CryptoWalletAddress>();
        foreach (var input in request.Addresses ?? Array.Empty<CreateCryptoWalletAddressRequest>())
        {
            var addressResult = CryptoWalletAddress.Create(input.Namespace, input.Address, input.Memo);
            if (addressResult.IsFailure)
                return Result<CryptoWallet>.Failure(addressResult.Errors);
            addresses.Add(addressResult.Value!);
        }

        var createResult = CryptoWallet.Create(request.OwnerId, addresses, request.Label);
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

    public async Task<IResult<SearchCryptoWalletsResponse>> SearchAsync(SearchCryptoWalletsRequest? request)
    {
        request ??= new SearchCryptoWalletsRequest();
        var query = _cryptoWallets.AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.OwnerId))
            query = query.Where(w => w.OwnerId == request.OwnerId.Trim());

        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(w => w.CreatedAt)
            .Skip(Math.Max(0, request.Offset))
            .Take(NormalizeLimit(request.Limit))
            .ToArrayAsync();

        return Result<SearchCryptoWalletsResponse>.Success(new SearchCryptoWalletsResponse
        {
            Total = (int)total,
            Items = items,
        });
    }

    private static int NormalizeLimit(int limit) => limit <= 0 ? 30 : Math.Min(limit, 999);

    private async Task<IResult?> ValidateOwnerExistsAsync(string? ownerId)
    {
        if (string.IsNullOrWhiteSpace(ownerId))
        {
            return Result.Failure(Error.Create()
                .WithCode(CryptoWalletErrorCodes.OwnerInvalid)
                .WithMessage("O ID do dono da wallet é obrigatório.")
                .Build());
        }

        var normalizedOwnerId = ownerId.Trim();
        if (!await _accountIdValidator.ExistsAsync(normalizedOwnerId))
        {
            return Result.Failure(Error.Create()
                .WithCode(CryptoWalletErrorCodes.OwnerNotFound)
                .WithMessage($"A conta do dono '{normalizedOwnerId}' não foi encontrada.")
                .Build());
        }

        return null;
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

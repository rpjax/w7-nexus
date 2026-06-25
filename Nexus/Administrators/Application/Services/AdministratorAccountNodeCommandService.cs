using Aidan.Core.Errors;
using Aidan.Core.Linq.Extensions;
using Aidan.Core.Patterns;
using Nexus.Accounts.Application.Contracts;
using Nexus.Administrators.Application.Contracts;
using Nexus.Authorization;
using Nexus.BankAccounts.Aggregates;
using Nexus.BankAccounts.Application.Contracts;
using Nexus.BankAccounts.Errors;
using Nexus.CryptoWallets.Aggregates;
using Nexus.CryptoWallets.Application.Contracts;
using Nexus.CryptoWallets.Errors;

namespace Nexus.Administrators.Application.Services;

public sealed class AdministratorAccountNodeCommandService : IAdministratorAccountNodeCommandService
{
    private readonly IBankAccountService _bankAccounts;
    private readonly ICryptoWalletService _cryptoWallets;
    private readonly IBankAccountRepository _bankAccountRepository;
    private readonly ICryptoWalletRepository _cryptoWalletRepository;
    private readonly IAccountRepository _accounts;

    public AdministratorAccountNodeCommandService(
        IBankAccountService bankAccounts,
        ICryptoWalletService cryptoWallets,
        IBankAccountRepository bankAccountRepository,
        ICryptoWalletRepository cryptoWalletRepository,
        IAccountRepository accounts)
    {
        _bankAccounts = bankAccounts;
        _cryptoWallets = cryptoWallets;
        _bankAccountRepository = bankAccountRepository;
        _cryptoWalletRepository = cryptoWalletRepository;
        _accounts = accounts;
    }

    public async Task<IResult<BankAccount>> CreateBankAccountAsync(CreateBankAccountRequest request)
    {
        var validation = ValidateStrawMan(
            request.StrawManId,
            BankAccountErrorCodes.StrawManInvalid,
            BankAccountErrorCodes.StrawManNotFound,
            BankAccountErrorCodes.StrawManRoleRequired);

        if (validation is not null)
            return Result<BankAccount>.Failure(validation.Errors);

        return await _bankAccounts.CreateAsync(request);
    }

    public async Task<IResult<CryptoWallet>> CreateCryptoWalletAsync(CreateCryptoWalletRequest request)
    {
        var validation = ValidateStrawMan(
            request.StrawManId,
            CryptoWalletErrorCodes.StrawManInvalid,
            CryptoWalletErrorCodes.StrawManNotFound,
            CryptoWalletErrorCodes.StrawManRoleRequired);

        if (validation is not null)
            return Result<CryptoWallet>.Failure(validation.Errors);

        return await _cryptoWallets.CreateAsync(request);
    }

    public Task<IResult<CryptoWallet>> UpsertCryptoWalletAddressAsync(
        UpsertCryptoWalletAddressRequest request) =>
        _cryptoWallets.UpsertAddressAsync(request);

    public Task<IResult<BankAccount>> GetBankAccountAsync(string bankAccountId) =>
        _bankAccounts.GetByIdAsync(bankAccountId);

    public Task<IResult<CryptoWallet>> GetCryptoWalletAsync(string cryptoWalletId) =>
        _cryptoWallets.GetByIdAsync(cryptoWalletId);

    public Task<IResult<BankAccount>> UpdateBankAccountLabelAsync(
        string bankAccountId,
        string? label) =>
        _bankAccounts.UpdateLabelAsync(bankAccountId, label);

    public async Task<IResult<SearchBankAccountsResponse>> SearchBankAccountsAsync(SearchBankAccountsRequest? request)
    {
        request ??= new SearchBankAccountsRequest();
        var query = _bankAccountRepository.AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.StrawManId))
            query = query.Where(a => a.StrawManId == request.StrawManId.Trim());

        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(a => a.CreatedAt)
            .Skip(Math.Max(0, request.Offset))
            .Take(NormalizeLimit(request.Limit))
            .ToArrayAsync();

        return Result<SearchBankAccountsResponse>.Success(new SearchBankAccountsResponse
        {
            Total = (int)total,
            Items = items,
        });
    }

    public async Task<IResult<SearchCryptoWalletsResponse>> SearchCryptoWalletsAsync(SearchCryptoWalletsRequest? request)
    {
        request ??= new SearchCryptoWalletsRequest();
        var query = _cryptoWalletRepository.AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.StrawManId))
            query = query.Where(w => w.StrawManId == request.StrawManId.Trim());

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

    private IResult? ValidateStrawMan(
        string strawManId,
        string invalidCode,
        string notFoundCode,
        string roleRequiredCode)
    {
        if (string.IsNullOrWhiteSpace(strawManId))
            return Result.Failure(Error.Create()
                .WithCode(invalidCode)
                .WithMessage("O ID do laranja é obrigatório.")
                .Build());

        var account = _accounts.AsQueryable().FirstOrDefault(a => a.Id == strawManId.Trim());
        if (account is null)
            return Result.Failure(Error.Create()
                .WithCode(notFoundCode)
                .WithMessage($"A conta laranja '{strawManId}' não foi encontrada.")
                .Build());

        if (!account.Roles.Contains(Roles.StrawMan, StringComparer.Ordinal))
            return Result.Failure(Error.Create()
                .WithCode(roleRequiredCode)
                .WithMessage($"A conta '{strawManId}' não possui o perfil de laranja.")
                .Build());

        return null;
    }

    private static int NormalizeLimit(int limit) => limit <= 0 ? 30 : Math.Min(limit, 999);
}

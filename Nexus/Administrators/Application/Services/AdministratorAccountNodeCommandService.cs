using Aidan.Core.Linq.Extensions;
using Aidan.Core.Patterns;
using Nexus.AccountNodes.Application.Contracts;
using Nexus.Administrators.Application.Contracts;

namespace Nexus.Administrators.Application.Services;

public sealed class AdministratorAccountNodeCommandService : IAdministratorAccountNodeCommandService
{
    private readonly IBankAccountService _bankAccounts;
    private readonly ICryptoWalletService _cryptoWallets;
    private readonly IBankAccountRepository _bankAccountRepository;
    private readonly ICryptoWalletRepository _cryptoWalletRepository;

    public AdministratorAccountNodeCommandService(
        IBankAccountService bankAccounts,
        ICryptoWalletService cryptoWallets,
        IBankAccountRepository bankAccountRepository,
        ICryptoWalletRepository cryptoWalletRepository)
    {
        _bankAccounts = bankAccounts;
        _cryptoWallets = cryptoWallets;
        _bankAccountRepository = bankAccountRepository;
        _cryptoWalletRepository = cryptoWalletRepository;
    }

    public Task<IResult<AccountNodes.Aggregates.BankAccount>> CreateBankAccountAsync(CreateBankAccountRequest request) =>
        _bankAccounts.CreateAsync(request);

    public Task<IResult<AccountNodes.Aggregates.CryptoWallet>> CreateCryptoWalletAsync(CreateCryptoWalletRequest request) =>
        _cryptoWallets.CreateAsync(request);

    public Task<IResult<AccountNodes.Aggregates.CryptoWallet>> UpsertCryptoWalletAddressAsync(
        UpsertCryptoWalletAddressRequest request) =>
        _cryptoWallets.UpsertAddressAsync(request);

    public Task<IResult<AccountNodes.Aggregates.BankAccount>> GetBankAccountAsync(string bankAccountId) =>
        _bankAccounts.GetByIdAsync(bankAccountId);

    public Task<IResult<AccountNodes.Aggregates.CryptoWallet>> GetCryptoWalletAsync(string cryptoWalletId) =>
        _cryptoWallets.GetByIdAsync(cryptoWalletId);

    public Task<IResult<AccountNodes.Aggregates.BankAccount>> UpdateBankAccountLabelAsync(
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

    private static int NormalizeLimit(int limit) => limit <= 0 ? 30 : Math.Min(limit, 999);
}

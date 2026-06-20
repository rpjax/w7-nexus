using Aidan.Core.Linq.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nexus.Controllers;
using Nexus.Withdrawals.Aggregates;
using Nexus.Withdrawals.Application.Contracts;

namespace Nexus.Withdrawals.Presentation;

[Route("api/withdrawals")]
[Authorize]
public sealed class WithdrawalsController : NexusController
{
    private readonly IBankAccountService _bankAccounts;
    private readonly ICryptoWalletService _cryptoWallets;
    private readonly IWithdrawalService _withdrawals;
    private readonly IBankAccountRepository _bankAccountRepository;
    private readonly ICryptoWalletRepository _cryptoWalletRepository;
    private readonly IWithdrawalRepository _withdrawalRepository;

    public WithdrawalsController(
        IBankAccountService bankAccounts,
        ICryptoWalletService cryptoWallets,
        IWithdrawalService withdrawals,
        IBankAccountRepository bankAccountRepository,
        ICryptoWalletRepository cryptoWalletRepository,
        IWithdrawalRepository withdrawalRepository)
    {
        _bankAccounts = bankAccounts;
        _cryptoWallets = cryptoWallets;
        _withdrawals = withdrawals;
        _bankAccountRepository = bankAccountRepository;
        _cryptoWalletRepository = cryptoWalletRepository;
        _withdrawalRepository = withdrawalRepository;
    }

    [HttpPost("bank-accounts")]
    public async Task<ActionResult> CreateBankAccountAsync([FromBody] CreateBankAccountRequest request) =>
        ToResponse(await _bankAccounts.CreateAsync(request));

    [HttpPatch("bank-accounts/{bankAccountId}/label")]
    public async Task<ActionResult> UpdateBankAccountLabelAsync(
        string bankAccountId,
        [FromBody] UpdateBankAccountLabelRequest request) =>
        ToResponse(await _bankAccounts.UpdateLabelAsync(bankAccountId, request?.Label));

    [HttpPost("bank-accounts/search")]
    public async Task<ActionResult> SearchBankAccountsAsync([FromBody] SearchBankAccountsRequest? request)
    {
        request ??= new SearchBankAccountsRequest();
        var query = _bankAccountRepository.AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.StrawManAccountId))
            query = query.Where(a => a.StrawManAccountId == request.StrawManAccountId.Trim());

        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(a => a.CreatedAt)
            .Skip(Math.Max(0, request.Offset))
            .Take(NormalizeLimit(request.Limit))
            .ToArrayAsync();

        return Ok(new { Total = total, Items = items.Select(ToBankAccountResponse).ToArray() });
    }

    [HttpPost("crypto-wallets")]
    public async Task<ActionResult> CreateCryptoWalletAsync([FromBody] CreateCryptoWalletRequest request) =>
        ToResponse(await _cryptoWallets.CreateAsync(request));

    [HttpPost("crypto-wallets/search")]
    public async Task<ActionResult> SearchCryptoWalletsAsync([FromBody] SearchCryptoWalletsRequest? request)
    {
        request ??= new SearchCryptoWalletsRequest();
        var query = _cryptoWalletRepository.AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.StrawManAccountId))
            query = query.Where(w => w.StrawManAccountId == request.StrawManAccountId.Trim());

        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(w => w.CreatedAt)
            .Skip(Math.Max(0, request.Offset))
            .Take(NormalizeLimit(request.Limit))
            .ToArrayAsync();

        return Ok(new { Total = total, Items = items.Select(ToCryptoWalletResponse).ToArray() });
    }

    [HttpPost]
    public async Task<ActionResult> CreateWithdrawalAsync([FromBody] CreateWithdrawalRequest request) =>
        ToResponse(await _withdrawals.CreateWithdrawalAsync(request));

    [HttpPost("search")]
    public async Task<ActionResult> SearchWithdrawalsAsync([FromBody] SearchWithdrawalsRequest? request)
    {
        request ??= new SearchWithdrawalsRequest();
        var query = _withdrawalRepository.AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.OperationId))
            query = query.Where(w => w.OperationId == request.OperationId.Trim());

        if (!string.IsNullOrWhiteSpace(request.StrawManAccountId))
            query = query.Where(w => w.StrawManAccountId == request.StrawManAccountId.Trim());

        if (request.Type.HasValue)
            query = query.Where(w => w.Type == request.Type.Value);

        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(w => w.CreatedAt)
            .Skip(Math.Max(0, request.Offset))
            .Take(NormalizeLimit(request.Limit))
            .ToArrayAsync();

        return Ok(new { Total = total, Items = items.Select(ToWithdrawalResponse).ToArray() });
    }

    [HttpGet("{withdrawalId}")]
    public async Task<ActionResult> GetWithdrawalAsync(string withdrawalId) =>
        ToResponse(await _withdrawals.GetByIdAsync(withdrawalId));

    private static int NormalizeLimit(int limit) => limit <= 0 ? 30 : Math.Min(limit, 999);

    internal static object ToBankAccountResponse(BankAccount account)
    {
        var (name, code, ispb) = BrazilianBankMetadata.Get(account.Bank);
        return new
        {
            account.Id,
            account.StrawManAccountId,
            Bank = account.Bank.ToString(),
            BankName = name,
            BankCode = code,
            BankIspb = ispb,
            account.Agency,
            account.AccountNumber,
            account.AccountDigit,
            AccountType = account.AccountType.ToString(),
            PixKeyType = account.PixKeyType.ToString(),
            account.PixKey,
            account.Label,
            account.CreatedAt,
            account.UpdatedAt,
        };
    }

    internal static object ToCryptoWalletResponse(CryptoWallet wallet) => new
    {
        wallet.Id,
        wallet.StrawManAccountId,
        Chain = wallet.Chain.ToString(),
        ChainCaip2 = wallet.Chain.ToCaip2(),
        Asset = wallet.Asset.ToString(),
        wallet.Address,
        wallet.Memo,
        wallet.Label,
        wallet.CreatedAt,
        wallet.UpdatedAt,
    };

    internal static object ToWithdrawalResponse(Withdrawal withdrawal) => new
    {
        withdrawal.Id,
        withdrawal.OperationId,
        Type = withdrawal.Type.ToString(),
        withdrawal.StrawManAccountId,
        withdrawal.BankAccountId,
        withdrawal.CryptoWalletId,
        withdrawal.PaymentIds,
        withdrawal.CostDescription,
        withdrawal.CostAmount,
        PixProof = withdrawal.PixProof is null ? null : new
        {
            withdrawal.PixProof.TransactionId,
            withdrawal.PixProof.AuthenticationCode,
        },
        CryptoProof = withdrawal.CryptoProof is null ? null : new
        {
            withdrawal.CryptoProof.TransactionId,
        },
        withdrawal.PaymentsTotalAmount,
        withdrawal.NetAmount,
        withdrawal.CreatedAt,
    };
}

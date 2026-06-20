using Aidan.Core.Patterns;
using Nexus.Administrators.Application.Contracts;
using Nexus.Withdrawals.Application.Contracts;

namespace Nexus.Administrators.Application.Services;

public sealed class AdministratorWithdrawalCommandService : IAdministratorWithdrawalCommandService
{
    private readonly IBankAccountService _bankAccounts;
    private readonly ICryptoWalletService _cryptoWallets;
    private readonly IWithdrawalService _withdrawals;

    public AdministratorWithdrawalCommandService(
        IBankAccountService bankAccounts,
        ICryptoWalletService cryptoWallets,
        IWithdrawalService withdrawals)
    {
        _bankAccounts = bankAccounts;
        _cryptoWallets = cryptoWallets;
        _withdrawals = withdrawals;
    }

    public Task<IResult<Withdrawals.Aggregates.BankAccount>> CreateBankAccountAsync(CreateBankAccountRequest request) =>
        _bankAccounts.CreateAsync(request);

    public Task<IResult<Withdrawals.Aggregates.CryptoWallet>> CreateCryptoWalletAsync(CreateCryptoWalletRequest request) =>
        _cryptoWallets.CreateAsync(request);

    public Task<IResult<Withdrawals.Aggregates.Withdrawal>> CreateWithdrawalAsync(CreateWithdrawalRequest request) =>
        _withdrawals.CreateWithdrawalAsync(request);

    public Task<IResult<Withdrawals.Aggregates.Withdrawal>> GetWithdrawalAsync(string withdrawalId) =>
        _withdrawals.GetByIdAsync(withdrawalId);
}

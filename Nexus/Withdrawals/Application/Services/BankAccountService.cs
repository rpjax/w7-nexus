using Aidan.Core.Errors;
using Aidan.Core.Patterns;
using Nexus.Accounts.Application.Contracts;
using Nexus.Withdrawals.Aggregates;
using Nexus.Withdrawals.Application.Contracts;
using Nexus.Withdrawals.Errors;

namespace Nexus.Withdrawals.Application.Services;

public sealed class BankAccountService : IBankAccountService
{
    private readonly IAccountRepository _accounts;
    private readonly IBankAccountRepository _bankAccounts;

    public BankAccountService(
        IAccountRepository accounts,
        IBankAccountRepository bankAccounts)
    {
        _accounts = accounts;
        _bankAccounts = bankAccounts;
    }

    public async Task<IResult<BankAccount>> CreateAsync(CreateBankAccountRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var strawManValidation = StrawManValidation.ValidateStrawManAccount(
            _accounts,
            request.StrawManAccountId,
            BankAccountErrorCodes.StrawManInvalid,
            BankAccountErrorCodes.StrawManNotFound,
            BankAccountErrorCodes.StrawManRoleRequired);

        if (strawManValidation is not null)
            return Result<BankAccount>.Failure(strawManValidation.Errors);

        var createResult = BankAccount.Create(
            request.StrawManAccountId,
            request.Bank,
            request.Agency,
            request.AccountNumber,
            request.AccountDigit,
            request.AccountType,
            request.PixKeyType,
            request.PixKey,
            request.Label);

        if (createResult.IsFailure)
            return createResult;

        var persisted = await _bankAccounts.CreateAsync(createResult.Value!);
        return Result<BankAccount>.Success(persisted);
    }

    public async Task<IResult<BankAccount>> UpdateLabelAsync(string bankAccountId, string? label)
    {
        var account = FindBankAccount(bankAccountId);
        if (account is null)
            return NotFound(bankAccountId);

        var updateResult = account.UpdateLabel(label);
        if (updateResult.IsFailure)
            return Result<BankAccount>.Failure(updateResult.Errors);

        await _bankAccounts.UpdateAsync(account);
        return Result<BankAccount>.Success(account);
    }

    public Task<IResult<BankAccount>> GetByIdAsync(string bankAccountId)
    {
        var account = FindBankAccount(bankAccountId);
        return Task.FromResult(account is null
            ? NotFound(bankAccountId)
            : Result<BankAccount>.Success(account));
    }

    private BankAccount? FindBankAccount(string bankAccountId)
    {
        if (string.IsNullOrWhiteSpace(bankAccountId))
            return null;

        return _bankAccounts.AsQueryable()
            .FirstOrDefault(a => a.Id == bankAccountId.Trim());
    }

    private static IResult<BankAccount> NotFound(string bankAccountId) =>
        Result<BankAccount>.Failure(Error.Create()
            .WithCode(BankAccountErrorCodes.BankAccountNotFound)
            .WithMessage($"A conta bancária '{bankAccountId}' não foi encontrada.")
            .Build());
}

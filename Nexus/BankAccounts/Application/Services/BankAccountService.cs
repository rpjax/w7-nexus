using Aidan.Core.Errors;
using Aidan.Core.Linq.Extensions;
using Aidan.Core.Patterns;
using Nexus.Accounts.Application.Contracts;
using Nexus.BankAccounts.Aggregates;
using Nexus.BankAccounts.Application.Contracts;
using Nexus.BankAccounts.Application.Requests;
using Nexus.BankAccounts.Application.Responses;
using Nexus.BankAccounts.Errors;

namespace Nexus.BankAccounts.Application.Services;

public sealed class BankAccountService : IBankAccountService
{
    private IBankAccountRepository _bankAccounts { get; }
    private IAccountIdValidator _accountIdValidator { get; }

    public BankAccountService(
        IBankAccountRepository bankAccounts,
        IAccountIdValidator accountIdValidator)
    {
        _bankAccounts = bankAccounts;
        _accountIdValidator = accountIdValidator;
    }

    public async Task<IResult<BankAccount>> CreateAsync(CreateBankAccountRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var ownerValidation = await ValidateOwnerExistsAsync(request.OwnerId);
        if (ownerValidation is not null)
            return Result<BankAccount>.Failure(ownerValidation.Errors);

        var createResult = BankAccount.Create(
            request.OwnerId,
            request.Bank,
            request.Agency,
            request.AccountNumber,
            request.AccountDigit,
            request.AccountType,
            request.Label);

        if (createResult.IsFailure)
            return createResult;

        var persisted = await _bankAccounts.CreateAsync(createResult.Value!);
        return Result<BankAccount>.Success(persisted);
    }

    public async Task<IResult<BankAccount>> UpdateLabelAsync(string bankAccountId, string? label)
    {
        var account = await FindBankAccountAsync(bankAccountId);
        if (account is null)
            return NotFound(bankAccountId);

        var updateResult = account.UpdateLabel(label);
        if (updateResult.IsFailure)
            return Result<BankAccount>.Failure(updateResult.Errors);

        await _bankAccounts.UpdateAsync(account);
        return Result<BankAccount>.Success(account);
    }

    public async Task<IResult<BankAccount>> GetByIdAsync(string bankAccountId)
    {
        var account = await FindBankAccountAsync(bankAccountId);
        return account is null
            ? NotFound(bankAccountId)
            : Result<BankAccount>.Success(account);
    }

    public async Task<IResult<SearchBankAccountsResponse>> SearchAsync(SearchBankAccountsRequest? request)
    {
        request ??= new SearchBankAccountsRequest();
        var query = _bankAccounts.AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.OwnerId))
            query = query.Where(a => a.OwnerId == request.OwnerId.Trim());

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

    private static int NormalizeLimit(int limit) => limit <= 0 ? 30 : Math.Min(limit, 999);

    private async Task<IResult?> ValidateOwnerExistsAsync(string? ownerId)
    {
        if (string.IsNullOrWhiteSpace(ownerId))
        {
            return Result.Failure(Error.Create()
                .WithCode(BankAccountErrorCodes.OwnerInvalid)
                .WithMessage("O ID do dono da conta é obrigatório.")
                .Build());
        }

        var normalizedOwnerId = ownerId.Trim();
        if (!await _accountIdValidator.ExistsAsync(normalizedOwnerId))
        {
            return Result.Failure(Error.Create()
                .WithCode(BankAccountErrorCodes.OwnerNotFound)
                .WithMessage($"A conta do dono '{normalizedOwnerId}' não foi encontrada.")
                .Build());
        }

        return null;
    }

    private async Task<BankAccount?> FindBankAccountAsync(string bankAccountId)
    {
        if (string.IsNullOrWhiteSpace(bankAccountId))
            return null;

        return await _bankAccounts.AsQueryable()
            .Where(a => a.Id == bankAccountId.Trim())
            .FirstOrDefaultAsync();
    }

    private static IResult<BankAccount> NotFound(string bankAccountId) =>
        Result<BankAccount>.Failure(Error.Create()
            .WithCode(BankAccountErrorCodes.BankAccountNotFound)
            .WithMessage($"A conta bancária '{bankAccountId}' não foi encontrada.")
            .Build());
}

using Aidan.Core.Linq;
using Aidan.Core.Patterns;
using Nexus.BankAccounts.Aggregates;

namespace Nexus.BankAccounts.Application.Contracts;

public interface IBankAccountRepository : IRepository<BankAccount>
{
    new Task<BankAccount> CreateAsync(BankAccount entity);
}

public interface IBankAccountService
{
    Task<IResult<BankAccount>> CreateAsync(CreateBankAccountRequest request);
    Task<IResult<BankAccount>> UpdateLabelAsync(string bankAccountId, string? label);
    Task<IResult<BankAccount>> GetByIdAsync(string bankAccountId);
}

public sealed class CreateBankAccountRequest
{
    public string StrawManId { get; init; } = string.Empty;
    public BrazilianBank Bank { get; init; }
    public string Agency { get; init; } = string.Empty;
    public string AccountNumber { get; init; } = string.Empty;
    public string? AccountDigit { get; init; }
    public BankAccountType AccountType { get; init; }
    public string? Label { get; init; }
}

public sealed class UpdateBankAccountLabelRequest
{
    public string? Label { get; init; }
}

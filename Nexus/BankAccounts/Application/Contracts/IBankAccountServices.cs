using Aidan.Core.Patterns;
using Nexus.BankAccounts.Aggregates;
using Nexus.BankAccounts.Application.Requests;
using Nexus.BankAccounts.Application.Responses;

namespace Nexus.BankAccounts.Application.Contracts;

public interface IBankAccountService
{
    Task<IResult<BankAccount>> CreateAsync(CreateBankAccountRequest request);
    Task<IResult<BankAccount>> UpdateLabelAsync(string bankAccountId, string? label);
    Task<IResult<BankAccount>> GetByIdAsync(string bankAccountId);
    Task<IResult<SearchBankAccountsResponse>> SearchAsync(SearchBankAccountsRequest? request);
}

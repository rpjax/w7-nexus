using Nexus.BankAccounts.Aggregates;

namespace Nexus.BankAccounts.Application.Responses;

public sealed class SearchBankAccountsResponse
{
    public int Total { get; init; }
    public IReadOnlyList<BankAccount> Items { get; init; } = Array.Empty<BankAccount>();
}

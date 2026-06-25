using Nexus.BankAccounts.Aggregates;

namespace Nexus.BankAccounts.Application.Requests;

public sealed class CreateBankAccountRequest
{
    public string OwnerId { get; init; } = string.Empty;
    public BrazilianBank Bank { get; init; }
    public BankAccountType AccountType { get; init; }
    public string Agency { get; init; } = string.Empty;
    public string AccountNumber { get; init; } = string.Empty;
    public string? AccountDigit { get; init; }
    public string? Label { get; init; }
}

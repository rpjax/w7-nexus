namespace Nexus.BankAccounts.Application.Requests;

public sealed class SearchBankAccountsRequest
{
    public string? OwnerId { get; init; }
    public int Limit { get; init; } = 30;
    public int Offset { get; init; }
}

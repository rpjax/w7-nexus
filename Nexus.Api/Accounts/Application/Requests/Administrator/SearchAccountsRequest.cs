namespace Nexus.Accounts.Application.Requests.Administrator;

public class SearchAccountsRequest
{
    public int Limit { get; set; }
    public int Offset { get; set; }
    public string? Keyword { get; set; }
}

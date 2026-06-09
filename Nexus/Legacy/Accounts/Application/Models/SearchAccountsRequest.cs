namespace Nexus.Legacy.Accounts.Application.Models;

public class SearchAccountsRequest
{
    public int Limit { get; set; }
    public int Offset { get; set; }
    public string? Keyword { get; set; }
}

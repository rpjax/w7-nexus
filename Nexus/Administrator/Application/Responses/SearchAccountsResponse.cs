using Nexus.Administrator.Application.Responses.Models;

namespace Nexus.Administrator.Application.Responses;

public class SearchAccountsResponse : SearchResponse<AccountDetails>
{
    public int Total { get; set; }
}

using Nexus.Actors.Responses.Models;

namespace Nexus.Actors.Responses;

public class SearchAccountsResponse : SearchResponse<AccountDetails>
{
    public int Total { get; set; }
}

using Nexus.Actors.Responses.Models;

namespace Nexus.Actors.Responses;

public class SearchOperationsResponse : SearchResponse<OperationDetails>
{
    public int Total { get; set; }
}

using Nexus.Administrator.Application.Responses.Models;

namespace Nexus.Administrator.Application.Responses;

public class SearchOperationsResponse : SearchResponse<OperationDetails>
{
    public int Total { get; set; }
}

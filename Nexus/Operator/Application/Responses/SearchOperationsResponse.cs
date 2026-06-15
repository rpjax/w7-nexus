using Nexus.Operator.Application.Responses.Models;

namespace Nexus.Operator.Application.Responses;

public class SearchOperationsResponse : SearchResponse<OperationDetails>
{
    public int Total { get; set; }
}

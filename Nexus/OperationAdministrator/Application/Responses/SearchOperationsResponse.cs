using Nexus.OperationAdministrator.Application.Responses.Models;

namespace Nexus.OperationAdministrator.Application.Responses;

public class SearchOperationsResponse : SearchResponse<OperationDetails>
{
    public int Total { get; set; }
}

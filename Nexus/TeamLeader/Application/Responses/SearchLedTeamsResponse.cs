using Nexus.TeamLeader.Application.Responses.Models;

namespace Nexus.TeamLeader.Application.Responses;

public class SearchLedTeamsResponse : SearchResponse<OperationWithLedTeamsDetails>
{
    public int Total { get; set; }
}

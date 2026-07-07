namespace Nexus.Olx.Application.Requests.Operator;

public class SearchAdPatchesRequest
{
    public int Limit { get; set; }
    public int Offset { get; set; }
    public string? Keyword { get; set; }
    public string[] OperationIds { get; set; } = [];
}

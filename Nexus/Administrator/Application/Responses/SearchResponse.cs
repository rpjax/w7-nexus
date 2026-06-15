namespace Nexus.Administrator.Application.Responses;

public abstract class SearchResponse<T>
{
    public int Offset { get; set; }
    public int Limit { get; set; }
    public List<T> Items { get; set; } = new();
}

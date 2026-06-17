namespace Nexus.Shared.Application.Models;

public abstract class SearchResponse<T>
{
    public int Offset { get; set; }
    public int Limit { get; set; }
    public int Total { get; set; }
    public List<T> Items { get; set; } = new();
}

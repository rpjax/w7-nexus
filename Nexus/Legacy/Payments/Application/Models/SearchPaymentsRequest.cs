namespace Nexus.Legacy.Payments.Application.Models;

public class SearchPaymentsRequest
{
    public int Limit { get; set; }
    public int Offset { get; set; }
    public string? Keyword { get; set; }
}

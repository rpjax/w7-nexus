namespace Nexus.Legacy.Frendz.Application.Models;

public class SearchFrendzCredentialsRequest
{
    public int Limit { get; set; }
    public int Offset { get; set; }
    public string? Keyword { get; set; }

    /// <summary>Null = todas; true = só habilitadas; false = só desabilitadas.</summary>
    public bool? EnabledOnly { get; set; }
}

namespace Nexus.Legacy.Frendz.Application.Models;

public class AddCredentialsRequest
{
    public string? StrawManId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Token { get; set; } = string.Empty;
    public bool Enabled { get; set; } = true;
}

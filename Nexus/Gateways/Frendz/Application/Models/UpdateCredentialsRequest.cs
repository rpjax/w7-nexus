namespace Nexus.Gateways.Frendz.Application.Models;

public class UpdateCredentialsRequest
{
    public string Id { get; set; } = string.Empty;
    public string? StrawManId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Token { get; set; } = string.Empty;
    public bool Enabled { get; set; } = true;
}

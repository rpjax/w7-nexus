namespace Nexus.Gateways.Frendz.Application.Models;

public class FrendzApiCredentials : IGatewayServiceCredentials
{
    public string Id { get; init; } = string.Empty;
    public bool Enabled { get; init; } = true;
    public string? StrawManId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Token { get; init; } = string.Empty;
}

public interface IGatewayServiceCredentials
{
    string Id { get; }
    bool Enabled { get; }
    string? StrawManId { get; }
}
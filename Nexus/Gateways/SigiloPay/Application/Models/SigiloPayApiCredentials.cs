namespace Nexus.Gateways.SigiloPay.Application.Models;

public class SigiloPayApiCredentials
{
    public string Id { get; init; } = string.Empty;
    public bool Enabled { get; init; } = true;
    public string? StrawManId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string PublicKey { get; init; } = string.Empty;
    public string SecretKey { get; init; } = string.Empty;
}

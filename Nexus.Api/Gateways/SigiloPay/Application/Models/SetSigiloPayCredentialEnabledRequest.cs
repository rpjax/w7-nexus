namespace Nexus.Gateways.SigiloPay.Application.Models;

public sealed class SetSigiloPayCredentialEnabledRequest
{
    public string Id { get; set; } = string.Empty;
    public bool Enabled { get; set; }
}

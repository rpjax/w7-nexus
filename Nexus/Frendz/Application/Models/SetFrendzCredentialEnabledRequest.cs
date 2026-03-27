namespace Nexus.Frendz.Application.Models;

public sealed class SetFrendzCredentialEnabledRequest
{
    public string Id { get; set; } = string.Empty;
    public bool Enabled { get; set; }
}

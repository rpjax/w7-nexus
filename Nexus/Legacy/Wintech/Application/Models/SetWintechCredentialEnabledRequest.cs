namespace Nexus.Legacy.Wintech.Application.Models;

public sealed class SetWintechCredentialEnabledRequest
{
    public string Id { get; set; } = string.Empty;
    public bool Enabled { get; set; }
}

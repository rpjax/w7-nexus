namespace Nexus.SigiloPay.Application.Models;

public class UpdateSigiloPayCredentialsRequest
{
    public string Id { get; set; } = string.Empty;
    public string? StrawManId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string PublicKey { get; set; } = string.Empty;
    public string SecretKey { get; set; } = string.Empty;
    public bool Enabled { get; set; } = true;
}

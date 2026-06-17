namespace Nexus.Authentications.Application.Services.Models;

public sealed class AuthenticationOptions
{
    public const string SectionName = "Authentication";

    public string AdministratorToken { get; set; } = string.Empty;
}

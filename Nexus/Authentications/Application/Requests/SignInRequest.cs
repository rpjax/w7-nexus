namespace Nexus.Authentications.Application.Requests;

public class SignInRequest
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

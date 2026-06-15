namespace Nexus.Authentication.Application.Services.Requests;

public class SignUpRequest
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

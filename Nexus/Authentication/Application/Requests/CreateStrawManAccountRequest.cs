namespace Nexus.Authentication.Application.Requests;

public class CreateStrawManAccountRequest
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

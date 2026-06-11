namespace Nexus.Controllers.Authentication.Requests;

public class CreateAccountRequest
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

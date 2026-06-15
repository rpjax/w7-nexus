namespace Nexus.Authentication.Application.Requests;

public class CreateAdministratorAccountRequest
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

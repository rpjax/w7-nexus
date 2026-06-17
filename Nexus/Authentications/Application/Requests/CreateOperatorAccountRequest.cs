namespace Nexus.Authentications.Application.Requests;

public class CreateOperatorAccountRequest
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

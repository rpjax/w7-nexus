namespace Nexus.Actors.Requests;

// IUnauthenticatedUser
public class CreateOperatorAccountRequest
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

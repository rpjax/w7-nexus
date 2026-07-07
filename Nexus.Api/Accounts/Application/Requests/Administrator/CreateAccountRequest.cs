namespace Nexus.Accounts.Application.Requests.Administrator;

public class CreateAccountRequest
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string[]? Roles { get; set; }
    public string[]? Permissions { get; set; }
}

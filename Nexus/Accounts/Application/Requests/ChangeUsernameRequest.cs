namespace Nexus.Accounts.Application.Requests;

public class ChangeUsernameRequest
{
    public string AccountId { get; set; } = string.Empty;
    public string NewUsername { get; set; } = string.Empty;
}

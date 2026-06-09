namespace Nexus.Legacy.Accounts.Application.Models;

public class ChangeUsernameRequest
{
    public string AccountId { get; set; } = string.Empty;
    public string NewUsername { get; set; } = string.Empty;
}

namespace Nexus.Legacy.Accounts.Application.Models;

public class RemoveRoleRequest
{
    public string AccountId { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
}

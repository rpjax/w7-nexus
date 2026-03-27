namespace Nexus.Accounts.Application.Models;

public class AddRoleRequest
{
    public string AccountId { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
}

namespace Nexus.Accounts.Presentation.Requests;

public class AddRoleRequest
{
    public string AccountId { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
}

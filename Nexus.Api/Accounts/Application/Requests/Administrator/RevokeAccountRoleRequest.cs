namespace Nexus.Accounts.Application.Requests.Administrator;

public class RevokeAccountRoleRequest
{
    public string AccountId { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
}

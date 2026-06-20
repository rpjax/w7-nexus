namespace Nexus.Administrators.Application.Requests;

public class GrantAccountRoleRequest
{
    public string AccountId { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
}

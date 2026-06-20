namespace Nexus.Administrators.Application.Requests;

public class RevokeAccountRoleRequest
{
    public string AccountId { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
}

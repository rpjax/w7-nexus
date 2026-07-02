namespace Nexus.Accounts.Application.Requests.Administrator;

public class RevokeAccountPermissionRequest
{
    public string AccountId { get; set; } = string.Empty;
    public string Permission { get; set; } = string.Empty;
}

namespace Nexus.Legacy.Accounts.Application.Models;

public class RemovePermissionRequest
{
    public string AccountId { get; set; } = string.Empty;
    public string Permission { get; set; } = string.Empty;

}

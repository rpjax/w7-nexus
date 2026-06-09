namespace Nexus.Legacy.Accounts.Application.Models;

public class AddPermissionRequest
{
    public string AccountId { get; set; } = string.Empty;
    public string Permission { get; set; } = string.Empty;
}

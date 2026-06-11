namespace Nexus.Accounts.Presentation.Requests;

public class RemovePermissionRequest
{
    public string AccountId { get; set; } = string.Empty;
    public string Permission { get; set; } = string.Empty;

}

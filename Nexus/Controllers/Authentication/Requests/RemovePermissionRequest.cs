namespace Nexus.Controllers.Authentication.Requests;

public class RemovePermissionRequest
{
    public string AccountId { get; set; } = string.Empty;
    public string Permission { get; set; } = string.Empty;

}

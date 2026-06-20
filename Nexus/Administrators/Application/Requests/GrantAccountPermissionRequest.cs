namespace Nexus.Administrators.Application.Requests;

public class GrantAccountPermissionRequest
{
    public string AccountId { get; set; } = string.Empty;
    public string Permission { get; set; } = string.Empty;
}

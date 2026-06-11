namespace Nexus.Actors.Requests;

public class UnassignOperationAdministratorRequest
{
    public string OperationId { get; set; } = string.Empty;
    public string AdministratorId { get; set; } = string.Empty;
}

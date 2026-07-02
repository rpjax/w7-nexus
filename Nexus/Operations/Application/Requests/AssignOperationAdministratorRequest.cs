namespace Nexus.Operations.Application.Requests.Administrator;

public class AssignOperationAdministratorRequest
{
    public string OperationId { get; set; } = string.Empty;
    public string AdministratorId { get; set; } = string.Empty;
}

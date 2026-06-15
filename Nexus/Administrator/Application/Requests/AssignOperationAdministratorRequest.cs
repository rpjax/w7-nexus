namespace Nexus.Administrator.Application.Requests;

public class AssignOperationAdministratorRequest
{
    public string OperationId { get; set; } = string.Empty;
    public string AdministratorId { get; set; } = string.Empty;
}

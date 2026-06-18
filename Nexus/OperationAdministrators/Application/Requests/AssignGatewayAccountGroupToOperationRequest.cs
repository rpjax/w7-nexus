namespace Nexus.OperationAdministrators.Application.Requests;

public class AssignGatewayAccountGroupToOperationRequest
{
    public string OperationId { get; set; } = string.Empty;
    public string GatewayCredentialsGroupId { get; set; } = string.Empty;
}

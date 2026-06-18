namespace Nexus.OperationAdministrators.Application.Requests;

public class UnassignGatewayAccountGroupFromOperationRequest
{
    public string OperationId { get; set; } = string.Empty;
    public string GatewayCredentialsGroupId { get; set; } = string.Empty;
}

namespace Nexus.Gateways.Application;

public interface IGatewayCredentialsIdValidator
{
    Task<bool> ExistsAsync(string credentialsId);
}

namespace Nexus.Charges.Application;

public interface IGatewayCredentialsIdValidator
{
    Task<bool> ExistsAsync(string credentialsId);
}

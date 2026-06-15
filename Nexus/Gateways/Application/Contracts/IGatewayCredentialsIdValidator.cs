using Nexus.Gateways.Application.Services.Contracts;

namespace Nexus.Gateways.Application.Services.Contracts;

public interface IGatewayCredentialsIdValidator
{
    Task<bool> ExistsAsync(string credentialsId);
}

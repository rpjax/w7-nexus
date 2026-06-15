using Nexus.Gateways.Application.Services;
using Nexus.Gateways.SigiloPay.Application.Contracts;
using Nexus.Gateways.Application.Contracts;
using Nexus.Gateways.SigiloPay.Application.Services;
using Nexus.Gateways.SigiloPay.Application.Models;

namespace Nexus.Gateways.Application.Services;

public sealed class SigiloPayGatewayPixServiceFactory : ISigiloPayGatewayPixServiceFactory
{
    private ISigiloPayClient _sigiloPayClient { get; }

    public SigiloPayGatewayPixServiceFactory(ISigiloPayClient sigiloPayClient)
    {
        _sigiloPayClient = sigiloPayClient;
    }

    public IGatewayPixService Create(SigiloPayApiCredentials credentials)
    {
        ArgumentNullException.ThrowIfNull(credentials);
        return new SigiloPayGatewayPixService(_sigiloPayClient, credentials);
    }
}

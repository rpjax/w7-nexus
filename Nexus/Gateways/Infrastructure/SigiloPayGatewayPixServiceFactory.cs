using Nexus.Gateways.Application;
using Nexus.Gateways.SigiloPay.Application;
using Nexus.Gateways.SigiloPay.Application.Models;

namespace Nexus.Gateways.Infrastructure;

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

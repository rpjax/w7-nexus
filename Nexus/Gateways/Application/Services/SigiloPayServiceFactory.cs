using Nexus.Gateways.SigiloPay.Application.Contracts;
using Nexus.Gateways.Application.Contracts;
using Nexus.Gateways.SigiloPay.Application.Models;

namespace Nexus.Gateways.Application.Services;

public sealed class SigiloPayServiceFactory : ISigiloPayServiceFactory
{
    private ISigiloPayClient _sigiloPayClient { get; }

    public SigiloPayServiceFactory(ISigiloPayClient sigiloPayClient)
    {
        _sigiloPayClient = sigiloPayClient;
    }

    public Task<IGatewayService> CreateAsync(SigiloPayApiCredentials credentials)
    {
        ArgumentNullException.ThrowIfNull(credentials);
        return Task.FromResult<IGatewayService>(new SigiloPayService(_sigiloPayClient, credentials));
    }
}

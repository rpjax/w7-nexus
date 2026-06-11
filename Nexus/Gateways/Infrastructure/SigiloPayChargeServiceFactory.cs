using Nexus.Charges.Infrastructure;
using Nexus.Gateways.Application;
using Nexus.Gateways.SigiloPay.Application;
using Nexus.Gateways.SigiloPay.Application.Models;

namespace Nexus.Gateways.Infrastructure;

public sealed class SigiloPayChargeServiceFactory : ISigiloPayChargeServiceFactory
{
    private ISigiloPayClient _sigiloPayClient { get; }

    public SigiloPayChargeServiceFactory(ISigiloPayClient sigiloPayClient)
    {
        _sigiloPayClient = sigiloPayClient;
    }

    public IChargeService Create(SigiloPayApiCredentials credentials)
    {
        ArgumentNullException.ThrowIfNull(credentials);
        return new SigiloPayChargeService(_sigiloPayClient, credentials);
    }
}

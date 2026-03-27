using Nexus.Charges.Application;
using Nexus.SigiloPay.Application;
using Nexus.SigiloPay.Application.Models;

namespace Nexus.Charges.Infrastructure;

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

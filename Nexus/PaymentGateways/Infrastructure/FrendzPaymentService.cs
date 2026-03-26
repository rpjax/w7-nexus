using Nexus.Frendz.Application;
using Nexus.PaymentGateways.Application;
using Nexus.PaymentGateways.Application.Models;
using Nexus.Payments.Application;

namespace Nexus.PaymentGateways.Infrastructure;

public class FrendzPaymentService : IPaymentGatewayService
{
    private IFrendzApiKeysService _credentialsService { get; }
    private IPixPaymentService _pixPaymentService { get; }

    public FrendzPaymentService(IFrendzApiKeysService credentialsService, IPixPaymentService pixPaymentService)
    {
        _credentialsService = credentialsService;
        _pixPaymentService = pixPaymentService;
    }

    public Task<PixPayment> CreatePixPaymentAsync(string userId, decimal amount)
    {
        throw new NotImplementedException();
    }

}
